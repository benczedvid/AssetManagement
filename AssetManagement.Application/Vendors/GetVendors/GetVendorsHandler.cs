using AssetManagement.Application.Common.Interfaces.Vendors;
using AssetManagement.Application.Common.Models;
using AssetManagement.Domain.Entities.Vendors;

namespace AssetManagement.Application.Vendors.GetVendors
{
    public sealed class GetVendorsHandler
    {
        private readonly IVendorRepository _repository;

        public GetVendorsHandler(IVendorRepository repository)
        {
            ArgumentNullException.ThrowIfNull(repository);
            _repository = repository;
        }
        public async Task<IReadOnlyList<GetVendorsResponse>>HandleAsync(CancellationToken cancellationToken)
        {
            var vendors = await _repository.GetAllAsync(cancellationToken);
            return [.. vendors.Select(Map)];
        }

        private static GetVendorsResponse Map(Vendor vendor)
        {
            return new GetVendorsResponse(
                Id: vendor.VendorId,
                Name: vendor.Name,
                Address: new AddressResponse(
                    CountryCode: vendor.Address.CountryCode,
                    PostalCode: vendor.Address.PostalCode,
                    City: vendor.Address.City,
                    Street: vendor.Address.Street,
                    PublicSpace: vendor.Address.PublicSpace,
                    HouseNumber: vendor.Address.HouseNumber),
                Webpage: vendor.Webpage
                );
        }
    }
}
