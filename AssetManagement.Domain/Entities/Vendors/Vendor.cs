using AssetManagement.Domain.Entities.ValueObjects.Address;

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

        public Guid AddContact(
            string firstName,
            string lastName,
            string jobTitle,
            string emailAddress,
            string phoneNumber)
        {
            EnsurePhoneNumberIsUnique(phoneNumber: phoneNumber, excludeContactId: null);

            var contact = Contact.Create(
                vendorId: VendorId,
                firstName: firstName,
                lastName: lastName,
                jobTitle: jobTitle,
                emailAddress: emailAddress,
                phoneNumber: phoneNumber);

            _contacts.Add(contact);

            return contact.ContactId;
        }

        public void UpdateContact(
            Guid contactId,
            string firstName,
            string lastName,
            string jobTitle,
            string emailAddress,
            string phoneNumber)
        {
            var contact = GetContact(contactId);
            
            EnsurePhoneNumberIsUnique(phoneNumber: phoneNumber, excludeContactId: contactId);

            contact.Update(
                firstName: firstName,
                lastName: lastName,
                jobTitle: jobTitle,
                emailAddress: emailAddress,
                phoneNumber: phoneNumber);
        }

        public void RemoveContact(Guid contactId)
        {
            var contact = GetContact(contactId);

            _contacts.Remove(contact);
        }

        private Contact GetContact(Guid contactId)
        {
            if (contactId == Guid.Empty)
            {
                throw new ArgumentException("A valid contact identifier must be specified.", nameof(contactId));
            }

            var contact = _contacts.SingleOrDefault(currentContact => currentContact.ContactId == contactId);

            return contact ?? throw new InvalidOperationException("The contact was not found for this vendor.");
        }

        private void EnsurePhoneNumberIsUnique(string phoneNumber, Guid? excludeContactId)
        {
            ArgumentException.ThrowIfNullOrWhiteSpace(phoneNumber);

            var normalizedPhoneNumber = phoneNumber.Trim();

            var alreadyExists = _contacts.Any(
                contact =>
                    contact.ContactId != excludeContactId &&
                    string.Equals(
                        contact.PhoneNumber,
                        normalizedPhoneNumber,
                        StringComparison.OrdinalIgnoreCase));

            if (alreadyExists)
            {
                throw new InvalidOperationException("A contact with the same phone number already exists for this vendor.");
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