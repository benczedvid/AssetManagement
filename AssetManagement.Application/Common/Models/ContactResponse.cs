namespace AssetManagement.Application.Common.Models
{
    public sealed record ContactResponse(
        Guid Id,
        string FirstName,
        string LastName,
        string JobTitle,
        string PhoneNumber,
        string EmailAddress
        );
}
