namespace AssetManagement.Application.Contacts
{
    /// <summary>
    /// Represents a standalone contact person returned by the contact endpoints.
    /// </summary>
    /// <param name="Id">The unique identifier of the contact.</param>
    /// <param name="FirstName">The first name of the contact person.</param>
    /// <param name="LastName">The last name of the contact person.</param>
    /// <param name="JobTitle">The job title of the contact person.</param>
    /// <param name="PhoneNumber">The phone number of the contact person.</param>
    /// <param name="EmailAddress">The email address of the contact person.</param>
    /// <param name="VendorId">The identifier of the vendor the contact is attached to, when attached.</param>
    public sealed record ContactDetailsResponse(
        Guid Id,
        string FirstName,
        string LastName,
        string JobTitle,
        string PhoneNumber,
        string EmailAddress,
        Guid? VendorId);
}
