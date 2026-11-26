using AssetManagement.Application.Contacts;
using AssetManagement.Application.Contacts.CreateContact;
using AssetManagement.Application.Contacts.UpdateContact;
using AssetManagement.Domain.Entities.Contacts;
using AssetManagement.Domain.Entities.ValueObjects.Address;
using AssetManagement.Domain.Entities.Vendors;
using AssetManagement.Infrastructure.Persistence;
using AssetManagement.WebApi.IntegrationTests.Authentication;
using AssetManagement.WebApi.IntegrationTests.Infrastructure;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using System.Net;
using System.Net.Http.Json;
using System.Text.Json;
using System.Text.Json.Serialization;

namespace AssetManagement.WebApi.IntegrationTests.Controllers.ContactControllers
{
    [TestClass]
    public sealed class ContactEndpointTests
    {
        private const string Endpoint = "/api/contacts";

        private AssetManagementWebApplicationFactory? _applicationFactory;
        private HttpClient? _httpClient;
        private JsonSerializerOptions _jsonOptions = null!;

        [TestInitialize]
        public void Initialize()
        {
            _applicationFactory = new AssetManagementWebApplicationFactory();
            _httpClient = _applicationFactory.CreateClient();

            _jsonOptions = new JsonSerializerOptions(JsonSerializerDefaults.Web);
            _jsonOptions.Converters.Add(new JsonStringEnumConverter());
        }

        [TestCleanup]
        public void Cleanup()
        {
            _httpClient?.Dispose();
            _applicationFactory?.Dispose();
        }

        [TestMethod]
        public async Task CreateContact_Should_Return_201_And_Persist_Standalone_Contact()
        {
            AddAuthenticatedHeaders();

            var request = new CreateContactRequest("John", "Doe", "Sales Manager", "+36 30 123 4567", "john.doe@example.com");

            using var response = await _httpClient!.PostAsJsonAsync(Endpoint, request, _jsonOptions, CancellationToken.None);

            Assert.AreEqual(HttpStatusCode.Created, response.StatusCode);

            var responseBody = await response.Content.ReadFromJsonAsync<ContactDetailsResponse>(_jsonOptions, CancellationToken.None);

            Assert.IsNotNull(responseBody);
            Assert.IsNull(responseBody.VendorId);
            Assert.IsNotNull(response.Headers.Location);
            StringAssert.EndsWith(response.Headers.Location.ToString(), $"{Endpoint}/{responseBody.Id}", StringComparison.OrdinalIgnoreCase);

            var persistedContact = await FindContactAsync(responseBody.Id);

            Assert.IsNotNull(persistedContact);
            Assert.IsNull(persistedContact.VendorId);
            Assert.AreEqual("john.doe@example.com", persistedContact.EmailAddress);
        }

        [TestMethod]
        public async Task CreateContact_Should_Return_400_When_Data_Is_Invalid()
        {
            AddAuthenticatedHeaders();

            var request = new CreateContactRequest(" ", "Doe", "Sales Manager", "+36 30 123 4567", "john.doe@example.com");

            using var response = await _httpClient!.PostAsJsonAsync(Endpoint, request, _jsonOptions, CancellationToken.None);

            Assert.AreEqual(HttpStatusCode.BadRequest, response.StatusCode);
        }

        [TestMethod]
        public async Task CreateContact_Should_Return_400_When_PhoneNumber_Format_Is_Invalid()
        {
            AddAuthenticatedHeaders();

            var request = new CreateContactRequest("John", "Doe", "Sales Manager", "06/30-123", "john.doe@example.com");

            using var response = await _httpClient!.PostAsJsonAsync(Endpoint, request, _jsonOptions, CancellationToken.None);

            Assert.AreEqual(HttpStatusCode.BadRequest, response.StatusCode);
        }

        [TestMethod]
        public async Task CreateContact_Should_Return_401_When_Request_Is_Not_Authenticated()
        {
            var request = new CreateContactRequest("John", "Doe", "Sales Manager", "+36 30 123 4567", "john.doe@example.com");

            using var response = await _httpClient!.PostAsJsonAsync(Endpoint, request, _jsonOptions, CancellationToken.None);

            Assert.AreEqual(HttpStatusCode.Unauthorized, response.StatusCode);
        }

        [TestMethod]
        [DataRow("CentralUser")]
        [DataRow("StoreManagement")]
        public async Task GetContacts_Should_Return_403_When_User_Cannot_Manage_Vendors(string role)
        {
            AddAuthenticatedHeaders(role);

            using var response = await _httpClient!.GetAsync(Endpoint, CancellationToken.None);

            Assert.AreEqual(HttpStatusCode.Forbidden, response.StatusCode);
        }

        [TestMethod]
        public async Task GetContacts_Should_Return_Attached_And_Standalone_Contacts()
        {
            AddAuthenticatedHeaders();

            var vendor = CreateVendor();
            var attachedContact = CreateContact("+36 30 111 1111");
            var standaloneContact = CreateContact("+36 30 222 2222");
            vendor.AttachContact(attachedContact);

            await SeedAsync(vendor, standaloneContact);

            var contacts = await _httpClient!.GetFromJsonAsync<IReadOnlyList<ContactDetailsResponse>>(Endpoint, _jsonOptions, CancellationToken.None);

            Assert.IsNotNull(contacts);
            Assert.HasCount(2, contacts);
            Assert.AreEqual(vendor.VendorId, contacts.Single(contact => contact.Id == attachedContact.ContactId).VendorId);
            Assert.IsNull(contacts.Single(contact => contact.Id == standaloneContact.ContactId).VendorId);
        }

        [TestMethod]
        public async Task GetContactById_Should_Return_404_When_Contact_Does_Not_Exist()
        {
            AddAuthenticatedHeaders();

            using var response = await _httpClient!.GetAsync($"{Endpoint}/{Guid.NewGuid()}", CancellationToken.None);

            Assert.AreEqual(HttpStatusCode.NotFound, response.StatusCode);
        }

        [TestMethod]
        public async Task UpdateContact_Should_Return_200_And_Persist_Changes()
        {
            AddAuthenticatedHeaders();

            var contact = CreateContact("+36 30 111 1111");

            await SeedAsync(null, contact);

            var request = new UpdateContactRequest("John", "Doe", "Senior Sales Manager", "+36 30 999 9999", "john.doe@example.com");

            using var response = await _httpClient!.PutAsJsonAsync($"{Endpoint}/{contact.ContactId}", request, _jsonOptions, CancellationToken.None);

            Assert.AreEqual(HttpStatusCode.OK, response.StatusCode);

            var persistedContact = await FindContactAsync(contact.ContactId);

            Assert.IsNotNull(persistedContact);
            Assert.AreEqual("Senior Sales Manager", persistedContact.JobTitle);
            Assert.AreEqual("+36 30 999 9999", persistedContact.PhoneNumber);
        }

        [TestMethod]
        public async Task UpdateContact_Should_Return_409_When_PhoneNumber_Is_Used_By_Other_Contact_Of_Same_Vendor()
        {
            AddAuthenticatedHeaders();

            var vendor = CreateVendor();
            var firstContact = CreateContact("+36 30 111 1111");
            var secondContact = CreateContact("+36 30 222 2222");
            vendor.AttachContact(firstContact);
            vendor.AttachContact(secondContact);

            await SeedAsync(vendor);

            var request = new UpdateContactRequest("Jane", "Smith", "Account Manager", "+36 30 222 2222", "jane.smith@example.com");

            using var response = await _httpClient!.PutAsJsonAsync($"{Endpoint}/{firstContact.ContactId}", request, _jsonOptions, CancellationToken.None);

            Assert.AreEqual(HttpStatusCode.Conflict, response.StatusCode);
            Assert.AreEqual("+36 30 111 1111", (await FindContactAsync(firstContact.ContactId))!.PhoneNumber);
        }

        [TestMethod]
        public async Task DeleteContact_Should_Return_204_And_Remove_Contact()
        {
            AddAuthenticatedHeaders();

            var vendor = CreateVendor();
            var contact = CreateContact("+36 30 111 1111");
            vendor.AttachContact(contact);

            await SeedAsync(vendor);

            using var response = await _httpClient!.DeleteAsync($"{Endpoint}/{contact.ContactId}", CancellationToken.None);

            Assert.AreEqual(HttpStatusCode.NoContent, response.StatusCode);
            Assert.IsNull(await FindContactAsync(contact.ContactId));
        }

        [TestMethod]
        public async Task DeleteContact_Should_Return_404_When_Contact_Does_Not_Exist()
        {
            AddAuthenticatedHeaders();

            using var response = await _httpClient!.DeleteAsync($"{Endpoint}/{Guid.NewGuid()}", CancellationToken.None);

            Assert.AreEqual(HttpStatusCode.NotFound, response.StatusCode);
        }

        private static Vendor CreateVendor()
        {
            return Vendor.Create(
                name: "Tech Supplier Kft.",
                countryCode: CountryCodes.HUN,
                postalCode: "1117",
                city: "Budapest",
                street: "Budafoki",
                publicSpace: PublicSpaces.Street,
                houseNumber: "56",
                webpage: "https://example.com");
        }

        private static Contact CreateContact(string phoneNumber)
        {
            return Contact.Create(
                firstName: "Jane",
                lastName: "Smith",
                jobTitle: "Account Manager",
                emailAddress: "jane.smith@example.com",
                phoneNumber: phoneNumber);
        }

        private void AddAuthenticatedHeaders(string? role = null)
        {
            _httpClient!.DefaultRequestHeaders.Add(TestAuthenticationHandler.AuthenticationHeader, "true");
            _httpClient.DefaultRequestHeaders.Add(TestAuthenticationHandler.EntraObjectIdHeader, Guid.NewGuid().ToString());
            _httpClient.DefaultRequestHeaders.Add(TestAuthenticationHandler.EntraTenantIdHeader, Guid.NewGuid().ToString());

            if (role is not null)
            {
                _httpClient.DefaultRequestHeaders.Add(TestAuthenticationHandler.ApplicationRoleHeader, role);
            }
        }

        private async Task SeedAsync(Vendor? vendor, params Contact[] contacts)
        {
            using var scope = _applicationFactory!.Services.CreateScope();

            var dbContext = scope.ServiceProvider.GetRequiredService<AppDbContext>();

            if (vendor is not null)
            {
                await dbContext.Vendors.AddAsync(vendor, CancellationToken.None);
            }

            await dbContext.Contacts.AddRangeAsync(contacts, CancellationToken.None);
            await dbContext.SaveChangesAsync(CancellationToken.None);
        }

        private async Task<Contact?> FindContactAsync(Guid contactId)
        {
            using var scope = _applicationFactory!.Services.CreateScope();

            var dbContext = scope.ServiceProvider.GetRequiredService<AppDbContext>();

            return await dbContext.Contacts
                .AsNoTracking()
                .SingleOrDefaultAsync(contact => contact.ContactId == contactId, CancellationToken.None);
        }
    }
}
