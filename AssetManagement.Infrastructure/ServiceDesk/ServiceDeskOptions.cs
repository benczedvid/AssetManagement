namespace AssetManagement.Infrastructure.ServiceDesk;

public sealed class ServiceDeskOptions
{
    public const string SectionName = "ServiceDeskPlus";
    public string BaseUrl { get; init; } = string.Empty;
    public string ApiKey { get; init; } = string.Empty;
}