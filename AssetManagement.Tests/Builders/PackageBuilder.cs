using AssetManagement.Domain.Entities.Packages;

namespace AssetManagement.Tests.Builders;

public sealed class PackageBuilder
{
    private string? _packageNumber = "PKG-2026-0001";
    private Guid _sourceStoreId = Guid.NewGuid();
    private Guid _destinationStoreId = Guid.NewGuid();
    private Guid? _courierUserId;
    private string? _comment;
    private DateTime _createdAtUtc = new(2026, 11, 17, 8, 0, 0, DateTimeKind.Utc);
    private Guid _createdByUserId = Guid.NewGuid();
    private string? _createdByUserName = "Test User";

    public PackageBuilder WithPackageNumber(string? packageNumber)
    {
        _packageNumber = packageNumber;
        return this;
    }

    public PackageBuilder WithSourceStoreId(Guid sourceStoreId)
    {
        _sourceStoreId = sourceStoreId;
        return this;
    }

    public PackageBuilder WithDestinationStoreId(Guid destinationStoreId)
    {
        _destinationStoreId = destinationStoreId;
        return this;
    }

    public PackageBuilder WithCourierUserId(Guid? courierUserId)
    {
        _courierUserId = courierUserId;
        return this;
    }

    public PackageBuilder WithoutCourier()
    {
        _courierUserId = null;
        return this;
    }

    public PackageBuilder WithComment(string? comment)
    {
        _comment = comment;
        return this;
    }

    public PackageBuilder WithoutComment()
    {
        _comment = null;
        return this;
    }

    public PackageBuilder WithCreatedAtUtc(DateTime createdAtUtc)
    {
        _createdAtUtc = createdAtUtc;
        return this;
    }

    public PackageBuilder WithCreatedByUserId(Guid createdByUserId)
    {
        _createdByUserId = createdByUserId;
        return this;
    }

    public PackageBuilder WithCreatedByUserName(string? createdByUserName)
    {
        _createdByUserName = createdByUserName;
        return this;
    }

    public Package Build()
    {
        return Package.Create(
            packageNumber: _packageNumber!,
            sourceStoreId: _sourceStoreId,
            destinationStoreId: _destinationStoreId,
            courierUserId: _courierUserId,
            comment: _comment,
            createdAtUtc: _createdAtUtc,
            createdByUserId: _createdByUserId,
            createdByUserName: _createdByUserName!);
    }
}