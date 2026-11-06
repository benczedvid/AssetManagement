namespace AssetManagement.Application.Vendors
{
    public sealed class VendorAlreadyExistsException : Exception
    {
        public VendorAlreadyExistsException(string name) : base($"A vendor with vendor name {name} already exists.")
        {
            Name = name;
        }
        public string Name { get; }
    }
}
