using AssetManagement.Application.Common.Authorization;
using AssetManagement.Application.Common.Interfaces.AssetHistories;
using AssetManagement.Application.Common.Interfaces.Assets;
using AssetManagement.Application.Common.Interfaces.Packages;
using AssetManagement.Application.Common.Interfaces.ServiceCases;
using AssetManagement.Application.Common.Interfaces.ServiceDesk;
using AssetManagement.Application.Common.Interfaces.Stores;
using AssetManagement.Application.Common.Interfaces.Users;
using AssetManagement.Application.Packages.Exceptions;
using AssetManagement.Domain.Entities.AssetHistories;
using AssetManagement.Domain.Entities.Assets;
using AssetManagement.Domain.Entities.Packages;
using AssetManagement.Domain.Entities.ServiceCases;
using AssetManagement.Domain.Entities.ServiceDesk;
using AssetManagement.Domain.Entities.Stores;

namespace AssetManagement.Application.Packages.DispatchPackage;

/// <summary>
/// Handles package dispatch requests.
/// </summary>
public sealed class DispatchPackageHandler
{
    private const string HeadquartersStoreNumber = "999";
    private const int CommentMaxLength = 2000;

    private readonly IPackageRepository _packageRepository;
    private readonly IAssetRepository _assetRepository;
    private readonly IStoreRepository _storeRepository;
    private readonly IAssetHistoryRepository _assetHistoryRepository;
    private readonly ICurrentApplicationUserResolver _currentApplicationUserResolver;
    private readonly IServiceCaseRepository _serviceCaseRepository;
    private readonly IAssetOperationExecutor _assetOperationExecutor;
    private readonly IServiceDeskNoteOutboxRepository _noteOutboxRepository;

    /// <summary>
    /// Initializes a new instance of the
    /// <see cref="DispatchPackageHandler"/> class.
    /// </summary>
    /// <param name="packageRepository">
    /// The package repository.
    /// </param>
    /// <param name="assetRepository">
    /// The asset repository.
    /// </param>
    /// <param name="storeRepository">
    /// The store repository.
    /// </param>
    /// <param name="assetHistoryRepository">
    /// The asset history repository.
    /// </param>
    /// <param name="currentApplicationUserResolver">
    /// Resolves the current application user.
    /// </param>    
    /// <param name="serviceCaseRepository">
    /// The service case repository.
    /// </param>
    /// <param name="assetOperationExecutor">
    /// Executes asset-related operations within a transaction protected by exclusive asset-level locks.
    /// </param>
    /// <param name="serviceDeskNoteOutboxRepository"></param>
    /// <exception cref="ArgumentNullException">
    /// Thrown when a required dependency is null.
    /// </exception>
    public DispatchPackageHandler(
        IPackageRepository packageRepository,
        IAssetRepository assetRepository,
        IStoreRepository storeRepository,
        IAssetHistoryRepository assetHistoryRepository,
        ICurrentApplicationUserResolver currentApplicationUserResolver,
        IServiceCaseRepository serviceCaseRepository,
        IAssetOperationExecutor assetOperationExecutor,
        IServiceDeskNoteOutboxRepository serviceDeskNoteOutboxRepository)
    {
        ArgumentNullException.ThrowIfNull(packageRepository);
        ArgumentNullException.ThrowIfNull(assetRepository);
        ArgumentNullException.ThrowIfNull(storeRepository);
        ArgumentNullException.ThrowIfNull(assetHistoryRepository);
        ArgumentNullException.ThrowIfNull(currentApplicationUserResolver);
        ArgumentNullException.ThrowIfNull(serviceCaseRepository);
        ArgumentNullException.ThrowIfNull(assetOperationExecutor);
        ArgumentNullException.ThrowIfNull(serviceDeskNoteOutboxRepository);

        _packageRepository = packageRepository;
        _assetRepository = assetRepository;
        _storeRepository = storeRepository;
        _assetHistoryRepository = assetHistoryRepository;
        _currentApplicationUserResolver = currentApplicationUserResolver;
        _serviceCaseRepository = serviceCaseRepository;
        _assetOperationExecutor = assetOperationExecutor;
        _noteOutboxRepository = serviceDeskNoteOutboxRepository;
    }

    /// <summary>
    /// Dispatches a package within a transaction protected by a package lock
    /// followed by exclusive locks for all included assets.
    /// </summary>
    /// <remarks>
    /// Asset identifiers are loaded using a no-tracking query after
    /// the package lock has been acquired.
    ///
    /// The core operation reloads and validates the package,
    /// updates the assets and optional service cases, and saves
    /// all changes before the executor commits the transaction.
    /// </remarks>
    /// <param name="packageId">
    /// The identifier of the package to dispatch.
    /// </param>
    /// <param name="request">
    /// The package dispatch request.
    /// </param>
    /// <param name="cancellationToken">
    /// A token used to cancel lookup, lock acquisition, and execution.
    /// </param>
    /// <returns>
    /// The dispatch result after the transaction has been committed.
    /// </returns>
    /// <exception cref="ArgumentNullException">
    /// Thrown when the request is null.
    /// </exception>
    /// <exception cref="ArgumentException">
    /// Thrown when the package identifier or comment is invalid.
    /// </exception>
    /// <exception cref="PackageNotFoundException">
    /// Thrown when the package cannot be found.
    /// </exception>
    /// <exception cref="PackageStateConflictException">
    /// Thrown when the package contents or business state
    /// do not allow dispatch.
    /// </exception>
    /// <exception cref="PackageAccessDeniedException">
    /// Thrown when the current user cannot dispatch the package.
    /// </exception>
    /// <exception cref="TimeoutException">
    /// Thrown when a required lock cannot be acquired within the timeout.
    /// </exception>
    /// <exception cref="OperationCanceledException">
    /// Thrown when the operation is canceled.
    /// </exception>
    /// <exception cref="InvalidOperationException">
    /// Thrown when the execution context does not support
    /// the protected operation.
    /// </exception>
    public Task<DispatchPackageResponse> HandleAsync(
        Guid packageId,
        DispatchPackageRequest request,
        CancellationToken cancellationToken = default)
    {
        ValidatePackageId(packageId);
        ArgumentNullException.ThrowIfNull(request);
        ValidateComment(request.Comment);

        return _assetOperationExecutor
            .ExecutePackageAsync<DispatchPackageResponse>(
                packageId: packageId,
                loadAssetIds: token =>
                    LoadPackageAssetIdsAsync(packageId, token),
                operation: (lockedAssetIds, token) =>
                    HandleCoreAsync(
                        packageId,
                        request,
                        lockedAssetIds,
                        token),
                cancellationToken: cancellationToken);
    }

    /// <summary>
    /// Loads the current package asset identifiers without tracking.
    /// </summary>
    /// <remarks>
    /// Called after acquiring the package lock and before acquiring
    /// asset locks. This method does not modify or save data.
    /// </remarks>
    /// <param name="packageId">
    /// The identifier of the protected package.
    /// </param>
    /// <param name="cancellationToken">
    /// A token used to cancel the lookup.
    /// </param>
    /// <returns>
    /// The distinct identifiers of the assets included in the package.
    /// </returns>
    /// <exception cref="PackageNotFoundException">
    /// Thrown when the package cannot be found.
    /// </exception>
    /// <exception cref="PackageStateConflictException">
    /// Thrown when a package item contains an empty asset identifier.
    /// </exception>
    private async Task<IReadOnlyCollection<Guid>> LoadPackageAssetIdsAsync(
        Guid packageId,
        CancellationToken cancellationToken)
    {
        var packageSnapshot =
            await _packageRepository.GetByIdAsync(
                packageId,
                cancellationToken)
            ?? throw new PackageNotFoundException(packageId);

        var assetIds = packageSnapshot.Items
            .Select(item => item.AssetId)
            .Distinct()
            .ToArray();

        if (assetIds.Any(assetId => assetId == Guid.Empty))
        {
            throw new PackageStateConflictException(
                "The package contains an invalid asset identifier.");
        }

        return assetIds;
    }

    /// <summary>
    /// Dispatches a created package from its source store.
    /// </summary>
    /// <param name="packageId">
    /// The unique identifier of the package.
    /// </param>
    /// <param name="request">
    /// The package dispatch request.
    /// </param>
    /// <param name="cancellationToken">
    /// A token used to cancel the asynchronous operation.
    /// </param>
    /// <returns>
    /// The dispatched package information.
    /// </returns>
    /// <exception cref="ArgumentNullException">
    /// Thrown when <paramref name="request"/> is null.
    /// </exception>
    /// <param name="lockedAssetIds">
    /// The identifiers of the assets protected by the executor.
    /// </param>
    /// <exception cref="ArgumentException">
    /// Thrown when the package identifier or comment is invalid.
    /// </exception>
    /// <exception cref="PackageNotFoundException">
    /// Thrown when the package cannot be found.
    /// </exception>
    /// <exception cref="PackageStoreNotFoundException">
    /// Thrown when the source or destination store cannot be found.
    /// </exception>
    /// <exception cref="PackageAssetNotFoundException">
    /// Thrown when an included asset cannot be found.
    /// </exception>
    /// <exception cref="PackageStateConflictException">
    /// Thrown when the package or an included asset is not
    /// in a valid state for dispatch.
    /// </exception>
    /// <exception cref="PackageAccessDeniedException">
    /// Thrown when the current user cannot dispatch the package.
    /// </exception>
    private async Task<DispatchPackageResponse> HandleCoreAsync(
        Guid packageId,
        DispatchPackageRequest request,
        IReadOnlyCollection<Guid> lockedAssetIds,
        CancellationToken cancellationToken = default)
    {
        ValidatePackageId(packageId);

        ArgumentNullException.ThrowIfNull(request);

        ValidateComment(request.Comment);

        var currentUser = await _currentApplicationUserResolver.ResolveAsync(cancellationToken);
        var package = await _packageRepository.GetForUpdateByIdAsync(packageId, cancellationToken)
            ?? throw new PackageNotFoundException(packageId);

        PackageAuthorization.EnsureCanManageAtSource(currentUser, package);
        ValidatePackageForDispatch(package);

        ValidateLockedPackageAssets(package, lockedAssetIds);

        var sourceStore = await _storeRepository.GetByIdAsync(package.SourceStoreId, cancellationToken)
            ?? throw new PackageStoreNotFoundException(package.SourceStoreId);

        var destinationStore = await _storeRepository.GetByIdAsync(package.DestinationStoreId, cancellationToken)
            ?? throw new PackageStoreNotFoundException(package.DestinationStoreId);

        ValidateRoute(sourceStore, destinationStore);

        var destinationIsHeadquarters = IsHeadquarters(destinationStore);
        var dispatchedAtUtc = DateTime.UtcNow;
        var assetResults = new List<DispatchPackageAssetResponse>(package.Items.Count);
        var assetHistories = new List<AssetHistory>(package.Items.Count);

        foreach (var packageItem in package.Items)
        {
            var asset = await _assetRepository.GetForUpdateByIdAsync(packageItem.AssetId, cancellationToken)
                ?? throw new PackageAssetNotFoundException(packageItem.AssetId);

            ValidateAssetForDispatch(
                asset,
                sourceStore,
                destinationStore,
                destinationIsHeadquarters);

            await UpdateServiceCaseForDispatchAsync(
                asset,
                sourceStore,
                destinationStore,
                destinationIsHeadquarters,
                request.Comment,
                currentUser.Id,
                currentUser.DisplayName,
                dispatchedAtUtc,
                cancellationToken);

            var previousStatus = asset.AssetStatus;

            DispatchAsset(asset, destinationIsHeadquarters);

            assetHistories.Add(
                new AssetHistory(
                    assetId: asset.Id,
                    action: ResolveAssetActionName(destinationIsHeadquarters),
                    propertyName: nameof(Asset.AssetStatus),
                    oldValue: previousStatus.ToString(),
                    newValue: asset.AssetStatus.ToString(),
                    changedByUserId: currentUser.Id,
                    changedByUserName: currentUser.DisplayName,
                    changedAtUtc: dispatchedAtUtc));

            assetResults.Add(
                new DispatchPackageAssetResponse(
                    AssetId: asset.Id,
                    PreviousStatus: previousStatus,
                    NewStatus: asset.AssetStatus));
        }

        try
        {
            package.Dispatch(
                comment: request.Comment,
                performedByUserId: currentUser.Id,
                performedByUserName: currentUser.DisplayName,
                dispatchedAtUtc: dispatchedAtUtc);
        }
        catch (InvalidOperationException exception)
        {
            throw new PackageStateConflictException(exception.Message);
        }

        await _assetHistoryRepository.AddRangeAsync(assetHistories, cancellationToken);
        await _packageRepository.SaveChangesAsync(cancellationToken);

        return new DispatchPackageResponse(
            PackageId: package.Id,
            PackageNumber: package.PackageNumber,
            PackageStatus: package.Status,
            SourceStoreId: package.SourceStoreId,
            DestinationStoreId: package.DestinationStoreId,
            CourierUserId: package.CourierUserId,
            Assets: assetResults,
            DispatchedAtUtc: dispatchedAtUtc,
            DispatchedByUserId: currentUser.Id,
            DispatchedByUserName: currentUser.DisplayName);
    }

    private static void DispatchAsset(Asset asset, bool destinationIsHeadquarters)
    {
        ArgumentNullException.ThrowIfNull(asset);

        try
        {
            if (destinationIsHeadquarters)
            {
                asset.SendToHeadquartersForServiceInspection();
                return;
            }

            asset.DispatchToStoreAfterService();
        }
        catch (InvalidOperationException exception)
        {
            throw new PackageStateConflictException(exception.Message);
        }
    }
    private async Task UpdateServiceCaseForDispatchAsync(
        Asset asset,
        Store sourceStore,
        Store destinationStore,
        bool destinationIsHeadquarters,
        string? comment,
        Guid performedByUserId,
        string performedByUserName,
        DateTime occurredAtUtc,
        CancellationToken cancellationToken)
    {
        var serviceCase = await _serviceCaseRepository.GetActiveForUpdateByAssetIdAsync(asset.Id, cancellationToken);

        if (serviceCase is null)
        {
            return;
        }

        // Dispatching to headquarters does not change the service case.
        if (destinationIsHeadquarters)
        {
            if (serviceCase.Status != ServiceCaseStatus.AwaitingCentralReceipt)
            {
                throw new PackageStateConflictException(
                    $"The active service case for asset '{asset.Id}' " +
                    "must be awaiting central receipt before dispatch " +
                    "to headquarters.");
            }

            if (serviceCase.OriginStoreId != sourceStore.Id)
            {
                throw new PackageStateConflictException(
                    $"The package source does not match the active service case origin store for asset '{asset.Id}'.");
            }

            return;
        }

        if (serviceCase.Status != ServiceCaseStatus.AwaitingReturn)
        {
            throw new PackageStateConflictException(
                $"The active service case for asset '{asset.Id}' must be awaiting return before dispatch to a store.");
        }

        if (serviceCase.OriginStoreId != destinationStore.Id)
        {
            throw new PackageStateConflictException(
                $"The package destination does not match the active service case origin store for asset '{asset.Id}'.");
        }

        var previousStatus = serviceCase.Status;

        try
        {
            serviceCase.DispatchToOriginStore();
        }
        catch (InvalidOperationException exception)
        {
            throw new PackageStateConflictException(exception.Message);
        }

        // Create the local service case event.
        var serviceCaseEvent = ServiceCaseEvent.Create(
            serviceCaseId: serviceCase.Id,
            type: ServiceCaseEventType.DispatchedToOriginStore,
            previousStatus: previousStatus,
            newStatus: serviceCase.Status,
            comment: comment,
            performedByUserId: performedByUserId,
            performedByUserName: performedByUserName,
            occurredAtUtc: occurredAtUtc);

        await _serviceCaseRepository.AddEventAsync(serviceCaseEvent, cancellationToken);

        // Build the Hungarian HTML description.
        var noteDescription = BuildServiceDeskNoteDescription(
            serviceCase,
            serviceCaseEvent,
            asset,
            destinationStore);

        // Register the ServiceDesk request note.
        // External synchronization is handled by the background processor.
        var noteOutbox = ServiceDeskNoteOutbox.Create(
            serviceCaseId: serviceCase.Id,
            serviceCaseEventId: serviceCaseEvent.Id,
            description: noteDescription,
            createdAtUtc: occurredAtUtc);

        await _noteOutboxRepository.AddAsync(noteOutbox, cancellationToken);
    }

    private static string ResolveAssetActionName(bool destinationIsHeadquarters)
    {
        return destinationIsHeadquarters
            ? nameof(Asset.SendToHeadquartersForServiceInspection)
            : nameof(Asset.DispatchToStoreAfterService);
    }

    private static void ValidatePackageForDispatch(Package package)
    {
        ArgumentNullException.ThrowIfNull(package);

        if (package.Status != PackageStatus.Created)
        {
            throw new PackageStateConflictException("Only created packages can be dispatched.");
        }

        if (package.Items.Count == 0)
        {
            throw new PackageStateConflictException("A package must contain at least one asset before dispatch.");
        }
    }

    private static void ValidateAssetForDispatch(
        Asset asset,
        Store sourceStore,
        Store destinationStore,
        bool destinationIsHeadquarters)
    {
        ArgumentNullException.ThrowIfNull(asset);
        ArgumentNullException.ThrowIfNull(sourceStore);
        ArgumentNullException.ThrowIfNull(destinationStore);

        ValidateAssetStoreAssignment(asset, sourceStore, destinationStore);

        if (!string.IsNullOrWhiteSpace(asset.AssignedEmployeeNumber))
        {
            throw new PackageStateConflictException($"Asset '{asset.Id}' must be returned by the assigned employee before dispatch.");
        }

        if (asset.AssignedUserId.HasValue)
        {
            throw new PackageStateConflictException($"Asset '{asset.Id}' must be unassigned from the application user before dispatch.");
        }

        if (asset.AssetStatus == AssetStatus.Disposed)
        {
            throw new PackageStateConflictException($"Disposed asset '{asset.Id}' cannot be dispatched.");
        }

        var expectedStatus = destinationIsHeadquarters
            ? AssetStatus.In_Store
            : AssetStatus.In_HQ;

        if (asset.AssetStatus != expectedStatus)
        {
            throw new PackageStateConflictException($"Asset '{asset.Id}' must be in the '{expectedStatus}' status before dispatch.");
        }
    }

    private static void ValidateAssetStoreAssignment(
        Asset asset,
        Store sourceStore,
        Store destinationStore)
    {
        var sourceIsHeadquarters = IsHeadquarters(sourceStore);

        if (sourceIsHeadquarters)
        {
            /*
             * AssignedStoreId represents the store responsible
             * for the asset, not the asset's current physical
             * location.
             *
             * When an asset is dispatched from headquarters,
             * AssignedStoreId must already point to the destination
             * store.
             */
            if (asset.AssignedStoreId != destinationStore.Id)
            {
                throw new PackageStateConflictException($"Asset '{asset.Id}' is not assigned to the package destination store.");
            }

            return;
        }

        /*
         * When an asset is dispatched from a regular store to
         * headquarters, AssignedStoreId must point to the source
         * store.
         */
        if (asset.AssignedStoreId != sourceStore.Id)
        {
            throw new PackageStateConflictException($"Asset '{asset.Id}' is not assigned to the package source store.");
        }
    }

    private static void ValidateRoute(Store sourceStore, Store destinationStore)
    {
        ArgumentNullException.ThrowIfNull(sourceStore);
        ArgumentNullException.ThrowIfNull(destinationStore);

        if (sourceStore.Id == destinationStore.Id)
        {
            throw new PackageStateConflictException("Source and destination stores must be different.");
        }

        var sourceIsHeadquarters = IsHeadquarters(sourceStore);
        var destinationIsHeadquarters = IsHeadquarters(destinationStore);

        if (sourceIsHeadquarters == destinationIsHeadquarters)
        {
            throw new PackageStateConflictException("A package must be transported between a store and headquarters.");
        }
    }

    private static bool IsHeadquarters(Store store)
    {
        ArgumentNullException.ThrowIfNull(store);

        return string.Equals(store.StoreNumber, HeadquartersStoreNumber, StringComparison.Ordinal);
    }

    private static void ValidateComment(string? comment)
    {
        if (comment is not null && string.IsNullOrWhiteSpace(comment))
        {
            throw new ArgumentException("Comment cannot be empty or whitespace.", nameof(comment));
        }

        if (comment?.Trim().Length > CommentMaxLength)
        {
            throw new ArgumentException($"Comment cannot exceed {CommentMaxLength} characters.", nameof(comment));
        }
    }

    private static void ValidatePackageId(Guid packageId)
    {
        if (packageId == Guid.Empty)
        {
            throw new ArgumentException("The package identifier cannot be empty.", nameof(packageId));
        }
    }

    /// <summary>
    /// Ensures that the package contains exactly the assets
    /// protected by the acquired locks.
    /// </summary>
    /// <param name="package">
    /// The package reloaded inside the protected operation.
    /// </param>
    /// <param name="lockedAssetIds">
    /// The identifiers of the assets protected by the executor.
    /// </param>
    /// <exception cref="ArgumentNullException">
    /// Thrown when a required argument is null.
    /// </exception>
    /// <exception cref="PackageStateConflictException">
    /// Thrown when package contents differ from the locked asset set
    /// or contain duplicate asset entries.
    /// </exception>
    private static void ValidateLockedPackageAssets(
        Package package,
        IReadOnlyCollection<Guid> lockedAssetIds)
    {
        ArgumentNullException.ThrowIfNull(package);
        ArgumentNullException.ThrowIfNull(lockedAssetIds);

        var currentAssetIds = package.Items
            .Select(item => item.AssetId)
            .ToArray();

        var lockedAssetSet = lockedAssetIds.ToHashSet();

        if (currentAssetIds.Length != lockedAssetSet.Count ||
            !lockedAssetSet.SetEquals(currentAssetIds))
        {
            throw new PackageStateConflictException(
                "The package contents changed before dispatch acquired " +
                "the required asset locks. Reload the package and try again.");
        }
    }
    /// <summary>
    /// Builds the Hungarian HTML description for dispatching
    /// an asset back to its origin store.
    /// </summary>
    private static string BuildServiceDeskNoteDescription(
        ServiceCase serviceCase,
        ServiceCaseEvent serviceCaseEvent,
        Asset asset,
        Store destinationStore)
    {
        ArgumentNullException.ThrowIfNull(serviceCase);
        ArgumentNullException.ThrowIfNull(serviceCaseEvent);
        ArgumentNullException.ThrowIfNull(asset);
        ArgumentNullException.ThrowIfNull(destinationStore);

        static string Encode(string? value)
        {
            return System.Net.WebUtility.HtmlEncode(string.IsNullOrWhiteSpace(value) ? "Nincs megadva" : value);
        }

        var comment = Encode(serviceCaseEvent.Comment)
            .Replace("\r\n", "\n")
            .Replace("\n", "<br>");

        return $"""
            <div>
                <h3>Visszaküldés az áruházba</h3>

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
                    <strong>Céláruház:</strong>
                    {Encode(destinationStore.StoreNumber)}
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