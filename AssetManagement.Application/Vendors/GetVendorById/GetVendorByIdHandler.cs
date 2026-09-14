using AssetManagement.Application.Common.Interfaces.Vendors;
using AssetManagement.Application.Common.Models;
using AssetManagement.Domain.Entities.Vendors;

namespace AssetManagement.Application.Vendors.GetVendorById
{
    public sealed class GetVendorByIdHandler
    {
        private readonly IVendorRepository _vendorRepository;

        public GetVendorByIdHandler(IVendorRepository vendorRepository)
        {
            ArgumentNullException.ThrowIfNull(vendorRepository);
            _vendorRepository = vendorRepository;
        }

        public async Task<GetVendorByIdResponse> HandleAsync(Guid id, CancellationToken cancellationToken)
        {
            var vendor = await _vendorRepository.GetByIdAsync(id, cancellationToken) ?? throw new VendorNotFoundException(id);
            return Map(vendor);
        }

        private static GetVendorByIdResponse Map(Vendor vendor)
        {
            return new GetVendorByIdResponse(
                Id: vendor.VendorId,
                Name: vendor.Name,
                Address: new AddressResponse(
                    CountryCode: vendor.Address.CountryCode,
                    PostalCode: vendor.Address.PostalCode,
                    City: vendor.Address.City,
                    Street: vendor.Address.Street,
                    PublicSpace: vendor.Address.PublicSpace,
                    HouseNumber: vendor.Address.HouseNumber),
                Webpage: vendor.Webpage,
                Contacts: [.. vendor.Contacts.Select(contact => new ContactResponse(
                    Id: contact.ContactId,
                    FirstName: contact.FirstName,
                    LastName: contact.LastName,
                    JobTitle: contact.JobTitle,
                    PhoneNumber: contact.PhoneNumber,
                    EmailAddress: contact.EmailAddress))]
                );
        }
    }
}
