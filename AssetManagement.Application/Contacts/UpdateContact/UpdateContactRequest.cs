namespace AssetManagement.Application.Contacts.UpdateContact
{
    /// <summary>
    /// Represents a request to update an existing contact.
    /// </summary>
    /// <param name="FirstName">The first name of the contact person.</param>
    /// <param name="LastName">The last name of the contact person.</param>
    /// <param name="JobTitle">The job title of the contact person.</param>
    /// <param name="PhoneNumber">The phone number of the contact person.</param>
    /// <param name="EmailAddress">The email address of the contact person.</param>
    public sealed record UpdateContactRequest(
        string FirstName,
        string LastName,
        string JobTitle,
        string PhoneNumber,
        string EmailAddress);
}
