namespace AssetManagement.Application.Common.Models
{
    public sealed record UpdateContactRequest(
        Guid? Id,
        string FirsName,
        string LastName,
        string JobTitle,
        string PhoneNumber,
        string EmailAddress);
}
