namespace AssetManagement.Application.Contacts.CreateContact
{
    /// <summary>
    /// Represents a request to create a new standalone contact.
    /// The contact is not attached to any vendor when it is created.
    /// </summary>
    /// <param name="FirstName">The first name of the contact person.</param>
    /// <param name="LastName">The last name of the contact person.</param>
    /// <param name="JobTitle">The job title of the contact person.</param>
    /// <param name="PhoneNumber">The phone number of the contact person.</param>
    /// <param name="EmailAddress">The email address of the contact person.</param>
    public sealed record CreateContactRequest(
        string FirstName,
        string LastName,
        string JobTitle,
        string PhoneNumber,
        string EmailAddress);
}
