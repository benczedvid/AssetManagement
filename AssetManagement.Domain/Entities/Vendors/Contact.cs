namespace AssetManagement.Domain.Entities.Vendors
{
    public sealed class Contact
    {
        public Guid ContactId { get; private set; }
        public Guid VendorId { get; private set; }
        public string FirstName { get; private set; } = string.Empty;
        public string LastName { get; private set; } = string.Empty;
        public string JobTitle { get; private set; } = string.Empty;
        public string EmailAddress { get; private set; } = string.Empty;
        public string PhoneNumber { get; private set; } = string.Empty;

        private Contact() {}

        private Contact(
            Guid contactId,
            Guid vendorId,
            string firstName,
            string lastName,
            string jobTitle,
            string emailAddress,
            string phoneNumber)
        {
            ContactId = contactId;
            VendorId = vendorId;
            FirstName = firstName;
            LastName = lastName;
            JobTitle = jobTitle;
            EmailAddress = emailAddress;
            PhoneNumber = phoneNumber;
        }

        internal static Contact Create(
            Guid vendorId,
            string firstName,
            string lastName,
            string jobTitle,
            string emailAddress,
            string phoneNumber)
        {
            if (vendorId == Guid.Empty)
            {
                throw new ArgumentException("A valid vendor identifier must be specified.", nameof(vendorId));
            }

            Validate(
                firstName,
                lastName,
                jobTitle,
                emailAddress,
                phoneNumber);

            return new Contact(
                contactId: Guid.NewGuid(),
                vendorId: vendorId,
                firstName: firstName.Trim(),
                lastName: lastName.Trim(),
                jobTitle: jobTitle.Trim(),
                emailAddress: emailAddress.Trim(),
                phoneNumber: phoneNumber.Trim());
        }

        internal void Update(
            string firstName,
            string lastName,
            string jobTitle,
            string emailAddress,
            string phoneNumber)
        {
            Validate(
                firstName,
                lastName,
                jobTitle,
                emailAddress,
                phoneNumber);

            FirstName = firstName.Trim();
            LastName = lastName.Trim();
            JobTitle = jobTitle.Trim();
            EmailAddress = emailAddress.Trim();
            PhoneNumber = phoneNumber.Trim();
        }

        private static void Validate(
            string firstName,
            string lastName,
            string jobTitle,
            string emailAddress,
            string phoneNumber)
        {
            ArgumentException.ThrowIfNullOrWhiteSpace(firstName);
            ArgumentException.ThrowIfNullOrWhiteSpace(lastName);
            ArgumentException.ThrowIfNullOrWhiteSpace(jobTitle);
            ArgumentException.ThrowIfNullOrWhiteSpace(emailAddress);
            ArgumentException.ThrowIfNullOrWhiteSpace(phoneNumber);
        }
    }
}