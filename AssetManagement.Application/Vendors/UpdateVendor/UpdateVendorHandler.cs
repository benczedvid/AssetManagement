using AssetManagement.Application.Common.Interfaces.Vendors;
using AssetManagement.Application.Common.Models;
using AssetManagement.Domain.Entities.Vendors;

namespace AssetManagement.Application.Vendors.UpdateVendor
{
    public sealed class UpdateVendorHandler
    {
        private readonly IVendorRepository _repository;

        public UpdateVendorHandler(IVendorRepository repository)
        {
            ArgumentNullException.ThrowIfNull(repository);
            _repository = repository;
        }

        public async Task<UpdateVendorResponse> HandleAsync(Guid id, UpdateVendorRequest request, CancellationToken cancellationToken)
        {
            ArgumentNullException.ThrowIfNull(request);

            var existingVendor =await  _repository.GetForUpdateByIdAsync(id, cancellationToken) ?? throw new VendorNotFoundException(id);
            existingVendor.UpdateDetails(
                name: request.Name,
                countryCode: request.Address.CountryCode,
                postalCode:  request.Address.PostalCode,
                city: request.Address.City,
                street: request.Address.Street,
                publicSpace: request.Address.PublicSpace,
                houseNumber: request.Address.HouseNumber,
                webpage: request.Webpage);

            await _repository.SaveChangesAsync(cancellationToken);

            return Map(existingVendor);
        }
        private static UpdateVendorResponse Map(Vendor vendor)
        {
            return new UpdateVendorResponse(
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
