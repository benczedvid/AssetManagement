using AssetManagement.Application.Common.Authorization;
using AssetManagement.Application.Common.Interfaces.Assets;
using AssetManagement.Application.Common.Interfaces.Packages;
using AssetManagement.Application.Common.Interfaces.Users;
using AssetManagement.Application.Packages.Exceptions;
using AssetManagement.Domain.Entities.Assets;
using AssetManagement.Domain.Entities.Packages;

namespace AssetManagement.Application.Packages.GetPackageById;

/// <summary>
/// Handles requests for retrieving detailed package information.
/// </summary>
public sealed class GetPackageByIdHandler
{
    private readonly IPackageRepository _packageRepository;
    private readonly IAssetRepository _assetRepository;
    private readonly ICurrentApplicationUserResolver _currentApplicationUserResolver;

    public GetPackageByIdHandler(
        IPackageRepository packageRepository,
        IAssetRepository assetRepository,
        ICurrentApplicationUserResolver currentApplicationUserResolver)
    {
        ArgumentNullException.ThrowIfNull(packageRepository);
        ArgumentNullException.ThrowIfNull(assetRepository);
        ArgumentNullException.ThrowIfNull(currentApplicationUserResolver);

        _packageRepository = packageRepository;
        _assetRepository = assetRepository;
        _currentApplicationUserResolver = currentApplicationUserResolver;
    }

    /// <summary>
    /// Gets the detailed information of a package.
    /// </summary>
    public async Task<GetPackageByIdResponse> HandleAsync(
        Guid id,
        CancellationToken cancellationToken = default)
    {
        ValidateIdentifier(id);

        var currentUser = await _currentApplicationUserResolver
            .ResolveAsync(cancellationToken);

        var package = await _packageRepository
            .GetByIdAsync(id, cancellationToken)
            ?? throw new PackageNotFoundException(id);

        PackageAuthorization.EnsureCanRead(currentUser, package);

        var assetIds = package.Items
            .Select(item => item.AssetId)
            .Distinct()
            .ToArray();

        IReadOnlyList<Asset> assets = assetIds.Length == 0
            ? []
            : await _assetRepository.GetByIdsAsync(
                assetIds,
                cancellationToken);

        var assetsById = assets.ToDictionary(asset => asset.Id);

        return MapResponse(package, assetsById);
    }

    private static GetPackageByIdResponse MapResponse(
        Package package,
        IReadOnlyDictionary<Guid, Asset> assetsById)
    {
        ArgumentNullException.ThrowIfNull(package);
        ArgumentNullException.ThrowIfNull(assetsById);

        var items = package.Items
            .OrderBy(packageItem => packageItem.AddedAtUtc)
            .ThenBy(packageItem => packageItem.Id)
            .Select(packageItem =>
            {
                if (!assetsById.TryGetValue(
                    packageItem.AssetId,
                    out var asset))
                {
                    throw new InvalidOperationException(
                        $"Asset '{packageItem.AssetId}' referenced by package " +
                        $"'{package.Id}' was not found.");
                }

                return new GetPackageItemResponse(
                    Id: packageItem.Id,
                    AssetId: packageItem.AssetId,
                    AddedAtUtc: packageItem.AddedAtUtc,
                    AddedByUserId: packageItem.AddedByUserId,
                    AssetName: asset.AssetName,
                    SerialNumber: asset.SerialNumber);
            })
            .ToArray();

        var events = package.Events
            .OrderBy(packageEvent => packageEvent.OccurredAtUtc)
            .ThenBy(packageEvent => packageEvent.Id)
            .Select(packageEvent => new GetPackageEventResponse(
                Id: packageEvent.Id,
                Type: packageEvent.Type,
                PreviousStatus: packageEvent.PreviousStatus,
                NewStatus: packageEvent.NewStatus,
                RelatedAssetId: packageEvent.RelatedAssetId,
                RelatedUserId: packageEvent.RelatedUserId,
                Comment: packageEvent.Comment,
                PerformedByUserId: packageEvent.PerformedByUserId,
                PerformedByUserName: packageEvent.PerformedByUserName,
                OccurredAtUtc: packageEvent.OccurredAtUtc))
            .ToArray();

        return new GetPackageByIdResponse(
            Id: package.Id,
            PackageNumber: package.PackageNumber,
            Status: package.Status,
            SourceStoreId: package.SourceStoreId,
            SourceStoreNumber: package.SourceStore.StoreNumber,
            SourceStoreName: package.SourceStore.Name,
            DestinationStoreId: package.DestinationStoreId,
            DestinationStoreNumber: package.DestinationStore.StoreNumber,
            DestinationStoreName: package.DestinationStore.Name,
            CourierUserId: package.CourierUserId,
            CourierUserName: package.CourierUser?.DisplayName,
            Comment: package.Comment,
            CreatedAtUtc: package.CreatedAtUtc,
            CreatedByUserId: package.CreatedByUserId,
            DispatchedAtUtc: package.DispatchedAtUtc,
            DispatchedByUserId: package.DispatchedByUserId,
            ReceivedAtUtc: package.ReceivedAtUtc,
            ReceivedByUserId: package.ReceivedByUserId,
            CancelledAtUtc: package.CancelledAtUtc,
            CancelledByUserId: package.CancelledByUserId,
            Items: items,
            Events: events);
    }

    private static void ValidateIdentifier(Guid id)
    {
        if (id == Guid.Empty)
        {
            throw new ArgumentException(
                "The package identifier cannot be empty.",
                nameof(id));
        }
    }
}