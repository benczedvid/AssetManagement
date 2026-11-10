using AssetManagement.Application.Assets;
using AssetManagement.Application.Common.Authorization;
using AssetManagement.Application.Common.Interfaces.AssetHistories;
using AssetManagement.Application.Common.Interfaces.Assets;
using AssetManagement.Application.Common.Interfaces.ServiceCases;
using AssetManagement.Application.Common.Interfaces.ServiceDesk;
using AssetManagement.Application.Common.Interfaces.Users;
using AssetManagement.Application.ServiceCases.Exceptions;
using AssetManagement.Domain.Entities.AssetHistories;
using AssetManagement.Domain.Entities.Assets;
using AssetManagement.Domain.Entities.ServiceCases;
using AssetManagement.Domain.Entities.ServiceDesk;
using AssetManagement.Domain.Entities.Users;

namespace AssetManagement.Application.ServiceCases.ReturnFromExternalService
{
    public sealed class ReturnFromExternalServiceHandler
    {
        private readonly IServiceCaseRepository _serviceCaseRepository;
        private readonly IAssetRepository _assetRepository;
        private readonly IAssetHistoryRepository _assetHistoryRepository;
        private readonly ICurrentApplicationUserResolver _currentApplicationUserResolver;
        private readonly IAssetOperationExecutor _assetOperationExecutor;
        private readonly IServiceDeskNoteOutboxRepository _noteOutboxRepository;

        public ReturnFromExternalServiceHandler(
            IServiceCaseRepository serviceCaseRepository,
            IAssetRepository assetRepository,
            IAssetHistoryRepository assetHistoryRepository,
            ICurrentApplicationUserResolver currentApplicationUserResolver,
            IAssetOperationExecutor assetOperationExecutor,
            IServiceDeskNoteOutboxRepository serviceDeskNoteOutboxRepository)
        {
            ArgumentNullException.ThrowIfNull(serviceCaseRepository);
            ArgumentNullException.ThrowIfNull(assetRepository);
            ArgumentNullException.ThrowIfNull(assetHistoryRepository);
            ArgumentNullException.ThrowIfNull(currentApplicationUserResolver);
            ArgumentNullException.ThrowIfNull(assetOperationExecutor);
            ArgumentNullException.ThrowIfNull(serviceDeskNoteOutboxRepository);

            _serviceCaseRepository = serviceCaseRepository;
            _assetRepository = assetRepository;
            _assetHistoryRepository = assetHistoryRepository;
            _currentApplicationUserResolver = currentApplicationUserResolver;
            _assetOperationExecutor = assetOperationExecutor;
            _noteOutboxRepository = serviceDeskNoteOutboxRepository;
        }

        /// <summary>
        /// Returns an asset from external service within a transaction
        /// protected by an exclusive asset-level lock.
        /// </summary>
        /// <remarks>
        /// A preliminary no-tracking lookup identifies the asset.
        ///
        /// The service case and asset are reloaded after acquiring
        /// the asset lock.
        ///
        /// All state changes, audit records, ServiceDesk task outbox
        /// creation, and persistence operations execute inside
        /// the protected transaction.
        ///
        /// No external ServiceDesk API calls are made within
        /// this transaction.
        /// </remarks>
        /// <param name="serviceCaseId">
        /// The identifier of the service case.
        /// </param>
        /// <param name="request">
        /// The external service return request.
        /// </param>
        /// <param name="cancellationToken">
        /// A token used to cancel lookup, lock acquisition, and execution.
        /// </param>
        /// <returns>
        /// The return result after the transaction commits.
        /// </returns>
        /// <exception cref="ArgumentNullException">
        /// Thrown when the request is null.
        /// </exception>
        /// <exception cref="ArgumentException">
        /// Thrown when the service case identifier is empty.
        /// </exception>
        /// <exception cref="ServiceCaseNotFoundException">
        /// Thrown when the service case cannot be found.
        /// </exception>
        /// <exception cref="ServiceCaseStateConflictException">
        /// Thrown when the service case relationship or state is invalid.
        /// </exception>
        /// <exception cref="TimeoutException">
        /// Thrown when the asset lock cannot be acquired.
        /// </exception>
        public async Task<ReturnFromExternalServiceResponse> HandleAsync(Guid serviceCaseId, ReturnFromExternalServiceRequest request, CancellationToken cancellationToken = default)
        {
            ValidateServiceCaseId(serviceCaseId);
            ArgumentNullException.ThrowIfNull(request);

            var serviceCaseSnapshot = await _serviceCaseRepository.GetByIdAsync(serviceCaseId, cancellationToken)
                ?? throw new ServiceCaseNotFoundException(serviceCaseId);

            var assetId = serviceCaseSnapshot.AssetId;

            if (assetId == Guid.Empty)
            {
                throw new ServiceCaseStateConflictException( serviceCaseId, "The service case does not reference a valid asset.");
            }

            return await _assetOperationExecutor.ExecuteAsync<ReturnFromExternalServiceResponse>(
                    assetIds: new[] { assetId },
                    operation: token => HandleCoreAsync(
                        serviceCaseId,
                        assetId,
                        request,
                        token),
                    cancellationToken: cancellationToken);
        }

        private async Task<ReturnFromExternalServiceResponse> HandleCoreAsync(
            Guid serviceCaseId,
            Guid lockedAssetId,
            ReturnFromExternalServiceRequest request,
            CancellationToken cancellationToken)
        {
            ValidateServiceCaseId(serviceCaseId);
            ArgumentNullException.ThrowIfNull(request);

            var currentUser = await _currentApplicationUserResolver.ResolveAsync(cancellationToken);

            ServiceCaseAuthorization.EnsureCanManageAll(currentUser);

            var serviceCase = await _serviceCaseRepository.GetForUpdateByIdAsync(serviceCaseId, cancellationToken)
                ?? throw new ServiceCaseNotFoundException(serviceCaseId);

            if (serviceCase.AssetId != lockedAssetId)
            {
                throw new ServiceCaseStateConflictException(serviceCaseId,
                    "The asset associated with the service case changed before the operation acquired its lock.");
            }

            var asset = await _assetRepository.GetForUpdateByIdAsync(lockedAssetId, cancellationToken)
                ?? throw new AssetNotFoundException(lockedAssetId);

            ValidateConsistentState(serviceCase, asset);

            var previousServiceCaseStatus = serviceCase.Status;
            var previousAssetStatus = asset.AssetStatus;
            var returnedAtUtc = DateTime.UtcNow;

            // Update the service case and asset.
            serviceCase.ReturnFromExternalService();
            asset.ReturnFromExternalService();

            // Create the local service case event.
            var serviceCaseEvent = ServiceCaseEvent.Create(
                serviceCaseId: serviceCase.Id,
                type: ServiceCaseEventType.ReturnedFromExternalService,
                previousStatus: previousServiceCaseStatus,
                newStatus: serviceCase.Status,
                comment: request.Comment,
                performedByUserId: currentUser.Id,
                performedByUserName: currentUser.DisplayName,
                occurredAtUtc: returnedAtUtc);

            // Create the asset status history.
            var assetHistory = new AssetHistory(
                assetId: asset.Id,
                action: nameof(Asset.ReturnFromExternalService),
                propertyName: nameof(Asset.AssetStatus),
                oldValue: previousAssetStatus.ToString(),
                newValue: asset.AssetStatus.ToString(),
                changedByUserId: currentUser.Id,
                changedByUserName: currentUser.DisplayName,
                changedAtUtc: returnedAtUtc);

            await _serviceCaseRepository.AddEventAsync(serviceCaseEvent, cancellationToken);

            // Build the Hungarian HTML description.
            var noteDescription = BuildServiceDeskNoteDescription(
                serviceCase,
                serviceCaseEvent,
                asset);

            // Register the ServiceDesk request note.
            // External synchronization is handled by the background processor.
            var noteOutbox = ServiceDeskNoteOutbox.Create(
                serviceCaseId: serviceCase.Id,
                serviceCaseEventId: serviceCaseEvent.Id,
                description: noteDescription,
                createdAtUtc: returnedAtUtc);

            await _noteOutboxRepository.AddAsync(noteOutbox, cancellationToken);

            // Persist the asset history.
            await _assetHistoryRepository.AddRangeAsync([assetHistory], cancellationToken);

            // Persist all changes within the protected transaction.
            await _serviceCaseRepository.SaveChangesAsync(cancellationToken);

            return CreateResponse(serviceCase, asset, currentUser, returnedAtUtc);
        }

        private static ReturnFromExternalServiceResponse CreateResponse(
            ServiceCase serviceCase,
            Asset asset,
            ApplicationUser currentUser,
            DateTime returnedAtUtc)
        {
            ArgumentNullException.ThrowIfNull(serviceCase);
            ArgumentNullException.ThrowIfNull(asset);
            ArgumentNullException.ThrowIfNull(currentUser);

            if (!serviceCase.VendorId.HasValue || serviceCase.VendorId.Value == Guid.Empty)
            {
                throw new InvalidOperationException("The service case does not have a valid assigned vendor.");
            }

            return new ReturnFromExternalServiceResponse(
                ServiceCaseId: serviceCase.Id,
                CaseNumber: serviceCase.CaseNumber,
                AssetId: asset.Id,
                ServiceCaseStatus: serviceCase.Status,
                AssetStatus: asset.AssetStatus,
                VendorId: serviceCase.VendorId.Value,
                ReturnedAtUtc: returnedAtUtc,
                ReturnedByUserId: currentUser.Id,
                ReturnedByUserName: currentUser.DisplayName);
        }

        private static void ValidateServiceCaseId(Guid serviceCaseId)
        {
            if (serviceCaseId == Guid.Empty)
            {
                throw new ArgumentException("The service case identifier cannot be empty.", nameof(serviceCaseId));
            }
        }

        private static void ValidateConsistentState(ServiceCase serviceCase, Asset asset)
        {
            ArgumentNullException.ThrowIfNull(serviceCase);
            ArgumentNullException.ThrowIfNull(asset);

            if (serviceCase.AssetId != asset.Id)
            {
                throw new ServiceCaseStateConflictException(serviceCase.Id, "The service case and asset relationship is invalid.");
            }

            if (serviceCase.Status != ServiceCaseStatus.InExternalService)
            {
                throw new ServiceCaseStateConflictException(serviceCase.Id,
                    "Only a service case currently in external service can be returned from external service.");
            }

            if (!serviceCase.VendorId.HasValue || serviceCase.VendorId.Value == Guid.Empty)
            {
                throw new ServiceCaseStateConflictException(serviceCase.Id,
                    "A valid vendor must be assigned before the return from external service can be recorded.");
            }

            if (asset.AssetStatus != AssetStatus.In_Service)
            {
                throw new ServiceCaseStateConflictException(serviceCase.Id,
                    "Only an asset currently in service can be returned from external service.");
            }
        }

    /// <summary>
    /// Builds the Hungarian HTML description for
    /// returning an asset from external service.
    /// </summary>
    private static string BuildServiceDeskNoteDescription(ServiceCase serviceCase, ServiceCaseEvent serviceCaseEvent, Asset asset)
    {
        ArgumentNullException.ThrowIfNull(serviceCase);
        ArgumentNullException.ThrowIfNull(serviceCaseEvent);
        ArgumentNullException.ThrowIfNull(asset);

        static string Encode(string? value)
        {
            return System.Net.WebUtility.HtmlEncode(string.IsNullOrWhiteSpace(value) ? "Nincs megadva" : value);
        }

        var comment = Encode(serviceCaseEvent.Comment)
            .Replace("\r\n", "\n")
            .Replace("\n", "<br>");

        return $"""
            <div>
                <h3>Visszaérkezett a külső szervizből</h3>

                <p>
                    <strong>Szervizügy:</strong>
                    {Encode(serviceCase.CaseNumber)}
                </p>

                <p>
                    <strong>Eszköz neve:</strong>
                    {Encode(asset.AssetName)}
                </p>

                <p>
                    <strong>Gyári szám:</strong>
                    {Encode(asset.SerialNumber)}
                </p>

                <p>
                    <strong>Korábbi állapot:</strong>
                    {Encode(serviceCaseEvent.PreviousStatus?.ToString())}
                </p>

                <p>
                    <strong>Új állapot:</strong>
                    {Encode(serviceCaseEvent.NewStatus.ToString())}
                </p>

                <p>
                    <strong>Műveletet végrehajtotta:</strong>
                    {Encode(serviceCaseEvent.PerformedByUserName)}
                </p>

                <p>
                    <strong>Megjegyzés:</strong><br>
                    {comment}
                </p>
            </div>
            """;
    }
    }
}