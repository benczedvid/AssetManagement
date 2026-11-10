using AssetManagement.Application.Assets;
using AssetManagement.Application.Common.Authorization;
using AssetManagement.Application.Common.Interfaces.Assets;
using AssetManagement.Application.Common.Interfaces.ServiceCases;
using AssetManagement.Application.Common.Interfaces.ServiceDesk;
using AssetManagement.Application.Common.Interfaces.Users;
using AssetManagement.Application.ServiceCases.Exceptions;
using AssetManagement.Domain.Entities.Assets;
using AssetManagement.Domain.Entities.ServiceCases;
using AssetManagement.Domain.Entities.ServiceDesk;

namespace AssetManagement.Application.ServiceCases.ResolveAfterExternalServiceReturn;

public sealed class ResolveAfterExternalServiceReturnHandler
{
    private readonly IServiceCaseRepository _serviceCaseRepository;
    private readonly IAssetRepository _assetRepository;
    private readonly ICurrentApplicationUserResolver _currentApplicationUserResolver;
    private readonly IAssetOperationExecutor _assetOperationExecutor;
    private readonly IServiceDeskNoteOutboxRepository _noteOutboxRepository;

    public ResolveAfterExternalServiceReturnHandler(
        IServiceCaseRepository serviceCaseRepository,
        IAssetRepository assetRepository,
        ICurrentApplicationUserResolver currentApplicationUserResolver,
        IAssetOperationExecutor assetOperationExecutor,
        IServiceDeskNoteOutboxRepository serviceDeskNoteOutboxRepository)
    {
        ArgumentNullException.ThrowIfNull(serviceCaseRepository);
        ArgumentNullException.ThrowIfNull(assetRepository);
        ArgumentNullException.ThrowIfNull(currentApplicationUserResolver);
        ArgumentNullException.ThrowIfNull(assetOperationExecutor);
        ArgumentNullException.ThrowIfNull(serviceDeskNoteOutboxRepository);

        _serviceCaseRepository = serviceCaseRepository;
        _assetRepository = assetRepository;
        _currentApplicationUserResolver = currentApplicationUserResolver;
        _assetOperationExecutor = assetOperationExecutor;
        _noteOutboxRepository = serviceDeskNoteOutboxRepository;
    }

    /// <summary>
    /// Resolves a headquarters-origin service case after external service
    /// within a transaction protected by an exclusive asset-level lock.
    /// </summary>
    /// <remarks>
    /// A preliminary no-tracking lookup identifies the asset to lock.
    ///
    /// The service case is reloaded inside the protected operation.
    /// Authorization, state validation, resolution, event creation,
    /// ServiceDesk task outbox creation, and persistence are performed
    /// after acquiring the asset lock.
    ///
    /// No external ServiceDesk API calls are made inside the transaction.
    /// </remarks>
    /// <param name="serviceCaseId">
    /// The identifier of the service case.
    /// </param>
    /// <param name="request">
    /// The service case resolution request.
    /// </param>
    /// <param name="cancellationToken">
    /// A token used to cancel lookup, lock acquisition, and execution.
    /// </param>
    /// <returns>
    /// The resolved service case information after the transaction commits.
    /// </returns>
    /// <exception cref="ArgumentNullException">
    /// Thrown when the request is null.
    /// </exception>
    /// <exception cref="ArgumentException">
    /// Thrown when the service case identifier or comment is invalid.
    /// </exception>
    /// <exception cref="ServiceCaseNotFoundException">
    /// Thrown when the service case cannot be found.
    /// </exception>
    /// <exception cref="ServiceCaseStateConflictException">
    /// Thrown when the service case relationship or state is invalid.
    /// </exception>
    /// <exception cref="TimeoutException">
    /// Thrown when the asset lock cannot be acquired within the timeout.
    /// </exception>
    public async Task<ResolveAfterExternalServiceReturnResponse> HandleAsync(Guid serviceCaseId, ResolveAfterExternalServiceReturnRequest request, CancellationToken cancellationToken = default)
    {
        ValidateServiceCaseId(serviceCaseId);
        ArgumentNullException.ThrowIfNull(request);
        ValidateComment(request.Comment);

        // The preliminary lookup must remain no-tracking.
        var serviceCaseSnapshot = await _serviceCaseRepository.GetByIdAsync(serviceCaseId, cancellationToken)
            ?? throw new ServiceCaseNotFoundException(serviceCaseId);

        var assetId = serviceCaseSnapshot.AssetId;

        if (assetId == Guid.Empty)
        {
            throw new ServiceCaseStateConflictException(serviceCaseId, "The service case does not reference a valid asset.");
        }

        return await _assetOperationExecutor.ExecuteAsync<ResolveAfterExternalServiceReturnResponse>(
                assetIds: new[] { assetId },
                operation: token => HandleCoreAsync(
                    serviceCaseId,
                    assetId,
                    request,
                    token),
                cancellationToken: cancellationToken);
    }

    private async Task<ResolveAfterExternalServiceReturnResponse> HandleCoreAsync(
        Guid serviceCaseId,
        Guid lockedAssetId,
        ResolveAfterExternalServiceReturnRequest request,
        CancellationToken cancellationToken = default)
    {
        ValidateServiceCaseId(serviceCaseId);
        ArgumentNullException.ThrowIfNull(request);
        ValidateComment(request.Comment);

        var currentUser = await _currentApplicationUserResolver.ResolveAsync(cancellationToken);

        ServiceCaseAuthorization.EnsureCanManageAll(currentUser);

        var serviceCase = await _serviceCaseRepository.GetForUpdateByIdAsync(serviceCaseId, cancellationToken)
            ?? throw new ServiceCaseNotFoundException(serviceCaseId);

        if (serviceCase.AssetId != lockedAssetId)
        {
            throw new ServiceCaseStateConflictException(serviceCaseId,
                "The asset associated with the service case changed before the operation acquired its lock.");
        }

        var asset = await _assetRepository.GetByIdAsync(lockedAssetId, cancellationToken)
            ?? throw new AssetNotFoundException(lockedAssetId);

        ValidateConsistentState(serviceCase, asset);

        var vendorId = serviceCase.VendorId
            ?? throw new InvalidOperationException("The service case does not have an assigned vendor.");

        var previousServiceCaseStatus = serviceCase.Status;
        var resolvedAtUtc = DateTime.UtcNow;

        // Resolve the local service case.
        serviceCase.ResolveAfterExternalServiceReturn();

        // Create the local service case event.
        var serviceCaseEvent = ServiceCaseEvent.Create(
            serviceCaseId: serviceCase.Id,
            type: ServiceCaseEventType.Resolved,
            previousStatus: previousServiceCaseStatus,
            newStatus: serviceCase.Status,
            comment: request.Comment,
            performedByUserId: currentUser.Id,
            performedByUserName: currentUser.DisplayName,
            occurredAtUtc: resolvedAtUtc);

        // Create the response.
        var response = new ResolveAfterExternalServiceReturnResponse(
            ServiceCaseId: serviceCase.Id,
            CaseNumber: serviceCase.CaseNumber,
            AssetId: asset.Id,
            ServiceCaseStatus: serviceCase.Status,
            AssetStatus: asset.AssetStatus,
            VendorId: vendorId,
            ResolvedAtUtc: resolvedAtUtc,
            ResolvedByUserId: currentUser.Id,
            ResolvedByUserName: currentUser.DisplayName);

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
            createdAtUtc: resolvedAtUtc);

        await _noteOutboxRepository.AddAsync(noteOutbox, cancellationToken);

        // Persist all changes within the protected transaction.
        await _serviceCaseRepository.SaveChangesAsync(cancellationToken);

        return response;
    }

    private static void ValidateServiceCaseId(Guid serviceCaseId)
    {
        if (serviceCaseId == Guid.Empty)
        {
            throw new ArgumentException("The service case identifier cannot be empty.", nameof(serviceCaseId));
        }
    }

    private static void ValidateComment(string? comment)
    {
        if (comment is null)
        {
            return;
        }

        if (string.IsNullOrWhiteSpace(comment))
        {
            throw new ArgumentException("Comment cannot be empty or whitespace.", nameof(comment));
        }

        if (comment.Length > 2000)
        {
            throw new ArgumentException("Comment cannot exceed 2000 characters.", nameof(comment));
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

        if (serviceCase.Status != ServiceCaseStatus.AwaitingReturn)
        {
            throw new ServiceCaseStateConflictException(serviceCase.Id,
                "Only a service case awaiting return can be resolved after external service.");
        }

        if (serviceCase.OriginStoreId.HasValue)
        {
            throw new ServiceCaseStateConflictException(serviceCase.Id,
                "A service case opened from a store must be returned to its origin store before it can be resolved.");
        }

        if (!serviceCase.VendorId.HasValue || serviceCase.VendorId.Value == Guid.Empty)
        {
            throw new ServiceCaseStateConflictException(serviceCase.Id,
                "A valid vendor must be assigned before the service case can be resolved after external service.");
        }

        if (asset.AssetStatus != AssetStatus.In_HQ)
        {
            throw new ServiceCaseStateConflictException(serviceCase.Id,
                "Only an asset located at headquarters can have its service case resolved after external service return.");
        }
    }
    /// <summary>
    /// Builds the Hungarian HTML description for resolving
    /// a service case after external service return.
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
                <h3>Szervizügy megoldva</h3>

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