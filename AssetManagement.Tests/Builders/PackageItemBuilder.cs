using AssetManagement.Domain.Entities.Packages;

namespace AssetManagement.Tests.Builders;

public sealed class PackageItemBuilder
{
    private Guid _packageId = Guid.NewGuid();
    private Guid _assetId = Guid.NewGuid();
    private DateTime _addedAtUtc = new(2026, 11, 17, 8, 0, 0, DateTimeKind.Utc);
    private Guid _addedByUserId = Guid.NewGuid();

    public PackageItemBuilder WithPackageId(Guid packageId)
    {
        _packageId = packageId;
        return this;
    }

    public PackageItemBuilder WithAssetId(Guid assetId)
    {
        _assetId = assetId;
        return this;
    }

    public PackageItemBuilder WithAddedAtUtc(DateTime addedAtUtc)
    {
        _addedAtUtc = addedAtUtc;
        return this;
    }

    public PackageItemBuilder WithAddedByUserId(Guid addedByUserId)
    {
        _addedByUserId = addedByUserId;
        return this;
    }

    public PackageItem Build()
    {
        return PackageItem.Create(
            packageId: _packageId,
            assetId: _assetId,
            addedAtUtc: _addedAtUtc,
            addedByUserId: _addedByUserId);
    }
}