namespace AssetManagement.Infrastructure.Email
{
    public sealed class MicrosoftGraphEmailOptions
    {
        public const string SectionName = "MicrosoftGraphEMail";

        public string TenantId { get; init; } = string.Empty;
        public string ClientId { get; init; } = string.Empty;
        public string ClientSecret { get; init; } = string.Empty;
        public string SendUserPrincipalName { get; init; } = string.Empty;
        public bool SaveToSentItems { get; init; } = true;
    }
}
