using AssetManagement.Domain.Entities.Assets;

namespace AssetManagement.Tests.Builders
{
    public sealed class AssetBuilder
    {
        private string? _assetName = "HUHQLAP0287";
        private AssetType _assetType = AssetType.Notebook;
        private AssetStatus _assetStatus = AssetStatus.In_HQ;
        private string? _manufacturer = "Lenovo";
        private string? _model = "21JN0008HV";
        private string? _serialNumber = "PF4KVG84";
        private Guid _assignedStoreId = Guid.NewGuid();
        private string? _rfidTagId = "RFID-0001";
        private Guid? _assignedVendorId;
        private string? _assignedEmployeeNumber;
        private Guid? _assignedUserId = Guid.NewGuid();
        private string? _macAddress = "74:5D:22:3A:76:C7";
        private string? _wifiMacAddress = "00:00:00:00:00:00";
        private string? _imei = "123456789123456";
        private string? _operatingSystem = "Windows";
        private string? _operatingSystemVersion = "24H2";

        public AssetBuilder WithAssetName(string? assetName) { _assetName = assetName; return this; }
        public AssetBuilder WithAssetType(AssetType assetType) { _assetType = assetType; return this; }
        public AssetBuilder WithAssetStatus(AssetStatus assetStatus) { _assetStatus = assetStatus; return this; }
        public AssetBuilder WithManufacturer(string? manufacturer) { _manufacturer = manufacturer; return this; }
        public AssetBuilder WithModel(string? model) { _model = model; return this; }
        public AssetBuilder WithSerialNumber(string? serialNumber) { _serialNumber = serialNumber; return this; }
        public AssetBuilder WithAssignedStoreId(Guid assignedStoreId) { _assignedStoreId = assignedStoreId; return this; }
        public AssetBuilder WithRfidTagId(string? rfidTagId) { _rfidTagId = rfidTagId; return this; }
        public AssetBuilder WithoutRfidTag() { _rfidTagId = null; return this; }
        public AssetBuilder WithAssignedVendorId( Guid? assignedVendorId) { _assignedVendorId = assignedVendorId; return this; }
        public AssetBuilder WithoutVendor() { _assignedVendorId = null; return this; }
        public AssetBuilder WithAssignedEmployeeNumber( string? assignedEmployeeNumber) { _assignedEmployeeNumber = assignedEmployeeNumber; return this; }
        public AssetBuilder WithAssignedUserId(Guid? assignedUserId) { _assignedUserId = assignedUserId; return this; }
        public AssetBuilder WithoutAssignedUser() { _assignedUserId = null; return this; }
        public AssetBuilder WithMacAddress(string? macAddress) { _macAddress = macAddress; return this; }
        public AssetBuilder WithWiFiMacAddress(string? wifiMacAddress) { _wifiMacAddress = wifiMacAddress; return this; }
        public AssetBuilder WithImei(string? imei) { _imei = imei; return this; }
        public AssetBuilder WithOperatingSystem(string? operatingSystem) { _operatingSystem = operatingSystem; return this; }
        public AssetBuilder WithOperatingSystemVersion(string? operatingSystemVersion) { _operatingSystemVersion = operatingSystemVersion; return this; }
        public AssetBuilder AsPdtInStore() {  _assetType = AssetType.PDT; _assetStatus = AssetStatus.In_Store; _assignedEmployeeNumber = null; return this; }
        public AssetBuilder AsCheckedOutPdt(string employeeNumber = "103549") { _assetType = AssetType.PDT; _assetStatus = AssetStatus.Used_In_Store; _assignedEmployeeNumber = employeeNumber; return this; }
        public Asset Build()
        {
            return Asset.Create(
                assetName: _assetName!,
                assetType: _assetType,
                assetStatus: _assetStatus,
                manufacturer: _manufacturer!,
                model: _model!,
                serialNumber: _serialNumber!,
                assignedStoreId: _assignedStoreId,
                rfidTagId: _rfidTagId,
                assignedVendorId: _assignedVendorId,
                assignedEmployeeNumber: _assignedEmployeeNumber,
                assignedUserId: _assignedUserId,
                macAddress: _macAddress,
                wifiMacAddress: _wifiMacAddress,
                imei: _imei,
                operatingSystem: _operatingSystem,
                operatingSystemVersion: _operatingSystemVersion);
        }
    }
}