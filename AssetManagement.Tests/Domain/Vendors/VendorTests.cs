using AssetManagement.Domain.Entities.ValueObjects.Address;
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
        public void AddContact_Should_Add_Contact_And_Return_Id()
        {
            var vendor = new VendorBuilder().Build();

            var contactId = vendor.AddContact(
                firstName: "John",
                lastName: "Doe",
                jobTitle: "Sales Manager",
                emailAddress: "john.doe@example.com",
                phoneNumber: "+36 30 123 4567");

            Assert.AreNotEqual(Guid.Empty, contactId);
            Assert.AreEqual(1, vendor.Contacts.Count);

            var contact = vendor.Contacts.Single();

            Assert.AreEqual(contactId, contact.ContactId);
            Assert.AreEqual(vendor.VendorId, contact.VendorId);
            Assert.AreEqual("John", contact.FirstName);
            Assert.AreEqual("Doe", contact.LastName);
            Assert.AreEqual("Sales Manager", contact.JobTitle);
            Assert.AreEqual("john.doe@example.com", contact.EmailAddress);
            Assert.AreEqual("+36 30 123 4567", contact.PhoneNumber);
        }

        [TestMethod]
        public void AddContact_Should_Trim_Contact_Values()
        {
            var vendor = new VendorBuilder().Build();

            vendor.AddContact(
                firstName: "  John  ",
                lastName: "  Doe  ",
                jobTitle: "  Sales Manager  ",
                emailAddress: "  john.doe@example.com  ",
                phoneNumber: "  +36 30 123 4567  ");

            var contact = vendor.Contacts.Single();

            Assert.AreEqual("John", contact.FirstName);
            Assert.AreEqual("Doe", contact.LastName);
            Assert.AreEqual("Sales Manager", contact.JobTitle);
            Assert.AreEqual("john.doe@example.com", contact.EmailAddress);
            Assert.AreEqual("+36 30 123 4567", contact.PhoneNumber);
        }

        [TestMethod]
        [DataRow("")]
        [DataRow(" ")]
        [DataRow("\t")]
        public void AddContact_Should_Throw_When_FirstName_Is_Whitespace(
            string invalidFirstName)
        {
            var vendor = new VendorBuilder().Build();

            var exception =
                Assert.ThrowsExactly<ArgumentException>(
                    () => vendor.AddContact(
                        firstName: invalidFirstName,
                        lastName: "Doe",
                        jobTitle: "Sales Manager",
                        emailAddress:
                            "john.doe@example.com",
                        phoneNumber:
                            "+36 30 123 4567"));

            Assert.AreEqual("firstName", exception.ParamName);
            Assert.AreEqual(0, vendor.Contacts.Count);
        }

        [TestMethod]
        public void AddContact_Should_Throw_When_PhoneNumber_Is_Null()
        {
            var vendor = new VendorBuilder().Build();

            var exception =
                Assert.ThrowsExactly<ArgumentNullException>(
                    () => vendor.AddContact(
                        firstName: "John",
                        lastName: "Doe",
                        jobTitle: "Sales Manager",
                        emailAddress:
                            "john.doe@example.com",
                        phoneNumber: null!));

            Assert.AreEqual("phoneNumber", exception.ParamName);
            Assert.AreEqual(0, vendor.Contacts.Count);
        }

        [TestMethod]
        public void AddContact_Should_Throw_When_PhoneNumber_Already_Exists()
        {
            var vendor = new VendorBuilder().Build();

            vendor.AddContact(
                firstName: "John",
                lastName: "Doe",
                jobTitle: "Sales Manager",
                emailAddress: "john.doe@example.com",
                phoneNumber: "+36 30 123 4567");

            Assert.ThrowsExactly<InvalidOperationException>(
                () => vendor.AddContact(
                    firstName: "Jane",
                    lastName: "Smith",
                    jobTitle: "Account Manager",
                    emailAddress: "jane.smith@example.com",
                    phoneNumber: "  +36 30 123 4567  "));

            Assert.AreEqual(1, vendor.Contacts.Count);
        }

        [TestMethod]
        public void UpdateContact_Should_Update_All_Contact_Properties()
        {
            var vendor = new VendorBuilder().Build();

            var contactId = vendor.AddContact(
                firstName: "John",
                lastName: "Doe",
                jobTitle: "Sales Manager",
                emailAddress: "john.doe@example.com",
                phoneNumber: "+36 30 123 4567");

            vendor.UpdateContact(
                contactId: contactId,
                firstName: "Jane",
                lastName: "Smith",
                jobTitle: "Account Manager",
                emailAddress: "jane.smith@example.com",
                phoneNumber: "+36 30 765 4321");

            var contact = vendor.Contacts.Single();

            Assert.AreEqual(contactId, contact.ContactId);
            Assert.AreEqual(vendor.VendorId, contact.VendorId);
            Assert.AreEqual("Jane", contact.FirstName);
            Assert.AreEqual("Smith", contact.LastName);
            Assert.AreEqual("Account Manager", contact.JobTitle);
            Assert.AreEqual("jane.smith@example.com", contact.EmailAddress);
            Assert.AreEqual("+36 30 765 4321", contact.PhoneNumber);
        }

        [TestMethod]
        public void UpdateContact_Should_Allow_Contact_To_Keep_Its_PhoneNumber()
        {
            var vendor = new VendorBuilder().Build();

            var contactId = vendor.AddContact(
                firstName: "John",
                lastName: "Doe",
                jobTitle: "Sales Manager",
                emailAddress: "john.doe@example.com",
                phoneNumber: "+36 30 123 4567");

            vendor.UpdateContact(
                contactId: contactId,
                firstName: "John",
                lastName: "Doe",
                jobTitle: "Senior Sales Manager",
                emailAddress: "john.doe@example.com",
                phoneNumber: "+36 30 123 4567");

            Assert.AreEqual("Senior Sales Manager", vendor.Contacts.Single().JobTitle);
        }

        [TestMethod]
        public void UpdateContact_Should_Throw_When_Other_Contact_Has_PhoneNumber()
        {
            var vendor = new VendorBuilder().Build();

            var firstContactId = vendor.AddContact(
                firstName: "John",
                lastName: "Doe",
                jobTitle: "Sales Manager",
                emailAddress: "john.doe@example.com",
                phoneNumber: "+36 30 111 1111");

            vendor.AddContact(
                firstName: "Jane",
                lastName: "Smith",
                jobTitle: "Account Manager",
                emailAddress: "jane.smith@example.com",
                phoneNumber: "+36 30 222 2222");

            Assert.ThrowsExactly<InvalidOperationException>(
                () => vendor.UpdateContact(
                    contactId: firstContactId,
                    firstName: "John",
                    lastName: "Doe",
                    jobTitle: "Sales Manager",
                    emailAddress: "john.doe@example.com",
                    phoneNumber: "+36 30 222 2222"));
        }

        [TestMethod]
        public void UpdateContact_Should_Throw_When_ContactId_Is_Empty()
        {
            var vendor = new VendorBuilder().Build();

            var exception =
                Assert.ThrowsExactly<ArgumentException>(
                    () => vendor.UpdateContact(
                        contactId: Guid.Empty,
                        firstName: "John",
                        lastName: "Doe",
                        jobTitle: "Sales Manager",
                        emailAddress: "john.doe@example.com",
                        phoneNumber: "+36 30 123 4567"));

            Assert.AreEqual("contactId", exception.ParamName);
        }

        [TestMethod]
        public void UpdateContact_Should_Throw_When_Contact_Is_Not_Found()
        {
            var vendor = new VendorBuilder().Build();

            Assert.ThrowsExactly<InvalidOperationException>(
                () => vendor.UpdateContact(
                    contactId: Guid.NewGuid(),
                    firstName: "John",
                    lastName: "Doe",
                    jobTitle: "Sales Manager",
                    emailAddress: "john.doe@example.com",
                    phoneNumber: "+36 30 123 4567"));
        }

        [TestMethod]
        public void RemoveContact_Should_Remove_Contact()
        {
            var vendor = new VendorBuilder().Build();

            var contactId = vendor.AddContact(
                firstName: "John",
                lastName: "Doe",
                jobTitle: "Sales Manager",
                emailAddress: "john.doe@example.com",
                phoneNumber: "+36 30 123 4567");

            vendor.RemoveContact(contactId);

            Assert.AreEqual(0, vendor.Contacts.Count);
        }

        [TestMethod]
        public void RemoveContact_Should_Throw_When_ContactId_Is_Empty()
        {
            var vendor = new VendorBuilder().Build();

            var exception = Assert.ThrowsExactly<ArgumentException>(() => vendor.RemoveContact(Guid.Empty));

            Assert.AreEqual("contactId", exception.ParamName);
        }

        [TestMethod]
        public void RemoveContact_Should_Throw_When_Contact_Is_Not_Found()
        {
            var vendor = new VendorBuilder().Build();

            Assert.ThrowsExactly<InvalidOperationException>(() => vendor.RemoveContact(Guid.NewGuid()));
        }
    }
}