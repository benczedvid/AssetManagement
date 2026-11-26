using AssetManagement.Domain.Entities.Contacts;

namespace AssetManagement.Tests.Builders
{
    public sealed class ContactBuilder
    {
        private string? _firstName = "Jane";
        private string? _lastName = "Smith";
        private string? _jobTitle = "Account Manager";
        private string? _emailAddress = "jane.smith@example.com";
        private string? _phoneNumber = "+36 30 765 4321";

        public ContactBuilder WithFirstName(string? firstName) { _firstName = firstName; return this; }
        public ContactBuilder WithLastName(string? lastName) { _lastName = lastName; return this; }
        public ContactBuilder WithJobTitle(string? jobTitle) { _jobTitle = jobTitle; return this; }
        public ContactBuilder WithEmailAddress(string? emailAddress) { _emailAddress = emailAddress; return this; }
        public ContactBuilder WithPhoneNumber(string? phoneNumber) { _phoneNumber = phoneNumber; return this; }

        public Contact Build()
        {
            return Contact.Create(
                firstName: _firstName!,
                lastName: _lastName!,
                jobTitle: _jobTitle!,
                emailAddress: _emailAddress!,
                phoneNumber: _phoneNumber!);
        }
    }
}
