using AssetManagement.Application.Common.Authorization;
using AssetManagement.Application.Common.Interfaces.Assets;
using AssetManagement.Application.Common.Interfaces.CaseNumbers;
using AssetManagement.Application.Common.Interfaces.Packages;
using AssetManagement.Application.Common.Interfaces.ServiceCases;
using AssetManagement.Application.Common.Interfaces.ServiceDesk;
using AssetManagement.Application.Common.Interfaces.Stores;
using AssetManagement.Application.Common.Interfaces.Users;
using AssetManagement.Application.Packages.Exceptions;
using AssetManagement.Domain.Entities.Assets;
using AssetManagement.Domain.Entities.Packages;
using AssetManagement.Domain.Entities.ServiceCases;
using AssetManagement.Domain.Entities.ServiceDesk;
using AssetManagement.Domain.Entities.Stores;

namespace AssetManagement.Application.Packages.CreatePackage;

/// <summary>
/// Handles package creation requests.
/// </summary>
public sealed class CreatePackageHandler
{
    private const string PackageNumberPrefix = "PKG";
    private const string HeadquartersStoreNumber = "999";
    private const int CommentMaxLength = 2000;

    private readonly IPackageRepository _packageRepository;
    private readonly IAssetRepository _assetRepository;
    private readonly IStoreRepository _storeRepository;
    private readonly IApplicationUserRepository _applicationUserRepository;
    private readonly ICurrentApplicationUserResolver _currentApplicationUserResolver;
    private readonly IUserAccessScopeResolver _userAccessScopeResolver;
    private readonly ICaseNumberGenerator _caseNumberGenerator;
    private readonly IServiceCaseRepository _serviceCaseRepository;
    private readonly IAssetOperationExecutor _assetOperationExecutor;
    private readonly IServiceDeskNoteOutboxRepository _noteOutboxRepository;

    /// <summary>
    /// Initializes a new instance of the
    /// <see cref="CreatePackageHandler"/> class.
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
    /// <param name="applicationUserRepository">
    /// The application user repository.
    /// </param>
    /// <param name="currentApplicationUserResolver">
    /// Resolves the current application user.
    /// </param>
    /// <param name="userAccessScopeResolver">
    /// Resolves the current user's store access scope.
    /// </param>
    /// <param name="caseNumberGenerator">
    /// Generates unique business identifiers.
    /// </param>
    /// <param name="serviceCaseRepository">
    /// The service case repository.
    /// </param>
    /// /// <param name="assetOperationExecutor">
    /// Executes package creation within a transaction protected
    /// by exclusive locks for all requested assets.
    /// </param>
    /// <param name="serviceDeskNoteOutboxRepository"></param>
    /// <exception cref="ArgumentNullException">
    /// Thrown when a required dependency is null.
    /// </exception>
    public CreatePackageHandler(
        IPackageRepository packageRepository,
        IAssetRepository assetRepository,
        IStoreRepository storeRepository,
        IApplicationUserRepository applicationUserRepository,
        ICurrentApplicationUserResolver currentApplicationUserResolver,
        IUserAccessScopeResolver userAccessScopeResolver,
        ICaseNumberGenerator caseNumberGenerator,
        IServiceCaseRepository serviceCaseRepository,
        IAssetOperationExecutor assetOperationExecutor,
        IServiceDeskNoteOutboxRepository serviceDeskNoteOutboxRepository)
    {
        ArgumentNullException.ThrowIfNull(packageRepository);
        ArgumentNullException.ThrowIfNull(assetRepository);
        ArgumentNullException.ThrowIfNull(storeRepository);
        ArgumentNullException.ThrowIfNull(applicationUserRepository);
        ArgumentNullException.ThrowIfNull(currentApplicationUserResolver);
        ArgumentNullException.ThrowIfNull(userAccessScopeResolver);
        ArgumentNullException.ThrowIfNull(caseNumberGenerator);
        ArgumentNullException.ThrowIfNull(serviceCaseRepository);
        ArgumentNullException.ThrowIfNull(assetOperationExecutor);
        ArgumentNullException.ThrowIfNull(serviceDeskNoteOutboxRepository);

        _packageRepository = packageRepository;
        _assetRepository = assetRepository;
        _storeRepository = storeRepository;
        _applicationUserRepository = applicationUserRepository;
        _currentApplicationUserResolver = currentApplicationUserResolver;
        _userAccessScopeResolver = userAccessScopeResolver;
        _caseNumberGenerator = caseNumberGenerator;
        _serviceCaseRepository = serviceCaseRepository;
        _assetOperationExecutor = assetOperationExecutor;
        _noteOutboxRepository = serviceDeskNoteOutboxRepository;
    }
    
    /// <summary>
    /// Creates a package within a transaction protected by exclusive
    /// locks for all requested assets.
    /// </summary>
    /// <remarks>
    /// Request validation is performed before acquiring locks.
    /// Business entities are loaded and validated only after all
    /// asset locks have been acquired.
    ///
    /// The core operation saves its changes. The executor commits
    /// the transaction before returning the response.
    ///
    /// The existing active service case validation remains part
    /// of the protected core operation.
    /// </remarks>
    /// <param name="request">
    /// The package creation request.
    /// </param>
    /// <param name="cancellationToken">
    /// A token used to cancel lock acquisition and execution.
    /// </param>
    /// <returns>
    /// The created package information after the transaction
    /// has been committed.
    /// </returns>
    /// <exception cref="ArgumentNullException">
    /// Thrown when the request is null.
    /// </exception>
    /// <exception cref="ArgumentException">
    /// Thrown when the request contains invalid identifiers,
    /// duplicate assets, no assets, or an invalid comment.
    /// </exception>
    /// <exception cref="TimeoutException">
    /// Thrown when an asset lock cannot be acquired within
    /// the configured timeout.
    /// </exception>
    /// <exception cref="OperationCanceledException">
    /// Thrown when the operation is canceled.
    /// </exception>
    /// <exception cref="InvalidOperationException">
    /// Thrown when the execution context does not support
    /// the protected operation.
    /// </exception>
    public Task<CreatePackageResponse> HandleAsync(
        CreatePackageRequest request,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(request);
        ValidateRequest(request);

        return _assetOperationExecutor.ExecuteAsync<CreatePackageResponse>(
            assetIds: request.AssetIds.ToArray(),
            operation: token => HandleCoreAsync(request, token),
            cancellationToken: cancellationToken);
    }

    /// <summary>
    /// Creates a new package containing the requested assets.
    /// </summary>
    /// <param name="request">
    /// The package creation request.
    /// </param>
    /// <param name="cancellationToken">
    /// A token used to cancel the asynchronous operation.
    /// </param>
    /// <returns>
    /// The successfully created package.
    /// </returns>
    /// <exception cref="ArgumentNullException">
    /// Thrown when <paramref name="request"/> is null.
    /// </exception>
    /// <exception cref="ArgumentException">
    /// Thrown when the request contains invalid identifiers, duplicate assets, no assets or an invalid comment.
    /// </exception>
    /// <exception cref="PackageStoreNotFoundException">
    /// Thrown when the source or destination store cannot be found.
    /// </exception>
    /// <exception cref="PackageAssetNotFoundException">
    /// Thrown when one of the requested assets cannot be found.
    /// </exception>
    /// <exception cref="PackageCourierNotFoundException">
    /// Thrown when the selected courier cannot be found.
    /// </exception>
    /// <exception cref="PackageStateConflictException">
    /// Thrown when the route, asset or courier is not valid for package creation.
    /// </exception>
    /// <exception cref="PackageAccessDeniedException">
    /// Thrown when the current user cannot create a package from the selected source store.
    /// </exception>
    public async Task<CreatePackageResponse> HandleCoreAsync(
        CreatePackageRequest request,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(request);

        ValidateRequest(request);

        var currentUser = await _currentApplicationUserResolver.ResolveAsync(cancellationToken);
        var accessScope = await _userAccessScopeResolver.ResolveAsync(cancellationToken);
        var sourceStore = await _storeRepository.GetByIdAsync(request.SourceStoreId, cancellationToken)
            ?? throw new PackageStoreNotFoundException(request.SourceStoreId);
        var destinationStore = await _storeRepository.GetByIdAsync(request.DestinationStoreId, cancellationToken)
            ?? throw new PackageStoreNotFoundException(request.DestinationStoreId);

        ValidateRoute(sourceStore, destinationStore);
        ValidateSourceStoreAccess(sourceStore, accessScope);

        await ValidateCourierAsync(request.CourierUserId, cancellationToken);

        var assets = await LoadAndValidateAssetsAsync(
                request.AssetIds,
                sourceStore,
                destinationStore,
                cancellationToken);

        var nowUtc = DateTime.UtcNow;

        var packageNumber = await _caseNumberGenerator.GenerateAsync(
                prefix: PackageNumberPrefix,
                occurredAtUtc: nowUtc,
                cancellationToken: cancellationToken);

        Package package;

        try
        {
            package = Package.Create(
                packageNumber: packageNumber,
                sourceStoreId: sourceStore.Id,
                destinationStoreId: destinationStore.Id,
                courierUserId: request.CourierUserId,
                comment: request.Comment,
                createdAtUtc: nowUtc,
                createdByUserId: currentUser.Id,
                createdByUserName: currentUser.DisplayName);

            foreach (var asset in assets)
            {
                package.AddAsset(
                    assetId: asset.Id,
                    performedByUserId: currentUser.Id,
                    performedByUserName: currentUser.DisplayName,
                    occurredAtUtc: nowUtc);
            }
        }
        catch (InvalidOperationException exception)
        {
            throw new PackageStateConflictException(exception.Message);
        }

        await _packageRepository.AddAsync(package, cancellationToken);

        // Register ServiceDesk notes for active service cases.
        foreach (var asset in assets)
        {
            var serviceCase = await _serviceCaseRepository.GetActiveForUpdateByAssetIdAsync(asset.Id, cancellationToken);

            if (serviceCase is null)
            {
                continue;
            }

            var serviceCaseEvent = ServiceCaseEvent.Create(
                serviceCaseId: serviceCase.Id,
                type: ServiceCaseEventType.PackageCreated,
                previousStatus: serviceCase.Status,
                newStatus: serviceCase.Status,
                comment: request.Comment,
                performedByUserId: currentUser.Id,
                performedByUserName: currentUser.DisplayName,
                occurredAtUtc: nowUtc);

            await _serviceCaseRepository.AddEventAsync(serviceCaseEvent, cancellationToken);

            var noteDescription = BuildServiceDeskNoteDescription(
                serviceCase,
                serviceCaseEvent,
                asset,
                package,
                sourceStore,
                destinationStore);

            var noteOutbox = ServiceDeskNoteOutbox.Create(
                serviceCaseId: serviceCase.Id,
                serviceCaseEventId: serviceCaseEvent.Id,
                description: noteDescription,
                createdAtUtc: nowUtc);

            await _noteOutboxRepository.AddAsync( noteOutbox, cancellationToken);
        }

        // Persist the package, service case events and note outbox entries.
        await _packageRepository.SaveChangesAsync(cancellationToken);

        return new CreatePackageResponse(
            Id: package.Id,
            PackageNumber: package.PackageNumber,
            Status: package.Status,
            SourceStoreId: package.SourceStoreId,
            DestinationStoreId: package.DestinationStoreId,
            CourierUserId: package.CourierUserId,
            AssetIds: package.Items
                .Select(packageItem => packageItem.AssetId)
                .ToArray(),
            CreatedAtUtc: package.CreatedAtUtc);
    }

    /// <summary>
    /// Loads the requested assets and validates whether they can be
    /// included in the package.
    /// </summary>
    /// <param name="assetIds">
    /// The identifiers of the requested assets.
    /// </param>
    /// <param name="sourceStore">
    /// The package source store.
    /// </param>
    /// <param name="destinationStore">
    /// The package destination store.
    /// </param>
    /// <param name="cancellationToken">
    /// A token used to cancel the asynchronous operation.
    /// </param>
    /// <returns>
    /// The assets that passed all package creation checks.
    /// </returns>
    /// <exception cref="PackageAssetNotFoundException">
    /// Thrown when a requested asset cannot be found.
    /// </exception>
    /// <exception cref="PackageStateConflictException">
    /// Thrown when an asset belongs to an active package or when
    /// its state, assignment, or active service case conflicts
    /// with the package route.
    /// </exception>
    private async Task<IReadOnlyList<Asset>> LoadAndValidateAssetsAsync(
        IReadOnlyCollection<Guid> assetIds,
        Store sourceStore,
        Store destinationStore,
        CancellationToken cancellationToken)
    {
        var assets = new List<Asset>(assetIds.Count);

        foreach (var assetId in assetIds)
        {
            var asset =
                await _assetRepository.GetForUpdateByIdAsync(
                    assetId,
                    cancellationToken)
                ?? throw new PackageAssetNotFoundException(assetId);

            ValidateAssetForPackage(
                asset,
                sourceStore,
                destinationStore);

            var hasActivePackage =
                await _packageRepository.HasActivePackageForAssetAsync(
                    asset.Id,
                    cancellationToken);

            if (hasActivePackage)
            {
                throw new PackageStateConflictException(
                    $"Asset '{asset.Id}' is already assigned " +
                    "to an active package.");
            }

            await ValidateServiceCaseForPackageAsync(
                asset,
                sourceStore,
                destinationStore,
                cancellationToken);

            assets.Add(asset);
        }

        return assets;
    }
    /// <summary>
    /// Validates an optional active service case against the package route.
    /// </summary>
    /// <remarks>
    /// An active service case is not required for package creation.
    /// This method does not modify the service case or the asset.
    /// Route validation must also be repeated at dispatch because
    /// the service case may change after package creation.
    /// </remarks>
    /// <param name="asset">
    /// The asset being added to the package.
    /// </param>
    /// <param name="sourceStore">
    /// The package source store.
    /// </param>
    /// <param name="destinationStore">
    /// The package destination store.
    /// </param>
    /// <param name="cancellationToken">
    /// A token used to cancel the asynchronous operation.
    /// </param>
    /// <returns>
    /// A task representing the asynchronous validation operation.
    /// </returns>
    /// <exception cref="PackageStateConflictException">
    /// Thrown when the active service case status or origin store
    /// conflicts with the package route.
    /// </exception>
    private async Task ValidateServiceCaseForPackageAsync(
        Asset asset,
        Store sourceStore,
        Store destinationStore,
        CancellationToken cancellationToken)
    {
        var serviceCase =
            await _serviceCaseRepository.GetActiveByAssetIdAsync(
                asset.Id,
                cancellationToken);

        if (serviceCase is null)
        {
            return;
        }

        if (IsHeadquarters(destinationStore))
        {
            if (serviceCase.Status != ServiceCaseStatus.AwaitingCentralReceipt)
            {
                throw new PackageStateConflictException(
                    $"The active service case for asset '{asset.Id}' " +
                    "must be awaiting central receipt before the asset " +
                    "can be added to a package destined for headquarters.");
            }

            if (serviceCase.OriginStoreId != sourceStore.Id)
            {
                throw new PackageStateConflictException(
                    $"The package source does not match the active " +
                    $"service case origin store for asset '{asset.Id}'.");
            }

            return;
        }

        if (serviceCase.Status != ServiceCaseStatus.AwaitingReturn)
        {
            throw new PackageStateConflictException(
                $"The active service case for asset '{asset.Id}' " +
                "must be awaiting return before the asset can be " +
                "added to a package destined for a store.");
        }

        if (serviceCase.OriginStoreId != destinationStore.Id)
        {
            throw new PackageStateConflictException(
                $"The package destination does not match the active " +
                $"service case origin store for asset '{asset.Id}'.");
        }
    }

    private async Task ValidateCourierAsync(Guid? courierUserId, CancellationToken cancellationToken)
    {
        if (!courierUserId.HasValue)
        {
            return;
        }

        var courier = await _applicationUserRepository.GetByIdAsync(courierUserId.Value, cancellationToken)
            ?? throw new PackageCourierNotFoundException(courierUserId.Value);

        if (!courier.IsActive)
        {
            throw new PackageStateConflictException("The selected courier is not an active user.");
        }
    }

    private static void ValidateRequest(CreatePackageRequest request)
    {
        ValidateIdentifier(request.SourceStoreId, nameof(request.SourceStoreId), "The source store identifier cannot be empty.");

        ValidateIdentifier(request.DestinationStoreId, nameof(request.DestinationStoreId), "The destination store identifier cannot be empty.");

        if (request.SourceStoreId == request.DestinationStoreId)
        {
            throw new ArgumentException("Source and destination stores must be different.", nameof(request.DestinationStoreId));
        }

        if (request.AssetIds is null || request.AssetIds.Count == 0)
        {
            throw new ArgumentException("At least one asset must be added to the package.", nameof(request.AssetIds));
        }

        if (request.AssetIds.Any(assetId => assetId == Guid.Empty))
        {
            throw new ArgumentException("Asset identifiers cannot be empty.", nameof(request.AssetIds));
        }

        if (request.AssetIds.Distinct().Count() != request.AssetIds.Count)
        {
            throw new ArgumentException("The package contains duplicate assets.", nameof(request.AssetIds));
        }

        if (request.CourierUserId.HasValue && request.CourierUserId.Value == Guid.Empty)
        {
            throw new ArgumentException("The courier user identifier cannot be empty.", nameof(request.CourierUserId));
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

    private static void ValidateSourceStoreAccess(Store sourceStore, UserAccessScope accessScope)
    {
        ArgumentNullException.ThrowIfNull(sourceStore);
        ArgumentNullException.ThrowIfNull(accessScope);

        if (accessScope.CanAccessAllStores)
        {
            return;
        }

        if (!accessScope.StoreId.HasValue || accessScope.StoreId.Value == Guid.Empty)
        {
            throw new PackageAccessDeniedException("The current user does not have a valid store access scope.");
        }

        if (accessScope.StoreId.Value != sourceStore.Id)
        {
            throw new PackageAccessDeniedException("The current user cannot create a package from another store.");
        }
    }

    private static void ValidateAssetForPackage(
        Asset asset,
        Store sourceStore,
        Store destinationStore)
    {
        ArgumentNullException.ThrowIfNull(asset);
        ArgumentNullException.ThrowIfNull(sourceStore);
        ArgumentNullException.ThrowIfNull(destinationStore);

        var sourceIsHeadquarters = IsHeadquarters(sourceStore);

        ValidateAssetStoreAssignment(
            asset,
            sourceStore,
            destinationStore,
            sourceIsHeadquarters);

        if (asset.AssetStatus == AssetStatus.Disposed)
        {
            throw new PackageStateConflictException($"Disposed asset '{asset.Id}' cannot be added to a package.");
        }

        if (!string.IsNullOrWhiteSpace(asset.AssignedEmployeeNumber))
        {
            throw new PackageStateConflictException($"Asset '{asset.Id}' must be returned by the assigned employee before it can be " +
                "added to a package.");
        }

        if (asset.AssignedUserId.HasValue)
        {
            throw new PackageStateConflictException($"Asset '{asset.Id}' must be unassigned from the application user before it can be " +
                "added to a package.");
        }

        if (sourceIsHeadquarters && asset.AssetStatus != AssetStatus.In_HQ)
        {
            throw new PackageStateConflictException($"Asset '{asset.Id}' must be located at headquarters.");
        }

        if (!sourceIsHeadquarters && asset.AssetStatus != AssetStatus.In_Store)
        {
            throw new PackageStateConflictException($"Asset '{asset.Id}' must be located at the source store.");
        }
    }

    private static void ValidateAssetStoreAssignment(
        Asset asset,
        Store sourceStore,
        Store destinationStore,
        bool sourceIsHeadquarters)
    {
        if (sourceIsHeadquarters)
        {
            /*
             * AssignedStoreId represents the store responsible
             * for the asset.
             *
             * When the asset is sent from headquarters, the
             * assigned store must therefore be the destination
             * store, not headquarters.
             */
            if (asset.AssignedStoreId != destinationStore.Id)
            {
                throw new PackageStateConflictException($"Asset '{asset.Id}' is not assigned to the package destination store.");
            }

            return;
        }

        /*
         * When an asset is sent from a regular store to
         * headquarters, its assigned store must be the source
         * store.
         */
        if (asset.AssignedStoreId != sourceStore.Id)
        {
            throw new PackageStateConflictException($"Asset '{asset.Id}' is not assigned to the package source store.");
        }
    }

    private static bool IsHeadquarters(Store store)
    {
        ArgumentNullException.ThrowIfNull(store);

        return string.Equals(store.StoreNumber, HeadquartersStoreNumber, StringComparison.Ordinal);
    }

    private static void ValidateIdentifier(
        Guid identifier,
        string parameterName,
        string errorMessage)
    {
        if (identifier == Guid.Empty)
        {
            throw new ArgumentException(errorMessage, parameterName);
        }
    }

    /// <summary>
    /// Builds the Hungarian HTML description for a
    /// ServiceDesk request note about package creation.
    /// </summary>
    private static string BuildServiceDeskNoteDescription(
        ServiceCase serviceCase,
        ServiceCaseEvent serviceCaseEvent,
        Asset asset,
        Package package,
        Store sourceStore,
        Store destinationStore)
    {
        ArgumentNullException.ThrowIfNull(serviceCase);
        ArgumentNullException.ThrowIfNull(serviceCaseEvent);
        ArgumentNullException.ThrowIfNull(asset);
        ArgumentNullException.ThrowIfNull(package);
        ArgumentNullException.ThrowIfNull(sourceStore);
        ArgumentNullException.ThrowIfNull(destinationStore);

        static string Encode(string? value)
        {
            return System.Net.WebUtility.HtmlEncode(
                string.IsNullOrWhiteSpace(value)
                    ? "Nincs megadva"
                    : value);
        }

        var comment = Encode(serviceCaseEvent.Comment)
            .Replace("\r\n", "\n")
            .Replace("\n", "<br>");

        return $"""
            <div>
                <h3>Csomag összeállítva</h3>

                <p>
                    <strong>Szervizügy:</strong>
                    {Encode(serviceCase.CaseNumber)}
                </p>

                <p>
                    <strong>Csomagszám:</strong>
                    {Encode(package.PackageNumber)}
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
                    <strong>Feladó áruház:</strong>
                    {Encode(sourceStore.StoreNumber)}
                </p>

                <p>
                    <strong>Célállomás:</strong>
                    {Encode(destinationStore.StoreNumber)}
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