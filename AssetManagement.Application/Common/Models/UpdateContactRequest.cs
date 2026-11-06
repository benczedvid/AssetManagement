namespace AssetManagement.Application.Common.Models
{
    /// <summary>
    /// Represents contact information used when updating a vendor.
    /// </summary>
    /// <param name="Id">
    /// The unique identifier of the contact, when updating an existing contact.
    /// </param>
    /// <param name="FirsName">The first name of the contact person.</param>
    /// <param name="LastName">The last name of the contact person.</param>
    /// <param name="JobTitle">The job title of the contact person.</param>
    /// <param name="PhoneNumber">The phone number of the contact person.</param>
    /// <param name="EmailAddress">The email address of the contact person.</param>
    public sealed record UpdateContactRequest(
        Guid? Id,
        string FirsName,
        string LastName,
        string JobTitle,
        string PhoneNumber,
        string EmailAddress);
}
