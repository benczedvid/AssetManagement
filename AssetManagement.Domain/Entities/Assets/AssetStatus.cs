namespace AssetManagement.Domain.Entities.Assets
{
    public enum AssetStatus
    {
        Unknown = 0,
        In_HQ = 1,
        Used_In_HQ = 2,
        In_Store = 3,
        Used_In_Store = 4,
        In_Service = 5,
        Route_To_Store = 6,
        Route_To_HQ = 7,
        Route_To_Service = 8,
        Disposed = 9
    }
}
