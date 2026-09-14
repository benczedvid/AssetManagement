using AssetManagement.Domain.Entities.Stores;
using AssetManagement.Domain.Entities.Users;
using AssetManagement.Domain.Entities.Vendors;

namespace AssetManagement.Domain.Entities.Assets;

public sealed class Asset
{
    public Guid Id { get; private set; }
    public string AssetName { get; private set; } = null!;
    public AssetType AssetType { get; private set; }
    public AssetStatus AssetStatus { get; private set; }
    public string Manufacturer { get; private set; } = null!;
    public string Model { get; private set; } = null!;
    public string SerialNumber { get; private set; } = null!;
    public string? RfidTagId { get; private set; }
    public string? AssignedEmployeeNumber { get; private set; }
    public string? MacAddress { get; private set; }
    public string? WiFiMacAddress { get; private set; }
    public string? Imei { get; private set; }
    public string? OperatingSystem { get; private set; }
    public string? OperatingSystemVersion { get; private set; }
    public Store AssignedStore { get; private set; } = null!;
    public Guid AssignedStoreId { get; private set; }
    public Guid? AssignedUserId { get; private set; }
    public ApplicationUser? AssignedUser { get; private set; }
    public DateTime CreatedAtUtc { get; private set; }
    public Guid? AssignedVendorId { get; private set; }
    public Vendor? AssignedVendor { get; private set; }
    private Asset()
    {
    }

    private Asset(
        Guid id,
        string assetName,
        AssetType assetType,
        AssetStatus assetStatus,
        string manufacturer,
        string model,
        string serialNumber,
        Guid assignedStoreId,
        string rfidTagId,
        string? assignedEmployeeNumber,
        Guid? assignedUserId,
        string? macAddress,
        string? wifiMacAddress,
        string? imei,
        string? operatingSystem,
        string? operatingSystemVersion,
        Guid? assignedVendorId
        )
    {
        Id = id;
        AssetName = assetName;
        AssetType = assetType;
        AssetStatus = assetStatus;
        Manufacturer = manufacturer;
        Model = model;
        SerialNumber = serialNumber;
        AssignedStoreId = assignedStoreId;
        RfidTagId = rfidTagId;
        AssignedEmployeeNumber = assignedEmployeeNumber;
        AssignedUserId = assignedUserId;
        MacAddress = macAddress;
        WiFiMacAddress = wifiMacAddress;
        Imei = imei;
        OperatingSystem = operatingSystem;
        OperatingSystemVersion = operatingSystemVersion;
        CreatedAtUtc = DateTime.UtcNow;
        AssignedVendorId = assignedVendorId;
    }

    public static Asset Create(
        string assetName,
        AssetType assetType,
        AssetStatus assetStatus,
        string manufacturer,
        string model,
        string serialNumber,
        Guid assignedStoreId,
        string? rfidTagId = null,
        Guid? assignedVendorId = null,
        string? assignedEmployeeNumber = null,
        Guid? assignedUserId = null,
        string? macAddress = null,
        string? wifiMacAddress = null,
        string? imei = null,
        string? operatingSystem = null,
        string? operatingSystemVersion = null
        )
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(assetName);
        ArgumentException.ThrowIfNullOrWhiteSpace(manufacturer);
        ArgumentException.ThrowIfNullOrWhiteSpace(model);
        ArgumentException.ThrowIfNullOrWhiteSpace(serialNumber);

        ValidateAssetType(assetType);
        ValidateAssetStatus(assetStatus);

        ValidateOptionalValue(rfidTagId, nameof(rfidTagId));
        ValidateOptionalValue(macAddress, nameof(macAddress));
        ValidateOptionalValue(wifiMacAddress, nameof(wifiMacAddress));
        ValidateOptionalValue(imei, nameof(imei));
        ValidateOptionalValue(operatingSystem, nameof(operatingSystem));
        ValidateOptionalValue(operatingSystemVersion, nameof(operatingSystemVersion));
        ValidateOptionalValue(assignedEmployeeNumber, nameof(assignedEmployeeNumber));

        if (assignedStoreId == Guid.Empty)
        {
            throw new ArgumentException("Store identifier cannot be empty.", nameof(assignedStoreId));
        }
        if (assignedUserId == Guid.Empty)
        {
            throw new ArgumentException("User identifier cannot be empty.", nameof(assignedUserId));
        }


        return new Asset(
            Guid.NewGuid(),
            assetName,
            assetType,
            assetStatus,
            manufacturer,
            model,
            serialNumber,
            assignedStoreId,
            rfidTagId,
            assignedEmployeeNumber,
            assignedUserId,
            macAddress,
            wifiMacAddress,
            imei,
            operatingSystem,
            operatingSystemVersion,
            assignedVendorId);
    }
    public void AssignToUser(Guid userId)
    {
        if (userId == Guid.Empty)
        {
            throw new ArgumentException("User identifier cannot be empty.", nameof(userId));
        }
        AssignedUserId = userId;
    }
    public void UnassignFromUser()
    {
        AssignedUserId = null;
    }

    public void Checkout(string employeeNumber)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(employeeNumber);

        if (AssetType != AssetType.PDT)
        {
            throw new InvalidOperationException("Csak PDT típusú eszköz vehető fel.");
        }

        if (AssetStatus != AssetStatus.In_Store)
        {
            throw new InvalidOperationException("Csak áruházban lévő eszköz vehető fel.");
        }

        AssetStatus = AssetStatus.Used_In_Store;
        AssignedEmployeeNumber = employeeNumber.Trim();
    }

    public void Return(string employeeNumber)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(employeeNumber);

        if (AssetType != AssetType.PDT)
        {
            throw new InvalidOperationException("Csak PDT típusú eszköz tehető le.");
        }

        if (AssetStatus != AssetStatus.Used_In_Store)
        {
            throw new InvalidOperationException("Csak használatban lévő eszköz tehető le.");
        }

        if (!string.Equals(AssignedEmployeeNumber, employeeNumber.Trim(), StringComparison.OrdinalIgnoreCase))
        {
            throw new InvalidOperationException("Az eszközt nem ez a dolgozó vette fel.");
        }

        AssetStatus = AssetStatus.In_Store;
        AssignedEmployeeNumber = null;
    }
    private static void ValidateAssetType(AssetType assetType)
    {
        if (assetType == AssetType.Unknown || !Enum.IsDefined(assetType))
        {
            throw new ArgumentOutOfRangeException(nameof(assetType), assetType, "A valid asset type must be specified.");
        }
    }
    private static void ValidateAssetStatus(AssetStatus assetStatus)
    {
        if (assetStatus == AssetStatus.Unknown || !Enum.IsDefined(assetStatus))
        {
            throw new ArgumentOutOfRangeException(nameof(assetStatus), assetStatus, "A valid asset status must be specified.");
        }
    }

    private static void ValidateOptionalValue(string? value, string parameterName)
    {
        if (value is not null && string.IsNullOrWhiteSpace(value))
        {
            throw new ArgumentException("Optional value cannot be empty or whitespace.", parameterName);
        }
    }

    public void UpdateDetails(
            string assetName,
            AssetType assetType,
            AssetStatus assetStatus,
            string manufacturer,
            string model,
            Guid assignedStoreId,
            string rfidTagId,
            Guid? assignedUserId,
            string? macAddress,
            string? wifiMacAddress,
            string? imei,
            string? operatingSystem,
            string? operatingSystemVersion
            )
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(assetName);
        ArgumentException.ThrowIfNullOrWhiteSpace(manufacturer);
        ArgumentException.ThrowIfNullOrWhiteSpace(model);
        ValidateOptionalValue(macAddress, nameof(macAddress));
        ValidateOptionalValue(rfidTagId, nameof(rfidTagId));
        ValidateOptionalValue(wifiMacAddress, nameof(wifiMacAddress));
        ValidateOptionalValue(imei, nameof(imei));
        ValidateOptionalValue(operatingSystem, nameof(operatingSystem));
        ValidateOptionalValue(operatingSystemVersion, nameof(operatingSystemVersion));
        ValidateAssetStatus(assetStatus);
        ValidateAssetType(assetType);
        if (assignedStoreId == Guid.Empty)
        {
            throw new ArgumentException("Store identifier cannot be empty.", nameof(assignedStoreId));
        }

        if (assignedUserId == Guid.Empty)
        {
            throw new ArgumentException("User identifier cannot be empty.", nameof(assignedUserId));
        }

        AssetName = assetName.Trim();
        AssetType = assetType;
        AssetStatus = assetStatus;
        Manufacturer = manufacturer.Trim();
        Model = model.Trim();
        RfidTagId = rfidTagId?.Trim();
        MacAddress = macAddress?.Trim();
        WiFiMacAddress = wifiMacAddress?.Trim();
        Imei = imei?.Trim();
        OperatingSystem = operatingSystem?.Trim();
        OperatingSystemVersion = operatingSystemVersion?.Trim();
        AssignedStoreId = assignedStoreId;
        AssignedUserId = assignedUserId;
    }
    public void AssignVendor(Guid vendorId)
    {
        if (vendorId == Guid.Empty)
        {
            throw new ArgumentException("Vendor identifier cannot be empty.", nameof(vendorId));
        }

        AssignedVendorId = vendorId;
    }

    public void RemoveVendor()
    {
        AssignedVendorId = null;
    }
}

//TODO: Add vendor
//TODO: Add acquisition date
//TODO: Add Purchase order
//TODO: Add warranty expiry date