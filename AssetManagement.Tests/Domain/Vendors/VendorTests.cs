using AssetManagement.Domain.Entities.ValueObjects.Address;
using AssetManagement.Domain.Entities.Vendors.Exceptions;
using AssetManagement.Tests.Builders;

namespace AssetManagement.Tests.Domain.Vendors
{
    [TestClass]
    public sealed class VendorTests
    {
        [TestMethod]
        public void Create_Should_Create_Vendor_With_Valid_Data()
        {
            const string name = "Tech Supplier Kft.";
            const string postalCode = "1117";
            const string city = "Budapest";
            const string street = "Budafoki";
            const string houseNumber = "56";
            const string webpage = "https://example.com";

            var vendor = new VendorBuilder()
                .WithName(name)
                .WithCountryCode(CountryCodes.HUN)
                .WithPostalCode(postalCode)
                .WithCity(city)
                .WithStreet(street)
                .WithPublicSpace(PublicSpaces.Street)
                .WithHouseNumber(houseNumber)
                .WithWebpage(webpage)
                .Build();

            Assert.AreNotEqual(Guid.Empty, vendor.VendorId);
            Assert.AreEqual(name, vendor.Name);
            Assert.AreEqual(webpage, vendor.Webpage);
            Assert.IsNotNull(vendor.Address);
            Assert.AreEqual(CountryCodes.HUN,vendor.Address.CountryCode);
            Assert.AreEqual(postalCode, vendor.Address.PostalCode);
            Assert.AreEqual(city, vendor.Address.City);
            Assert.AreEqual(street, vendor.Address.Street);
            Assert.AreEqual(PublicSpaces.Street, vendor.Address.PublicSpace);
            Assert.AreEqual(houseNumber, vendor.Address.HouseNumber);
            Assert.IsNotNull(vendor.Contacts);
            Assert.AreEqual(0, vendor.Contacts.Count);
        }

        [TestMethod]
        public void Create_Should_Generate_Unique_VendorId()
        {
            var firstVendor = new VendorBuilder().Build();
            var secondVendor = new VendorBuilder().Build();

            Assert.AreNotEqual(firstVendor.VendorId, secondVendor.VendorId);
        }

        [TestMethod]
        public void Create_Should_Trim_Name_Address_And_Webpage()
        {
            var vendor = new VendorBuilder()
                .WithName("  Tech Supplier Kft.  ")
                .WithPostalCode("  1117  ")
                .WithCity("  Budapest  ")
                .WithStreet("  Budafoki  ")
                .WithHouseNumber("  56  ")
                .WithWebpage("  https://example.com  ")
                .Build();

            Assert.AreEqual("Tech Supplier Kft.", vendor.Name);
            Assert.AreEqual("1117", vendor.Address.PostalCode);
            Assert.AreEqual("Budapest", vendor.Address.City);
            Assert.AreEqual("Budafoki", vendor.Address.Street);
            Assert.AreEqual("56", vendor.Address.HouseNumber);
            Assert.AreEqual("https://example.com", vendor.Webpage);
        }

        [TestMethod]
        [DataRow(null)]
        [DataRow("")]
        [DataRow(" ")]
        [DataRow("\t")]
        public void Create_Should_Normalize_Missing_Webpage_To_Null(string? webpage)
        {
            var vendor = new VendorBuilder()
                .WithWebpage(webpage)
                .Build();

            Assert.IsNull(vendor.Webpage);
        }

        [TestMethod]
        public void Create_Should_Throw_When_Name_Is_Null()
        {
            var builder = new VendorBuilder()
                .WithName(null);

            var exception = Assert.ThrowsExactly<ArgumentNullException>(() => builder.Build());

            Assert.AreEqual("name", exception.ParamName);
        }

        [TestMethod]
        [DataRow("")]
        [DataRow(" ")]
        [DataRow("\t")]
        [DataRow("\r\n")]
        public void Create_Should_Throw_When_Name_Is_Whitespace(string invalidName)
        {
            var builder = new VendorBuilder()
                .WithName(invalidName);

            var exception = Assert.ThrowsExactly<ArgumentException>(() => builder.Build());

            Assert.AreEqual("name", exception.ParamName);
        }

        [TestMethod]
        [DataRow("example.com")]
        [DataRow("ftp://example.com")]
        [DataRow("not a URL")]
        public void Create_Should_Throw_When_Webpage_Is_Invalid(string invalidWebpage)
        {
            var builder = new VendorBuilder()
                .WithWebpage(invalidWebpage);

            var exception = Assert.ThrowsExactly<ArgumentException>(() => builder.Build());

            Assert.AreEqual("webpage", exception.ParamName);
        }

        [TestMethod]
        public void Create_Should_Throw_When_Webpage_Exceeds_Maximum_Length()
        {
            var webpage = "https://example.com/" + new string('a', 2048);

            var builder = new VendorBuilder()
                .WithWebpage(webpage);

            var exception = Assert.ThrowsExactly<ArgumentException>(() => builder.Build());

            Assert.AreEqual("webpage", exception.ParamName);
        }

        [TestMethod]
        public void UpdateDetails_Should_Update_Vendor_Details()
        {
            var vendor = new VendorBuilder().Build();
            var originalVendorId = vendor.VendorId;

            vendor.UpdateDetails(
                name: "Updated Supplier Kft.",
                countryCode: CountryCodes.HUN,
                postalCode: "7621",
                city: "Pécs",
                street: "Király",
                publicSpace: PublicSpaces.Street,
                houseNumber: "10",
                webpage: "https://updated.example.com");

            Assert.AreEqual(originalVendorId, vendor.VendorId);
            Assert.AreEqual("Updated Supplier Kft.", vendor.Name);
            Assert.AreEqual(CountryCodes.HUN, vendor.Address.CountryCode);
            Assert.AreEqual("7621", vendor.Address.PostalCode);
            Assert.AreEqual("Pécs", vendor.Address.City);
            Assert.AreEqual("Király", vendor.Address.Street);
            Assert.AreEqual(PublicSpaces.Street, vendor.Address.PublicSpace);
            Assert.AreEqual("10", vendor.Address.HouseNumber);
            Assert.AreEqual("https://updated.example.com", vendor.Webpage);
        }

        [TestMethod]
        public void UpdateDetails_Should_Normalize_Webpage()
        {
            var vendor = new VendorBuilder().Build();

            vendor.UpdateDetails(
                name: "Updated Supplier",
                countryCode: CountryCodes.HUN,
                postalCode: "7621",
                city: "Pécs",
                street: "Király",
                publicSpace: PublicSpaces.Street,
                houseNumber: "10",
                webpage: "   ");

            Assert.IsNull(vendor.Webpage);
        }

        [TestMethod]
        public void UpdateDetails_Should_Not_Change_Vendor_When_Address_Is_Invalid()
        {
            var vendor = new VendorBuilder().Build();

            var originalName = vendor.Name;
            var originalAddress = vendor.Address;
            var originalWebpage = vendor.Webpage;

            Assert.ThrowsExactly<ArgumentNullException>(
                () => vendor.UpdateDetails(
                    name: "Updated Supplier",
                    countryCode: CountryCodes.HUN,
                    postalCode: null!,
                    city: "Pécs",
                    street: "Király",
                    publicSpace: PublicSpaces.Street,
                    houseNumber: "10",
                    webpage: "https://updated.example.com"));

            Assert.AreEqual(originalName, vendor.Name);
            Assert.AreSame(originalAddress, vendor.Address);
            Assert.AreEqual(originalWebpage, vendor.Webpage);
        }

        [TestMethod]
        public void AttachContact_Should_Attach_Standalone_Contact()
        {
            var vendor = new VendorBuilder().Build();
            var contact = new ContactBuilder().Build();

            vendor.AttachContact(contact);

            Assert.AreEqual(vendor.VendorId, contact.VendorId);
            Assert.IsTrue(contact.IsAttachedToVendor);
            Assert.AreSame(contact, vendor.Contacts.Single());
        }

        [TestMethod]
        public void AttachContact_Should_Throw_When_Contact_Is_Null()
        {
            var vendor = new VendorBuilder().Build();

            var exception = Assert.ThrowsExactly<ArgumentNullException>(() => vendor.AttachContact(null!));

            Assert.AreEqual("contact", exception.ParamName);
        }

        [TestMethod]
        public void AttachContact_Should_Be_Idempotent_For_Same_Vendor()
        {
            var vendor = new VendorBuilder().Build();
            var contact = new ContactBuilder().Build();

            vendor.AttachContact(contact);
            vendor.AttachContact(contact);

            Assert.AreEqual(1, vendor.Contacts.Count);
            Assert.AreEqual(vendor.VendorId, contact.VendorId);
        }

        [TestMethod]
        public void AttachContact_Should_Throw_When_Contact_Is_Attached_To_Another_Vendor()
        {
            var firstVendor = new VendorBuilder().Build();
            var secondVendor = new VendorBuilder().WithName("Other Supplier Kft.").Build();
            var contact = new ContactBuilder().Build();

            firstVendor.AttachContact(contact);

            Assert.ThrowsExactly<VendorContactConflictException>(() => secondVendor.AttachContact(contact));

            Assert.AreEqual(firstVendor.VendorId, contact.VendorId);
            Assert.AreEqual(0, secondVendor.Contacts.Count);
        }

        [TestMethod]
        public void AttachContact_Should_Throw_When_PhoneNumber_Is_Already_Used_By_Vendor_Contact()
        {
            var vendor = new VendorBuilder().Build();
            var firstContact = new ContactBuilder().WithPhoneNumber("+36 30 123 4567").Build();
            var secondContact = new ContactBuilder().WithFirstName("John").WithPhoneNumber("  +36 30 123 4567  ").Build();

            vendor.AttachContact(firstContact);

            Assert.ThrowsExactly<VendorContactConflictException>(() => vendor.AttachContact(secondContact));

            Assert.IsNull(secondContact.VendorId);
            Assert.AreEqual(1, vendor.Contacts.Count);
        }

        [TestMethod]
        public void AttachContact_Should_Allow_Same_PhoneNumber_At_Different_Vendors()
        {
            var firstVendor = new VendorBuilder().Build();
            var secondVendor = new VendorBuilder().WithName("Other Supplier Kft.").Build();
            var firstContact = new ContactBuilder().Build();
            var secondContact = new ContactBuilder().Build();

            firstVendor.AttachContact(firstContact);
            secondVendor.AttachContact(secondContact);

            Assert.AreEqual(secondVendor.VendorId, secondContact.VendorId);
        }

        [TestMethod]
        public void DetachContact_Should_Detach_Contact_And_Keep_It_Standalone()
        {
            var vendor = new VendorBuilder().Build();
            var contact = new ContactBuilder().Build();

            vendor.AttachContact(contact);
            vendor.DetachContact(contact.ContactId);

            Assert.AreEqual(0, vendor.Contacts.Count);
            Assert.IsNull(contact.VendorId);
            Assert.IsFalse(contact.IsAttachedToVendor);
        }

        [TestMethod]
        public void DetachContact_Should_Allow_Contact_To_Be_Attached_To_Another_Vendor()
        {
            var firstVendor = new VendorBuilder().Build();
            var secondVendor = new VendorBuilder().WithName("Other Supplier Kft.").Build();
            var contact = new ContactBuilder().Build();

            firstVendor.AttachContact(contact);
            firstVendor.DetachContact(contact.ContactId);
            secondVendor.AttachContact(contact);

            Assert.AreEqual(secondVendor.VendorId, contact.VendorId);
        }

        [TestMethod]
        public void DetachContact_Should_Throw_When_ContactId_Is_Empty()
        {
            var vendor = new VendorBuilder().Build();

            var exception = Assert.ThrowsExactly<ArgumentException>(() => vendor.DetachContact(Guid.Empty));

            Assert.AreEqual("contactId", exception.ParamName);
        }

        [TestMethod]
        public void DetachContact_Should_Throw_When_Contact_Is_Not_Attached_To_Vendor()
        {
            var vendor = new VendorBuilder().Build();

            Assert.ThrowsExactly<VendorContactConflictException>(() => vendor.DetachContact(Guid.NewGuid()));
        }

        [TestMethod]
        public void EnsureContactPhoneNumberIsUnique_Should_Throw_When_Other_Contact_Has_PhoneNumber()
        {
            var vendor = new VendorBuilder().Build();
            var firstContact = new ContactBuilder().WithPhoneNumber("+36 30 111 1111").Build();
            var secondContact = new ContactBuilder().WithPhoneNumber("+36 30 222 2222").Build();

            vendor.AttachContact(firstContact);
            vendor.AttachContact(secondContact);

            firstContact.Update("Jane", "Smith", "Account Manager", "jane.smith@example.com", "+36 30 222 2222");

            Assert.ThrowsExactly<VendorContactConflictException>(() => vendor.EnsureContactPhoneNumberIsUnique(firstContact));
        }

        [TestMethod]
        public void EnsureContactPhoneNumberIsUnique_Should_Allow_Contact_To_Keep_Its_PhoneNumber()
        {
            var vendor = new VendorBuilder().Build();
            var contact = new ContactBuilder().Build();

            vendor.AttachContact(contact);

            vendor.EnsureContactPhoneNumberIsUnique(contact);
        }
    }
}
