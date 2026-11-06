using AssetManagement.Application.Common.Interfaces.AssetHistories;
using AssetManagement.Application.Common.Interfaces.Assets;
using AssetManagement.Application.Common.Interfaces.Packages;
using AssetManagement.Application.Common.Interfaces.ServiceCases;
using AssetManagement.Application.Common.Interfaces.Users;
using AssetManagement.Domain.Entities.AssetHistories;
using AssetManagement.Domain.Entities.Assets;
using AssetManagement.Domain.Entities.Users;

namespace AssetManagement.Application.Assets.DisposeAsset{

    /// <summary>
    /// Handles standalone asset disposal requests.
    /// </summary>
    /// <remarks>
    /// Only active application administrators can dispose of assets.
    /// The asset domain model validates the permitted source statuses
    /// and user assignment restrictions.
    ///
    /// Assets with an active service case must be disposed of through
    /// the service case workflow instead of this standalone operation.
    ///
    /// The status change and disposal reason are recorded as separate
    /// asset history entries with the same timestamp and performing user.
    ///
    /// All repositories must share the same scoped persistence context
    /// so that the asset and its history entries are saved together.
    /// </remarks>
    public sealed class DisposeAssetHandler
    {
        private const int MaximumReasonLength = 2000;
        private const string DisposalReasonPropertyName = "DisposalReason";

        private readonly IAssetRepository _assetRepository;
        private readonly IAssetHistoryRepository _assetHistoryRepository;
        private readonly IServiceCaseRepository _serviceCaseRepository;
        private readonly ICurrentApplicationUserResolver _currentApplicationUserResolver;
        private readonly IPackageRepository _packageRepository;
        private readonly IAssetOperationExecutor _assetOperationExecutor;

        /// <summary>
        /// Initializes a new instance of the
        /// <see cref="DisposeAssetHandler"/> class.
        /// </summary>
        /// <param name="assetRepository">
        /// The repository used to load and persist the asset.
        /// </param>
        /// <param name="assetHistoryRepository">
        /// The repository used to record the disposal audit entries.
        /// </param>
        /// <param name="serviceCaseRepository">
        /// The repository used to check for an active service case.
        /// </param>
        /// <param name="currentApplicationUserResolver">
        /// The resolver used to identify the performing application user.
        /// </param>
        /// <param name="packageRepository">
        /// The repository used to verify that the asset is not part of a package.
        /// </param>
        /// <param name="assetOperationExecutor">
        /// The executor used to perform the asset disposal operation within a transaction.
        /// </param>
        /// <exception cref="ArgumentNullException">
        /// A required dependency is null.
        /// </exception>
        public DisposeAssetHandler(
            IAssetRepository assetRepository,
            IAssetHistoryRepository assetHistoryRepository,
            IServiceCaseRepository serviceCaseRepository,
            ICurrentApplicationUserResolver currentApplicationUserResolver,
            IPackageRepository packageRepository,
            IAssetOperationExecutor assetOperationExecutor)
        {
            ArgumentNullException.ThrowIfNull(assetRepository);
            ArgumentNullException.ThrowIfNull(assetHistoryRepository);
            ArgumentNullException.ThrowIfNull(serviceCaseRepository);
            ArgumentNullException.ThrowIfNull(currentApplicationUserResolver);
            ArgumentNullException.ThrowIfNull(packageRepository);
            ArgumentNullException.ThrowIfNull(assetOperationExecutor);

            _assetRepository = assetRepository;
            _assetHistoryRepository = assetHistoryRepository;
            _serviceCaseRepository = serviceCaseRepository;
            _currentApplicationUserResolver = currentApplicationUserResolver;
            _packageRepository = packageRepository;
            _assetOperationExecutor = assetOperationExecutor;
        }

        /// <summary>
        /// Disposes of an asset within a transaction protected
        /// by an exclusive asset-level lock.
        /// </summary>
        /// <remarks>
        /// Request values are validated before acquiring the lock.
        ///
        /// User resolution, authorization, asset loading, active package
        /// and service case checks, disposal, and audit persistence
        /// are performed inside the protected operation.
        ///
        /// The core operation saves its changes. The executor commits
        /// the transaction before returning the response.
        /// </remarks>
        /// <param name="assetId">
        /// The identifier of the asset to dispose of.
        /// </param>
        /// <param name="request">
        /// The request containing the mandatory disposal reason.
        /// </param>
        /// <param name="cancellationToken">
        /// A token used to cancel lock acquisition and execution.
        /// </param>
        /// <returns>
        /// The disposal result after the transaction has been committed.
        /// </returns>
        /// <exception cref="ArgumentNullException">
        /// Thrown when the request is null.
        /// </exception>
        /// <exception cref="ArgumentException">
        /// Thrown when the asset identifier or disposal reason is invalid.
        /// </exception>
        /// <exception cref="AssetNotFoundException">
        /// Thrown when the requested asset does not exist.
        /// </exception>
        /// <exception cref="AssetDisposalAccessDeniedException">
        /// Thrown when the current user is not an active
        /// application administrator.
        /// </exception>
        /// <exception cref="AssetDisposalStateConflictException">
        /// Thrown when the asset belongs to an active package,
        /// has an active service case, or cannot be disposed of
        /// in its current state.
        /// </exception>
        /// <exception cref="TimeoutException">
        /// Thrown when the asset lock cannot be acquired
        /// within the configured timeout.
        /// </exception>
        /// <exception cref="OperationCanceledException">
        /// Thrown when the operation is canceled.
        /// </exception>
        /// <exception cref="InvalidOperationException">
        /// Thrown when the execution context does not support
        /// the protected operation.
        /// </exception>
        public Task<DisposeAssetResponse> HandleAsync(
            Guid assetId,
            DisposeAssetRequest request,
            CancellationToken cancellationToken = default)
        {
            ValidateAssetId(assetId);
            ArgumentNullException.ThrowIfNull(request);

            // Reject invalid input before opening a transaction.
            _ = ValidateAndNormalizeReason(request.Reason);

            return _assetOperationExecutor.ExecuteAsync<DisposeAssetResponse>(
                assetIds: new[] { assetId },
                operation: token => HandleCoreAsync(assetId, request, token),
                cancellationToken: cancellationToken);
        }

        /// <summary>
        /// Disposes of an asset and records the disposal audit entries.
        /// </summary>
        /// <param name="assetId">
        /// The unique identifier of the asset to dispose of.
        /// </param>
        /// <param name="request">
        /// The request containing the mandatory disposal reason.
        /// </param>
        /// <param name="cancellationToken">
        /// The token used to cancel the operation.
        /// </param>
        /// <returns>
        /// The disposed asset status, recorded reason, disposal timestamp,
        /// and performing administrator.
        /// </returns>
        /// <exception cref="ArgumentNullException">
        /// The request is null.
        /// </exception>
        /// <exception cref="ArgumentException">
        /// The asset identifier is empty, or the disposal reason is empty
        /// or exceeds the maximum permitted length after trimming.
        /// </exception>
        /// <exception cref="AssetNotFoundException">
        /// The requested asset does not exist.
        /// </exception>
        /// <exception cref="AssetDisposalAccessDeniedException">
        /// The current user is inactive or is not an application administrator.
        /// </exception>
        /// <exception cref="AssetDisposalStateConflictException">
        /// The asset has an active service case, has already been disposed of,
        /// is still assigned to a user, or its status does not allow disposal.
        /// </exception>
        /// <exception cref="OperationCanceledException">
        /// The operation was canceled.
        /// </exception>
        public async Task<DisposeAssetResponse> HandleCoreAsync(
            Guid assetId,
            DisposeAssetRequest request,
            CancellationToken cancellationToken = default)
        {
            ValidateAssetId(assetId);
            ArgumentNullException.ThrowIfNull(request);

            var reason = ValidateAndNormalizeReason(request.Reason);
            var currentUser = await _currentApplicationUserResolver.ResolveAsync(cancellationToken);

            EnsureCanDispose(currentUser);

            var asset = await _assetRepository.GetForUpdateByIdAsync(assetId, cancellationToken)
                ?? throw new AssetNotFoundException(assetId);

            var hasActivePackage = await _packageRepository.HasActivePackageForAssetAsync(asset.Id, cancellationToken);

            if (hasActivePackage)
            {
                throw new AssetDisposalStateConflictException(
                    "An asset included in an active package cannot be disposed of. " +
                    "Remove it from the package or cancel the package before dispatch. " +
                    "For a package in transit, receive the package first.");
            }

            var hasActiveServiceCase = await _serviceCaseRepository.HasActiveServiceCaseForAssetAsync(asset.Id, cancellationToken);

            if (hasActiveServiceCase)
            {
                throw new AssetDisposalStateConflictException(
                    "An asset with an active service case must be disposed of through the service case workflow.");
            }

            var previousStatus = asset.AssetStatus;

            try
            {
                asset.Dispose();
            }
            catch (InvalidOperationException exception)
            {
                throw new AssetDisposalStateConflictException(exception.Message, exception);
            }

            var disposedAtUtc = DateTime.UtcNow;

            var histories = CreateHistories(
                asset,
                previousStatus,
                reason,
                currentUser,
                disposedAtUtc);

            await _assetHistoryRepository.AddRangeAsync(histories, cancellationToken);

            // Persist the asset and both audit entries in a single save.
            await _assetRepository.SaveChangesAsync(cancellationToken);

            return new DisposeAssetResponse(
                AssetId: asset.Id,
                AssetName: asset.AssetName,
                AssetStatus: asset.AssetStatus,
                Reason: reason,
                DisposedAtUtc: disposedAtUtc,
                DisposedByUserId: currentUser.Id,
                DisposedByUserName: currentUser.DisplayName);
        }

        /// <summary>
        /// Validates the asset identifier.
        /// </summary>
        /// <param name="assetId">
        /// The asset identifier to validate.
        /// </param>
        /// <exception cref="ArgumentException">
        /// The asset identifier is empty.
        /// </exception>
        private static void ValidateAssetId(Guid assetId)
        {
            if (assetId == Guid.Empty)
            {
                throw new ArgumentException("The asset identifier cannot be empty.", nameof(assetId));
            }
        }

        /// <summary>
        /// Validates and trims the mandatory disposal reason.
        /// </summary>
        /// <param name="reason">
        /// The disposal reason supplied by the client.
        /// </param>
        /// <returns>
        /// The validated and trimmed disposal reason.
        /// </returns>
        /// <exception cref="ArgumentException">
        /// The reason is null, empty, whitespace, or exceeds 2000 characters
        /// after trimming.
        /// </exception>
        private static string ValidateAndNormalizeReason(string? reason)
        {
            if (string.IsNullOrWhiteSpace(reason))
            {
                throw new ArgumentException("A disposal reason is required.", nameof(reason));
            }

            var normalizedReason = reason.Trim();

            if (normalizedReason.Length > MaximumReasonLength)
            {
                throw new ArgumentException(
                    $"The disposal reason cannot exceed {MaximumReasonLength} characters.", nameof(reason));
            }

            return normalizedReason;
        }

        /// <summary>
        /// Ensures that the performing user is an active application administrator.
        /// </summary>
        /// <param name="currentUser">
        /// The application user performing the operation.
        /// </param>
        /// <exception cref="ArgumentNullException">
        /// The application user is null.
        /// </exception>
        /// <exception cref="AssetDisposalAccessDeniedException">
        /// The user is inactive or is not an application administrator.
        /// </exception>
        private static void EnsureCanDispose(ApplicationUser currentUser)
        {
            ArgumentNullException.ThrowIfNull(currentUser);

            if (!currentUser.IsActive || currentUser.Role != ApplicationRole.ApplicationAdministrator)
            {
                throw new AssetDisposalAccessDeniedException();
            }
        }

        /// <summary>
        /// Creates audit entries for the status change and disposal reason.
        /// </summary>
        /// <param name="asset">
        /// The asset after disposal.
        /// </param>
        /// <param name="previousStatus">
        /// The asset status before disposal.
        /// </param>
        /// <param name="reason">
        /// The validated disposal reason.
        /// </param>
        /// <param name="performedBy">
        /// The administrator who performed the disposal.
        /// </param>
        /// <param name="disposedAtUtc">
        /// The UTC timestamp shared by both audit entries.
        /// </param>
        /// <returns>
        /// The status change entry and the disposal reason entry.
        /// </returns>
        /// <remarks>
        /// DisposalReason is an audit metadata key, not an Asset property.
        /// The reason is stored separately to preserve the existing
        /// status history format.
        /// </remarks>
        private static IReadOnlyList<AssetHistory> CreateHistories(
            Asset asset,
            AssetStatus previousStatus,
            string reason,
            ApplicationUser performedBy,
            DateTime disposedAtUtc)
        {
            return
            [
                new AssetHistory(
                    assetId: asset.Id,
                    action: nameof(Asset.Dispose),
                    propertyName: nameof(Asset.AssetStatus),
                    oldValue: previousStatus.ToString(),
                    newValue: asset.AssetStatus.ToString(),
                    changedByUserId: performedBy.Id,
                    changedByUserName: performedBy.DisplayName,
                    changedAtUtc: disposedAtUtc),

                new AssetHistory(
                    assetId: asset.Id,
                    action: nameof(Asset.Dispose),
                    propertyName: DisposalReasonPropertyName,
                    oldValue: null,
                    newValue: reason,
                    changedByUserId: performedBy.Id,
                    changedByUserName: performedBy.DisplayName,
                    changedAtUtc: disposedAtUtc)
            ];
        }
    }
}