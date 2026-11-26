using AssetManagement.Domain.Entities.AssetMovements;

namespace AssetManagement.Tests.Builders;

public sealed class AssetMovementBuilder
{
    private Guid _assetId = Guid.NewGuid();
    private Guid _storeId = Guid.NewGuid();
    private string? _employeeNumber = "103549";
    private AssetMovementType _type = AssetMovementType.Checkout;

    public AssetMovementBuilder WithAssetId(Guid assetId)
    {
        _assetId = assetId;
        return this;
    }

    public AssetMovementBuilder WithStoreId(Guid storeId)
    {
        _storeId = storeId;
        return this;
    }

    public AssetMovementBuilder WithEmployeeNumber(string? employeeNumber)
    {
        _employeeNumber = employeeNumber;
        return this;
    }

    public AssetMovementBuilder WithType(AssetMovementType type)
    {
        _type = type;
        return this;
    }

    public AssetMovementBuilder AsCheckout()
    {
        _type = AssetMovementType.Checkout;
        return this;
    }

    public AssetMovementBuilder AsReturn()
    {
        _type = AssetMovementType.Return;
        return this;
    }

    public AssetMovement Build()
    {
        return AssetMovement.Create(
            assetId: _assetId,
            storeId: _storeId,
            employeeNumber: _employeeNumber!,
            type: _type);
    }
}