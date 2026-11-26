using AssetManagement.Domain.Entities.Contacts;
using AssetManagement.Domain.Entities.ValueObjects.Address;
using AssetManagement.Domain.Entities.Vendors.Exceptions;

namespace AssetManagement.Domain.Entities.Vendors
{
    public sealed class Vendor
    {
        private readonly List<Contact> _contacts = [];
        public Guid VendorId { get; private set; }
        public string Name { get; private set; } = string.Empty;
        public Address Address { get; private set; } = null!;
        public string? Webpage { get; private set; }
        public IReadOnlyCollection<Contact> Contacts =>
            _contacts.AsReadOnly();

        private Vendor() {}

        private Vendor(
            Guid vendorId,
            string name,
            Address address,
            string? webpage)
        {
            VendorId = vendorId;
            Name = name;
            Address = address;
            Webpage = webpage;
        }

        public static Vendor Create(
            string name,
            CountryCodes countryCode,
            string postalCode,
            string city,
            string street,
            PublicSpaces publicSpace,
            string houseNumber,
            string? webpage)
        {
            ArgumentException.ThrowIfNullOrWhiteSpace(name);

            var address = Address.Create(
                countryCode: countryCode,
                postalCode: postalCode,
                city: city,
                street: street,
                publicSpace: publicSpace,
                houseNumber: houseNumber);

            return new Vendor(
                vendorId: Guid.NewGuid(),
                name: name.Trim(),
                address: address,
                webpage: NormalizeWebpage(webpage));
        }

        public void UpdateDetails(
            string name,
            CountryCodes countryCode,
            string postalCode,
            string city,
            string street,
            PublicSpaces publicSpace,
            string houseNumber,
            string? webpage)
        {
            ArgumentException.ThrowIfNullOrWhiteSpace(name);

            var updatedAddress = Address.Create(
                countryCode: countryCode,
                postalCode: postalCode,
                city: city,
                street: street,
                publicSpace: publicSpace,
                houseNumber: houseNumber);

            var normalizedWebpage = NormalizeWebpage(webpage);

            Name = name.Trim();
            Address = updatedAddress;
            Webpage = normalizedWebpage;
        }

        /// <summary>
        /// Attaches a standalone contact to this vendor.
        /// Attaching a contact that is already attached to this vendor has no effect.
        /// </summary>
        public void AttachContact(Contact contact)
        {
            ArgumentNullException.ThrowIfNull(contact);

            if (contact.VendorId == VendorId)
            {
                if (_contacts.All(currentContact => currentContact.ContactId != contact.ContactId))
                {
                    _contacts.Add(contact);
                }

                return;
            }

            if (contact.IsAttachedToVendor)
            {
                throw new VendorContactConflictException("The contact is already attached to another vendor.");
            }

            EnsureContactPhoneNumberIsUnique(contact);

            contact.AssignToVendor(VendorId);
            _contacts.Add(contact);
        }

        /// <summary>
        /// Detaches a contact from this vendor. The contact itself is kept as a standalone contact.
        /// </summary>
        public void DetachContact(Guid contactId)
        {
            if (contactId == Guid.Empty)
            {
                throw new ArgumentException("A valid contact identifier must be specified.", nameof(contactId));
            }

            var contact = _contacts.SingleOrDefault(currentContact => currentContact.ContactId == contactId)
                ?? throw new VendorContactConflictException("The contact is not attached to this vendor.");

            contact.DetachFromVendor();
            _contacts.Remove(contact);
        }

        /// <summary>
        /// Ensures that no other contact attached to this vendor has the same phone number as the given contact.
        /// </summary>
        public void EnsureContactPhoneNumberIsUnique(Contact contact)
        {
            ArgumentNullException.ThrowIfNull(contact);

            var alreadyExists = _contacts.Any(
                currentContact =>
                    currentContact.ContactId != contact.ContactId &&
                    string.Equals(
                        currentContact.PhoneNumber,
                        contact.PhoneNumber,
                        StringComparison.OrdinalIgnoreCase));

            if (alreadyExists)
            {
                throw new VendorContactConflictException("A contact with the same phone number is already attached to this vendor.");
            }
        }

        private static string? NormalizeWebpage(string? webpage)
        {
            if (string.IsNullOrWhiteSpace(webpage))
            {
                return null;
            }

            var normalizedWebpage = webpage.Trim();

            if (normalizedWebpage.Length > 2048)
            {
                throw new ArgumentException("The webpage URL must not exceed 2048 characters.", nameof(webpage));
            }

            if (!Uri.TryCreate(
                    normalizedWebpage,
                    UriKind.Absolute,
                    out var webpageUri) ||
                webpageUri.Scheme != Uri.UriSchemeHttp &&
                webpageUri.Scheme != Uri.UriSchemeHttps)
            {
                throw new ArgumentException("The webpage must be a valid HTTP or HTTPS URL.", nameof(webpage));
            }

            return normalizedWebpage;
        }
    }
}