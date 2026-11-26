using System.Text.RegularExpressions;

namespace AssetManagement.Domain.Entities.Contacts
{
    /// <summary>
    /// A standalone contact person. A contact does not belong to any vendor
    /// when it is created, but it can be attached to (and detached from) a vendor.
    /// </summary>
    public sealed partial class Contact
    {
        /// <summary>
        /// International phone number format: a leading '+' and country code, followed by
        /// 6-12 further digits that may be grouped with single spaces or hyphens
        /// (e.g. "+36 30 123 4567", "+36-1-234-5678", "+4915112345678").
        /// </summary>
        public const string PhoneNumberPattern = @"^\+[1-9][0-9]{0,2}(?:[ -]?[0-9]){6,12}$";

        public Guid ContactId { get; private set; }
        public Guid? VendorId { get; private set; }
        public string FirstName { get; private set; } = string.Empty;
        public string LastName { get; private set; } = string.Empty;
        public string JobTitle { get; private set; } = string.Empty;
        public string EmailAddress { get; private set; } = string.Empty;
        public string PhoneNumber { get; private set; } = string.Empty;

        public bool IsAttachedToVendor => VendorId.HasValue;

        private Contact() {}

        private Contact(
            Guid contactId,
            string firstName,
            string lastName,
            string jobTitle,
            string emailAddress,
            string phoneNumber)
        {
            ContactId = contactId;
            FirstName = firstName;
            LastName = lastName;
            JobTitle = jobTitle;
            EmailAddress = emailAddress;
            PhoneNumber = phoneNumber;
        }

        public static Contact Create(
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

            return new Contact(
                contactId: Guid.NewGuid(),
                firstName: firstName.Trim(),
                lastName: lastName.Trim(),
                jobTitle: jobTitle.Trim(),
                emailAddress: emailAddress.Trim(),
                phoneNumber: phoneNumber.Trim());
        }

        public void Update(
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

        internal void AssignToVendor(Guid vendorId)
        {
            if (vendorId == Guid.Empty)
            {
                throw new ArgumentException("A valid vendor identifier must be specified.", nameof(vendorId));
            }

            VendorId = vendorId;
        }

        internal void DetachFromVendor()
        {
            VendorId = null;
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

            if (!PhoneNumberRegex().IsMatch(phoneNumber.Trim()))
            {
                throw new ArgumentException(
                    "The phone number must be in international format, for example +36 30 123 4567.",
                    nameof(phoneNumber));
            }
        }

        [GeneratedRegex(PhoneNumberPattern, RegexOptions.CultureInvariant)]
        private static partial Regex PhoneNumberRegex();
    }
}
