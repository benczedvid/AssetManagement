namespace AssetManagement.Application.Common.Email
{
    public sealed record EmailMessage
        (
        IReadOnlyCollection<string> ToRecipients,
        string Subject,
        string TextBody,
        string HtmlBody
        );
}
