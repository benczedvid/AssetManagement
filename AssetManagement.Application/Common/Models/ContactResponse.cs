namespace AssetManagement.Application.Common.Models
{
    /// <summary>
    /// Represents the contact information of a vendor contact person.
    /// </summary>
    /// <param name="Id">The unique identifier of the contact.</param>
    /// <param name="FirstName">The first name of the contact person.</param>
    /// <param name="LastName">The last name of the contact person.</param>
    /// <param name="JobTitle">The job title of the contact person.</param>
    /// <param name="PhoneNumber">The phone number of the contact person.</param>
    /// <param name="EmailAddress">The email address of the contact person.</param>
    public sealed record ContactResponse(
        Guid Id,
        string FirstName,
        string LastName,
        string JobTitle,
        string PhoneNumber,
        string EmailAddress
        );
}
