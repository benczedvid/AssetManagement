namespace AssetManagement.Application.Assets
{
    public sealed class AssetNotFoundException : Exception
    {
        public AssetNotFoundException(Guid id) : base($"The asset with identifier '{id}' was not found.")
        {
            Id = id;
        }

        public AssetNotFoundException(string serialNumber) : base($"The asset with serialNumber '{serialNumber}' was not found.")
        {
            SerialNumber = serialNumber;
        }
        public Guid Id { get; }
        public string SerialNumber { get; }
    }
}
