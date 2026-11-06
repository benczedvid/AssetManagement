namespace AssetManagement.Application.Vendors
{
    public sealed class VendorNotFoundException : Exception
    {
        public VendorNotFoundException(Guid id) : base($"The vendor with identifier '{id}' was not found.")
        {

            Id = id;
        }

        public Guid Id { get; }
    }
}
