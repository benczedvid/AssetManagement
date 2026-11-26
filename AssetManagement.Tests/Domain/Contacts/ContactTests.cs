using AssetManagement.Domain.Entities.Contacts;
using AssetManagement.Tests.Builders;

namespace AssetManagement.Tests.Domain.Contacts
{
    [TestClass]
    public sealed class ContactTests
    {
        [TestMethod]
        public void Create_Should_Create_Standalone_Contact()
        {
            var contact = Contact.Create(
                firstName: "John",
                lastName: "Doe",
                jobTitle: "Sales Manager",
                emailAddress: "john.doe@example.com",
                phoneNumber: "+36 30 123 4567");

            Assert.AreNotEqual(Guid.Empty, contact.ContactId);
            Assert.IsNull(contact.VendorId);
            Assert.IsFalse(contact.IsAttachedToVendor);
            Assert.AreEqual("John", contact.FirstName);
            Assert.AreEqual("Doe", contact.LastName);
            Assert.AreEqual("Sales Manager", contact.JobTitle);
            Assert.AreEqual("john.doe@example.com", contact.EmailAddress);
            Assert.AreEqual("+36 30 123 4567", contact.PhoneNumber);
        }

        [TestMethod]
        public void Create_Should_Generate_Unique_ContactId()
        {
            var firstContact = new ContactBuilder().Build();
            var secondContact = new ContactBuilder().Build();

            Assert.AreNotEqual(firstContact.ContactId, secondContact.ContactId);
        }

        [TestMethod]
        public void Create_Should_Trim_Values()
        {
            var contact = Contact.Create(
                firstName: "  John  ",
                lastName: "  Doe  ",
                jobTitle: "  Sales Manager  ",
                emailAddress: "  john.doe@example.com  ",
                phoneNumber: "  +36 30 123 4567  ");

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
        public void Create_Should_Throw_When_FirstName_Is_Whitespace(string invalidFirstName)
        {
            var builder = new ContactBuilder().WithFirstName(invalidFirstName);

            var exception = Assert.ThrowsExactly<ArgumentException>(() => builder.Build());

            Assert.AreEqual("firstName", exception.ParamName);
        }

        [TestMethod]
        public void Create_Should_Throw_When_PhoneNumber_Is_Null()
        {
            var builder = new ContactBuilder().WithPhoneNumber(null);

            var exception = Assert.ThrowsExactly<ArgumentNullException>(() => builder.Build());

            Assert.AreEqual("phoneNumber", exception.ParamName);
        }

        [TestMethod]
        [DataRow(null, "Doe", "Sales", "a@b.hu", "+36 30 123 4567", "firstName")]
        [DataRow("John", null, "Sales", "a@b.hu", "+36 30 123 4567", "lastName")]
        [DataRow("John", "Doe", null, "a@b.hu", "+36 30 123 4567", "jobTitle")]
        [DataRow("John", "Doe", "Sales", null, "+36 30 123 4567", "emailAddress")]
        public void Create_Should_Throw_When_Required_Value_Is_Null(
            string? firstName,
            string? lastName,
            string? jobTitle,
            string? emailAddress,
            string? phoneNumber,
            string expectedParamName)
        {
            var exception = Assert.ThrowsExactly<ArgumentNullException>(
                () => Contact.Create(firstName!, lastName!, jobTitle!, emailAddress!, phoneNumber!));

            Assert.AreEqual(expectedParamName, exception.ParamName);
        }

        [TestMethod]
        [DataRow("+36 30 123 4567")]
        [DataRow("+36-1-234-5678")]
        [DataRow("+4915112345678")]
        [DataRow("  +36 20 987 6543  ")]
        public void Create_Should_Accept_International_PhoneNumber(string phoneNumber)
        {
            var contact = new ContactBuilder().WithPhoneNumber(phoneNumber).Build();

            Assert.AreEqual(phoneNumber.Trim(), contact.PhoneNumber);
        }

        [TestMethod]
        [DataRow("06 30 123 4567")]
        [DataRow("+36")]
        [DataRow("+36 30 123 456a")]
        [DataRow("+36  30 123 4567")]
        [DataRow("(06) 30/123-4567")]
        [DataRow("+0 30 123 4567")]
        [DataRow("+36 30 123 4567 8901 2")]
        public void Create_Should_Throw_When_PhoneNumber_Format_Is_Invalid(string phoneNumber)
        {
            var builder = new ContactBuilder().WithPhoneNumber(phoneNumber);

            var exception = Assert.ThrowsExactly<ArgumentException>(() => builder.Build());

            Assert.AreEqual("phoneNumber", exception.ParamName);
        }

        [TestMethod]
        public void Update_Should_Throw_When_PhoneNumber_Format_Is_Invalid()
        {
            var contact = new ContactBuilder().Build();

            Assert.ThrowsExactly<ArgumentException>(
                () => contact.Update("Jane", "Smith", "Sales", "jane@example.com", "123"));

            Assert.AreEqual("+36 30 765 4321", contact.PhoneNumber);
        }

        [TestMethod]
        public void Update_Should_Update_And_Trim_All_Properties()
        {
            var contact = new ContactBuilder().Build();
            var originalId = contact.ContactId;

            contact.Update(
                firstName: " John ",
                lastName: " Doe ",
                jobTitle: " Senior Sales Manager ",
                emailAddress: " john.doe@example.com ",
                phoneNumber: " +36 30 111 2222 ");

            Assert.AreEqual(originalId, contact.ContactId);
            Assert.AreEqual("John", contact.FirstName);
            Assert.AreEqual("Doe", contact.LastName);
            Assert.AreEqual("Senior Sales Manager", contact.JobTitle);
            Assert.AreEqual("john.doe@example.com", contact.EmailAddress);
            Assert.AreEqual("+36 30 111 2222", contact.PhoneNumber);
        }

        [TestMethod]
        public void Update_Should_Not_Change_Contact_When_Data_Is_Invalid()
        {
            var contact = new ContactBuilder().Build();

            Assert.ThrowsExactly<ArgumentException>(
                () => contact.Update("John", " ", "Sales", "john@example.com", "+36 30 111 2222"));

            Assert.AreEqual("Jane", contact.FirstName);
            Assert.AreEqual("Smith", contact.LastName);
            Assert.AreEqual("+36 30 765 4321", contact.PhoneNumber);
        }

        [TestMethod]
        public void Update_Should_Keep_Vendor_Assignment()
        {
            var vendor = new VendorBuilder().Build();
            var contact = new ContactBuilder().Build();

            vendor.AttachContact(contact);

            contact.Update("John", "Doe", "Sales", "john@example.com", "+36 30 111 2222");

            Assert.AreEqual(vendor.VendorId, contact.VendorId);
        }
    }
}
