namespace AssetManagement.Application.Dashboard.GetStoreDashboard
{
    /// <summary>
    /// Represents PDT asset statistics for a store.
    /// </summary>
    /// <param name="StoreId">The unique identifier of the store.</param>
    /// <param name="StoreNumber">The store number.</param>
    /// <param name="StoreName">The name of the store.</param>
    /// <param name="TotalPDTs">The total number of active PDT assets assigned to the store.</param>
    /// <param name="UsedPDTs">The number of PDT assets currently in use in the store.</param>
    /// <param name="ServicePDTs">The number of PDT assets currently in service.</param>
    /// <param name="AvailablePDTs">The number of PDT assets currently available in the store.</param>
    public sealed record StoreDashboardResponse
    (
        Guid StoreId,
        string StoreNumber,
        string StoreName,
        int TotalPDTs,
        int UsedPDTs,
        int ServicePDTs,
        int AvailablePDTs
    );
}
