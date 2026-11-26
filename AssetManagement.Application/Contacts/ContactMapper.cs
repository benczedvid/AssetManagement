using AssetManagement.Domain.Entities.Contacts;

namespace AssetManagement.Application.Contacts
{
    internal static class ContactMapper
    {
        public static ContactDetailsResponse ToDetailsResponse(Contact contact)
        {
            return new ContactDetailsResponse(
                Id: contact.ContactId,
                FirstName: contact.FirstName,
                LastName: contact.LastName,
                JobTitle: contact.JobTitle,
                PhoneNumber: contact.PhoneNumber,
                EmailAddress: contact.EmailAddress,
                VendorId: contact.VendorId);
        }
    }
}
