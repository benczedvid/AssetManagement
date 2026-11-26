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


namespace AssetManagement.Application.Packages.ReceivePackage;

/// <summary>
/// Handles package receipt requests.
/// </summary>
public sealed class ReceivePackageHandler
{
    private const string HeadquartersStoreNumber = "999";
    private const int CommentMaxLength = 2000;

    private readonly IPackageRepository _packageRepository;
    private readonly IAssetRepository _assetRepository;
    private readonly IStoreRepository _storeRepository;
    private readonly IServiceCaseRepository _serviceCaseRepository;
    private readonly IAssetHistoryRepository _assetHistoryRepository;
    private readonly ICurrentApplicationUserResolver _currentApplicationUserResolver;
    private readonly IAssetOperationExecutor _assetOperationExecutor;
    private readonly IServiceDeskNoteOutboxRepository _noteOutboxRepository;

    /// <summary>
    /// Initializes a new instance of the
    /// <see cref="ReceivePackageHandler"/> class.
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
    /// <param name="serviceCaseRepository">
    /// The service case repository.
    /// </param>
    /// <param name="assetHistoryRepository">
    /// The asset history repository.
    /// </param>
    /// <param name="currentApplicationUserResolver">
    /// Resolves the current application user.
    /// </param>
    /// <param name="assetOperationExecutor">
    /// Executes receipt within a transaction protected
    /// by exclusive locks for all included assets.
    /// </param>
    /// <param name="serviceDeskNoteOutboxRepository"></param>
    /// <exception cref="ArgumentNullException">
    /// Thrown when a required dependency is null.
    /// </exception>
    public ReceivePackageHandler(
        IPackageRepository packageRepository,
        IAssetRepository assetRepository,
        IStoreRepository storeRepository,
        IServiceCaseRepository serviceCaseRepository,
        IAssetHistoryRepository assetHistoryRepository,
        ICurrentApplicationUserResolver currentApplicationUserResolver,
        IAssetOperationExecutor assetOperationExecutor,
        IServiceDeskNoteOutboxRepository serviceDeskNoteOutboxRepository
        )
    {
        ArgumentNullException.ThrowIfNull(packageRepository);
        ArgumentNullException.ThrowIfNull(assetRepository);
        ArgumentNullException.ThrowIfNull(storeRepository);
        ArgumentNullException.ThrowIfNull(serviceCaseRepository);
        ArgumentNullException.ThrowIfNull(assetHistoryRepository);
        ArgumentNullException.ThrowIfNull(currentApplicationUserResolver);
        ArgumentNullException.ThrowIfNull(assetOperationExecutor);
        ArgumentNullException.ThrowIfNull(serviceDeskNoteOutboxRepository);

        _packageRepository = packageRepository;
        _assetRepository = assetRepository;
        _storeRepository = storeRepository;
        _serviceCaseRepository = serviceCaseRepository;
        _assetHistoryRepository = assetHistoryRepository;
        _currentApplicationUserResolver = currentApplicationUserResolver;
        _assetOperationExecutor = assetOperationExecutor;
        _noteOutboxRepository = serviceDeskNoteOutboxRepository;
    }

    /// <summary>
    /// Receives a package within a transaction protected by a package lock
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
    /// The identifier of the package to receive.
    /// </param>
    /// <param name="request">
    /// The package receipt request.
    /// </param>
    /// <param name="cancellationToken">
    /// A token used to cancel lookup, lock acquisition, and execution.
    /// </param>
    /// <returns>
    /// The receipt result after the transaction has been committed.
    /// </returns>
    /// <exception cref="ArgumentNullException">
    /// Thrown when the request is null.
    /// </exception>
    /// <exception cref="ArgumentException">
    /// Thrown when the package identifier, receiving store identifier,
    /// or comment is invalid.
    /// </exception>
    /// <exception cref="PackageNotFoundException">
    /// Thrown when the package cannot be found.
    /// </exception>
    /// <exception cref="PackageStateConflictException">
    /// Thrown when package contents or business state do not allow receipt.
    /// </exception>
    /// <exception cref="PackageAccessDeniedException">
    /// Thrown when the current user cannot receive the package.
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
    public Task<ReceivePackageResponse> HandleAsync(
        Guid packageId,
        ReceivePackageRequest request,
        CancellationToken cancellationToken = default)
    {
        ValidatePackageId(packageId);
        ArgumentNullException.ThrowIfNull(request);
        ValidateRequest(request);

        return _assetOperationExecutor
            .ExecutePackageAsync<ReceivePackageResponse>(
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
    /// Receives a package at its destination store.
    /// </summary>
    /// <param name="packageId">
    /// The unique identifier of the package.
    /// </param>
    /// <param name="request">
    /// The package receipt request.
    /// </param>
    /// <param name="lockedAssetIds">
    /// The unique identifiers of the assets that have been locked for exclusive access during the operation.
    /// </param>
    /// <param name="cancellationToken">
    /// A token used to cancel the asynchronous operation.
    /// </param>
    /// <returns>
    /// The received package and resulting asset statuses.
    /// </returns>
    /// <exception cref="ArgumentNullException">
    /// Thrown when <paramref name="request"/> is null.
    /// </exception>
    /// <exception cref="ArgumentException">
    /// Thrown when the package identifier, receiving store identifier or comment is invalid.
    /// </exception>
    /// <exception cref="PackageNotFoundException">
    /// Thrown when the package cannot be found.
    /// </exception>
    /// <exception cref="PackageStoreNotFoundException">
    /// Thrown when the destination store cannot be found.
    /// </exception>
    /// <exception cref="PackageAssetNotFoundException">
    /// Thrown when an included asset cannot be found.
    /// </exception>
    /// <exception cref="PackageStateConflictException">
    /// Thrown when the package, asset or related service case is not in a valid state for receipt.
    /// </exception>
    /// <exception cref="PackageAccessDeniedException">
    /// Thrown when the current user cannot receive the package.
    /// </exception>
    private async Task<ReceivePackageResponse> HandleCoreAsync(
        Guid packageId,
        ReceivePackageRequest request,
        IReadOnlyCollection<Guid> lockedAssetIds,
        CancellationToken cancellationToken = default)
    {
        ValidatePackageId(packageId);

        ArgumentNullException.ThrowIfNull(request);

        ValidateRequest(request);

        var currentUser = await _currentApplicationUserResolver.ResolveAsync(cancellationToken);
        var package = await _packageRepository.GetForUpdateByIdAsync(packageId, cancellationToken)
            ?? throw new PackageNotFoundException(packageId);

        ValidatePackageForReceipt(package, request.ReceivingStoreId);
        PackageAuthorization.EnsureCanReceive(currentUser, package);
        ValidateLockedPackageAssets(package, lockedAssetIds);

        var destinationStore = await _storeRepository.GetByIdAsync(package.DestinationStoreId, cancellationToken)
            ?? throw new PackageStoreNotFoundException(package.DestinationStoreId);
        var destinationIsHeadquarters = IsHeadquarters(destinationStore);
        var receivedAtUtc = DateTime.UtcNow;
        var assetResults = new List<ReceivePackageAssetResponse>(package.Items.Count);
        var assetHistories = new List<AssetHistory>(package.Items.Count);

        foreach (var packageItem in package.Items)
        {
            var asset = await _assetRepository.GetForUpdateByIdAsync(packageItem.AssetId, cancellationToken)
                ?? throw new PackageAssetNotFoundException(packageItem.AssetId);

            ValidateAssetForReceipt(
                asset,
                package,
                destinationStore,
                destinationIsHeadquarters);

            var previousStatus = asset.AssetStatus;

            ReceiveAsset(asset, destinationIsHeadquarters);

            assetHistories.Add(CreateAssetStatusHistory(
                    asset,
                    previousStatus,
                    destinationIsHeadquarters,
                    currentUser.Id,
                    currentUser.DisplayName,
                    receivedAtUtc));

            await UpdateServiceCaseAsync(
                asset,
                destinationStore,
                destinationIsHeadquarters,
                request.Comment,
                currentUser.Id,
                currentUser.DisplayName,
                receivedAtUtc,
                cancellationToken);

            assetResults.Add(new ReceivePackageAssetResponse(
                    AssetId: asset.Id,
                    AssignedStoreId: asset.AssignedStoreId,
                    PreviousStatus: previousStatus,
                    NewStatus: asset.AssetStatus));
        }

        try
        {
            package.Receive(
                receivingStoreId: request.ReceivingStoreId,
                comment: request.Comment,
                performedByUserId: currentUser.Id,
                performedByUserName: currentUser.DisplayName,
                receivedAtUtc: receivedAtUtc);
        }
        catch (InvalidOperationException exception)
        {
            throw new PackageStateConflictException(exception.Message);
        }

        await _assetHistoryRepository.AddRangeAsync(assetHistories, cancellationToken);
        await _packageRepository.SaveChangesAsync(cancellationToken);

        return new ReceivePackageResponse(
            PackageId: package.Id,
            PackageNumber: package.PackageNumber,
            PackageStatus: package.Status,
            DestinationStoreId: package.DestinationStoreId,
            Assets: assetResults,
            ReceivedAtUtc: receivedAtUtc,
            ReceivedByUserId: currentUser.Id,
            ReceivedByUserName: currentUser.DisplayName);
    }

    private async Task UpdateServiceCaseAsync(
        Asset asset,
        Store destinationStore,
        bool destinationIsHeadquarters,
        string? comment,
        Guid performedByUserId,
        string performedByUserName,
        DateTime occurredAtUtc,
        CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(asset);
        ArgumentNullException.ThrowIfNull(destinationStore);

        var serviceCase = await _serviceCaseRepository.GetActiveForUpdateByAssetIdAsync(asset.Id, cancellationToken);

        if (serviceCase is null)
        {
            return;
        }

        var previousStatus = serviceCase.Status;
        ServiceCaseEventType eventType;

        try
        {
            if (destinationIsHeadquarters)
            {
                ValidateHeadquartersServiceCase(serviceCase, asset);

                serviceCase.ReceiveAtHeadquarters();

                eventType = ServiceCaseEventType.ReceivedAtHeadquarters;
            }
            else
            {
                ValidateStoreServiceCase(serviceCase, asset, destinationStore);

                serviceCase.ReceiveAtOriginStore();

                eventType = ServiceCaseEventType.ReceivedInStore;
            }
        }
        catch (InvalidOperationException exception)
        {
            throw new PackageStateConflictException(exception.Message);
        }

        // Create the local service case event.
        var serviceCaseEvent = ServiceCaseEvent.Create(
            serviceCaseId: serviceCase.Id,
            type: eventType,
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
            destinationIsHeadquarters);

        // Register the ServiceDesk request note.
        // External synchronization is handled by the background processor.
        var noteOutbox = ServiceDeskNoteOutbox.Create(
            serviceCaseId: serviceCase.Id,
            serviceCaseEventId: serviceCaseEvent.Id,
            description: noteDescription,
            createdAtUtc: occurredAtUtc);

        await _noteOutboxRepository.AddAsync(noteOutbox,  cancellationToken);
    }

    private static void ReceiveAsset(Asset asset, bool destinationIsHeadquarters)
    {
        ArgumentNullException.ThrowIfNull(asset);

        try
        {
            if (destinationIsHeadquarters)
            {
                asset.ReceiveAtHeadquartersForServiceInspection();

                return;
            }

            asset.ReceiveAtStoreAfterService();
        }
        catch (InvalidOperationException exception)
        {
            throw new PackageStateConflictException(exception.Message);
        }
    }

    /// <summary>
    /// Validates whether the active service case can be received
    /// at headquarters with the asset.
    /// </summary>
    /// <remarks>
    /// The caller must first validate that the asset's assigned store
    /// matches the package source store.
    /// The service case origin store must match that assignment.
    /// </remarks>
    /// <param name="serviceCase">
    /// The active service case associated with the asset.
    /// </param>
    /// <param name="asset">
    /// The asset being received.
    /// </param>
    /// <exception cref="ArgumentNullException">
    /// Thrown when the service case or asset is null.
    /// </exception>
    /// <exception cref="PackageStateConflictException">
    /// Thrown when the service case belongs to another asset,
    /// is not awaiting central receipt, or its origin store
    /// does not match the asset's assigned store.
    /// </exception>
    private static void ValidateHeadquartersServiceCase(
        ServiceCase serviceCase,
        Asset asset)
    {
        ArgumentNullException.ThrowIfNull(serviceCase);
        ArgumentNullException.ThrowIfNull(asset);

        if (serviceCase.AssetId != asset.Id)
        {
            throw new PackageStateConflictException(
                "The service case and asset relationship is invalid.");
        }

        if (serviceCase.Status != ServiceCaseStatus.AwaitingCentralReceipt)
        {
            throw new PackageStateConflictException(
                "Only a service case awaiting central receipt " +
                "can be received at headquarters.");
        }

        if (!serviceCase.OriginStoreId.HasValue ||
            serviceCase.OriginStoreId.Value == Guid.Empty)
        {
            throw new PackageStateConflictException(
                "The active service case does not have a valid origin store.");
        }

        if (serviceCase.OriginStoreId.Value != asset.AssignedStoreId)
        {
            throw new PackageStateConflictException(
                "The service case origin store does not match " +
                "the asset's assigned store and package source.");
        }
    }

    private static void ValidateStoreServiceCase(ServiceCase serviceCase, Asset asset, Store destinationStore)
    {
        ArgumentNullException.ThrowIfNull(serviceCase);
        ArgumentNullException.ThrowIfNull(asset);
        ArgumentNullException.ThrowIfNull(destinationStore);

        if (serviceCase.AssetId != asset.Id)
        {
            throw new PackageStateConflictException("The service case and asset relationship is invalid.");
        }

        if (serviceCase.Status != ServiceCaseStatus.ReturningToStore)
        {
            throw new PackageStateConflictException("Only a service case returning to its origin store can be received at a store.");
        }

        if (!serviceCase.OriginStoreId.HasValue || serviceCase.OriginStoreId.Value != destinationStore.Id)
        {
            throw new PackageStateConflictException("The package destination does not match the service case origin store.");
        }
    }

    private static AssetHistory CreateAssetStatusHistory(
        Asset asset,
        AssetStatus previousStatus,
        bool destinationIsHeadquarters,
        Guid changedByUserId,
        string changedByUserName,
        DateTime changedAtUtc)
    {
        ArgumentNullException.ThrowIfNull(asset);

        var action = destinationIsHeadquarters
            ? nameof(Asset.ReceiveAtHeadquartersForServiceInspection)
            : nameof(Asset.ReceiveAtStoreAfterService);

        return new AssetHistory(
            assetId: asset.Id,
            action: action,
            propertyName: nameof(Asset.AssetStatus),
            oldValue: previousStatus.ToString(),
            newValue: asset.AssetStatus.ToString(),
            changedByUserId: changedByUserId,
            changedByUserName: changedByUserName,
            changedAtUtc: changedAtUtc);
    }

    private static void ValidatePackageForReceipt(Package package,Guid receivingStoreId)
    {
        ArgumentNullException.ThrowIfNull(package);

        if (package.Status != PackageStatus.InTransit)
        {
            throw new PackageStateConflictException("Only packages in transit can be received.");
        }

        if (package.DestinationStoreId != receivingStoreId)
        {
            throw new PackageStateConflictException("The package can only be received at its destination store.");
        }

        if (package.Items.Count == 0)
        {
            throw new PackageStateConflictException("An empty package cannot be received.");
        }
    }

    private static void ValidateAssetForReceipt(
        Asset asset,
        Package package,
        Store destinationStore,
        bool destinationIsHeadquarters)
    {
        ArgumentNullException.ThrowIfNull(asset);
        ArgumentNullException.ThrowIfNull(package);
        ArgumentNullException.ThrowIfNull(destinationStore);

        var expectedStatus = destinationIsHeadquarters
            ? AssetStatus.Route_To_HQ
            : AssetStatus.Route_To_Store;

        if (asset.AssetStatus != expectedStatus)
        {
            throw new PackageStateConflictException($"Asset '{asset.Id}' must be in the '{expectedStatus}' status before receipt.");
        }

        /*
         * AssignedStoreId represents the store responsible
         * for the asset, not its current physical location.
         *
         * When the destination is headquarters, the assignment
         * must remain the package source store.
         */
        if (destinationIsHeadquarters)
        {
            if (asset.AssignedStoreId != package.SourceStoreId)
            {
                throw new PackageStateConflictException($"Asset '{asset.Id}' is not assigned to the package source store.");
            }

            return;
        }

        /*
         * When the package is received at a regular store,
         * AssignedStoreId must already point to the destination
         * store.
         */
        if (asset.AssignedStoreId != destinationStore.Id)
        {
            throw new PackageStateConflictException($"Asset '{asset.Id}' is not assigned to the package destination store.");
        }
    }

    private static void ValidatePackageAccess(Package package, UserAccessScope accessScope)
    {
        ArgumentNullException.ThrowIfNull(package);
        ArgumentNullException.ThrowIfNull(accessScope);

        if (accessScope.CanAccessAllStores)
        {
            return;
        }

        if (!accessScope.StoreId.HasValue || accessScope.StoreId.Value == Guid.Empty)
        {
            throw new PackageAccessDeniedException("The current user does not have a valid store access scope.");
        }

        if (package.DestinationStoreId != accessScope.StoreId.Value)
        {
            throw new PackageAccessDeniedException("The current user cannot receive a package at another store.");
        }
    }

    private static bool IsHeadquarters(Store store)
    {
        ArgumentNullException.ThrowIfNull(store);

        return string.Equals(store.StoreNumber, HeadquartersStoreNumber, StringComparison.Ordinal);
    }

    private static void ValidateRequest(ReceivePackageRequest request)
    {
        if (request.ReceivingStoreId == Guid.Empty)
        {
            throw new ArgumentException("The receiving store identifier cannot be empty.", nameof(request.ReceivingStoreId));
        }

        if (request.Comment is not null && string.IsNullOrWhiteSpace(request.Comment))
        {
            throw new ArgumentException("Comment cannot be empty or whitespace.", nameof(request.Comment));
        }

        if (request.Comment?.Trim().Length > CommentMaxLength)
        {
            throw new ArgumentException($"Comment cannot exceed {CommentMaxLength} characters.", nameof(request.Comment));
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
                "The package contents changed before receipt acquired " +
                "the required asset locks. Reload the package and try again.");
        }
    }

    /// <summary>
    /// Builds the Hungarian HTML description for
    /// receiving an asset associated with a service case.
    /// </summary>
    private static string BuildServiceDeskNoteDescription(
        ServiceCase serviceCase,
        ServiceCaseEvent serviceCaseEvent,
        Asset asset,
        bool destinationIsHeadquarters)
    {
        ArgumentNullException.ThrowIfNull(serviceCase);
        ArgumentNullException.ThrowIfNull(serviceCaseEvent);
        ArgumentNullException.ThrowIfNull(asset);

        static string Encode(string? value)
        {
            return System.Net.WebUtility.HtmlEncode(string.IsNullOrWhiteSpace(value) ? "Nincs megadva" : value);
        }

        var title = destinationIsHeadquarters ? "Eszköz átvéve a központban" : "Eszköz átvéve az áruházban";

        var comment = Encode(serviceCaseEvent.Comment)
            .Replace("\r\n", "\n")
            .Replace("\n", "<br>");

        return $"""
            <div>
                <h3>{Encode(title)}</h3>

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
                    <strong>Átvétel helye:</strong>
                    {Encode(destinationIsHeadquarters ? "Központ" : "Áruház")}
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