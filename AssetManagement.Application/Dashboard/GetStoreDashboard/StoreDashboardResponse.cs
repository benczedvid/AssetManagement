namespace AssetManagement.Application.Dashboard.GetStoreDashboard
{
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
