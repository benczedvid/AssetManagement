namespace AssetManagement.Domain.Entities.ServiceCases
{
    public enum ServiceIssueType
    {
        Unknown = 0,
        DoesNotTurnOn = 1,
        PhysicalDamage = 2,
        ChargingIssue = 3,
        DisplayIssue = 4,
        NetworkIssue = 5,
        SoftwareIssue = 6,
        ContinuousRestart = 7,
        FaultyButton = 8,
        Other = 99
    }
}