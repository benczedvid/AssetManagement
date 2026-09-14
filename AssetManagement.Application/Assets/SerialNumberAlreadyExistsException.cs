namespace AssetManagement.Application.Assets
{
    public sealed class SerialNumberAlreadyExistsException : Exception
    {
    public SerialNumberAlreadyExistsException(string serialNumber) : base($"The asset with serial number '{serialNumber}' was not found.")
        {
            SerialNumber = serialNumber;
        }

    public string SerialNumber { get; }
}
}
