
using AssetManagement.Application.Common.Interfaces.Vendors;
using AssetManagement.Application.Common.Models;
using AssetManagement.Domain.Entities.Vendors;

namespace AssetManagement.Application.Vendors.CreateVendor
{
    public sealed class CreateVendorHandler
    {
        private readonly IVendorRepository _vendorRepository;

        public CreateVendorHandler(IVendorRepository vendorRepository)
        {
            ArgumentNullException.ThrowIfNull(vendorRepository);
            _vendorRepository = vendorRepository;
        }

        public async Task<CreateVendorResponse> HandleAsync(CreateVendorRequest request, CancellationToken cancellationToken)
        {
            ArgumentNullException.ThrowIfNull(request);

            var existingVendor = await _vendorRepository.GetByNameAsync(request.Name, cancellationToken);

            if(existingVendor is not null)
            {
                throw new VendorAlreadyExistsException(request.Name);
            }

            var newVendor = Vendor.Create(
                name: request.Name,
                countryCode: request.Address.CountryCode,
                postalCode: request.Address.PostalCode,
                city: request.Address.City,
                street: request.Address.Street,
                publicSpace: request.Address.PublicSpace,
                houseNumber: request.Address.HouseNumber,
                webpage: request.Webpage
                );

            await _vendorRepository.AddAsync(newVendor, cancellationToken);
            await _vendorRepository.SaveChangesAsync(cancellationToken);

            return new CreateVendorResponse(
                Id: newVendor.VendorId,
                Name: newVendor.Name,
                Address: new AddressResponse(
                    CountryCode: newVendor.Address.CountryCode,
                    PostalCode: newVendor.Address.PostalCode,
                    City: newVendor.Address.City,
                    Street:  newVendor.Address.Street,
                    PublicSpace: newVendor.Address.PublicSpace,
                    HouseNumber: newVendor.Address.HouseNumber),
                Webpage: newVendor.Webpage
                );
        }
    }
}
