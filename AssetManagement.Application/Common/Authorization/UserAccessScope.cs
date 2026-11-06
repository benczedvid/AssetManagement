namespace AssetManagement.Application.Common.Authorization;

public sealed record UserAccessScope(
    bool CanAccessAllStores,
    Guid? StoreId)
{
    public static UserAccessScope Global()
    {
        return new UserAccessScope(CanAccessAllStores: true, StoreId: null);
    }

    public static UserAccessScope ForStore(Guid storeId)
    {
        if (storeId == Guid.Empty)
        {
            throw new ArgumentException("The store identifier cannot be empty.", nameof(storeId));
        }

        return new UserAccessScope(CanAccessAllStores: false, StoreId: storeId);
    }
}