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
    public DateOnly? WarrantyEndDate { get; private set; }
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
        string? rfidTagId,
        string? assignedEmployeeNumber,
        Guid? assignedUserId,
        string? macAddress,
        string? wifiMacAddress,
        string? imei,
        string? operatingSystem,
        string? operatingSystemVersion,
        Guid? assignedVendorId,
        DateOnly? warrantyEndDate
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
        WarrantyEndDate = warrantyEndDate;
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
    string? operatingSystemVersion = null,
    DateOnly? warrantyEndDate = null)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(assetName);
        ArgumentException.ThrowIfNullOrWhiteSpace(manufacturer);
        ArgumentException.ThrowIfNullOrWhiteSpace(model);
        ArgumentException.ThrowIfNullOrWhiteSpace(serialNumber);

        ValidateAssetType(assetType);
        ValidateAssetStatus(assetStatus);

        ValidateOptionalValue(rfidTagId, nameof(rfidTagId));
        ValidateOptionalValue(assignedEmployeeNumber, nameof(assignedEmployeeNumber));
        ValidateOptionalValue(macAddress, nameof(macAddress));
        ValidateOptionalValue(wifiMacAddress, nameof(wifiMacAddress));
        ValidateOptionalValue(imei, nameof(imei));
        ValidateOptionalValue(operatingSystem, nameof(operatingSystem));
        ValidateOptionalValue(operatingSystemVersion, nameof(operatingSystemVersion));

        if (assignedStoreId == Guid.Empty)
        {
            throw new ArgumentException("Store identifier cannot be empty.", nameof(assignedStoreId));
        }

        if (assignedUserId == Guid.Empty)
        {
            throw new ArgumentException("User identifier cannot be empty.", nameof(assignedUserId));
        }

        if (assignedVendorId == Guid.Empty)
        {
            throw new ArgumentException("Vendor identifier cannot be empty.",  nameof(assignedVendorId));
        }


        return new Asset(
            id: Guid.NewGuid(),
            assetName: assetName.Trim(),
            assetType: assetType,
            assetStatus: assetStatus,
            manufacturer: manufacturer.Trim(),
            model: model.Trim(),
            serialNumber: serialNumber.Trim(),
            assignedStoreId: assignedStoreId,
            rfidTagId: rfidTagId?.Trim(),
            assignedEmployeeNumber: assignedEmployeeNumber?.Trim(),
            assignedUserId: assignedUserId,
            macAddress: macAddress?.Trim(),
            wifiMacAddress: wifiMacAddress?.Trim(),
            imei: imei?.Trim(),
            operatingSystem: operatingSystem?.Trim(),
            operatingSystemVersion: operatingSystemVersion?.Trim(),
            assignedVendorId: assignedVendorId,
            warrantyEndDate: warrantyEndDate);
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

    public void SendToHeadquartersForServiceInspection()
    {
        if (AssignedEmployeeNumber is not null)
        {
            throw new InvalidOperationException("The asset must be returned by the store employee before a service case can be opened.");
        }

        if (AssetStatus != AssetStatus.In_Store)
        {
            throw new InvalidOperationException("Only an asset located in a store can be sent to headquarters for service inspection.");
        }

        AssetStatus = AssetStatus.Route_To_HQ;
    }
    public void ReceiveAtHeadquartersForServiceInspection()
    {
    if (AssetStatus != AssetStatus.Route_To_HQ)
    {
        throw new InvalidOperationException("Only an asset being routed to headquarters can be received for service inspection.");
    }

    AssetStatus = AssetStatus.In_HQ;
    }
    public void DispatchToExternalService()
    {
        if (AssetStatus != AssetStatus.In_HQ)
        {
            throw new InvalidOperationException("Only an asset located at headquarters can be sent to external service.");
        }

        AssetStatus = AssetStatus.Route_To_Service;
    }
    public void HandOverToExternalService()
    {
        if (AssetStatus != AssetStatus.In_HQ)
        {
            throw new InvalidOperationException("Only an asset being routed to external service can be handed over to the external service.");
        }

        AssetStatus = AssetStatus.In_Service;
    }
    public void ConfirmReceivedByExternalService()
    {
        if (AssetStatus != AssetStatus.Route_To_Service)
        {
            throw new InvalidOperationException("Only an asset being routed to external service can be marked as received by the external service.");
        }

        AssetStatus = AssetStatus.In_Service;
    }
    public void ReceiveFromExternalService()
    {
        if (AssetStatus != AssetStatus.In_Service)
        {
            throw new InvalidOperationException("Only an asset currently in external service can be received from external service.");
        }

        AssetStatus = AssetStatus.In_HQ;
    }
    public void ReturnFromExternalService()
    {
        if (AssetStatus != AssetStatus.In_Service)
        {
            throw new InvalidOperationException("Only an asset currently in service can be returned from external service.");
        }

        AssetStatus = AssetStatus.In_HQ;
    }

    public void DispatchToStoreAfterService()
    {
        if (AssetStatus != AssetStatus.In_HQ)
        {
            throw new InvalidOperationException("Only an asset located at headquarters can be dispatched to a store after service.");
        }

        AssetStatus = AssetStatus.Route_To_Store;
    }
    public void ReceiveAtStoreAfterService()
{
    if (AssetStatus != AssetStatus.Route_To_Store)
    {
        throw new InvalidOperationException("Only an asset being routed to a store can be received after service.");
    }

    AssetStatus = AssetStatus.In_Store;
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
            string? rfidTagId,
            Guid? assignedUserId,
            string? macAddress,
            string? wifiMacAddress,
            string? imei,
            string? operatingSystem,
            string? operatingSystemVersion,
            DateOnly? warrantyEndDate
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
        WarrantyEndDate = warrantyEndDate;
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
    /// <summary>
    /// Marks the asset as disposed.
    /// </summary>
    /// <remarks>
    /// Assets located in a store, at headquarters, or at an external
    /// service provider can be disposed.
    /// An asset may remain at the external service provider for donor parts.
    /// The asset must be returned from employee use and unassigned
    /// from its application user before disposal.
    /// Store and vendor assignments are preserved.
    /// </remarks>
    /// <exception cref="InvalidOperationException">
    /// The asset is already disposed, is still assigned to a user,
    /// or its current status does not allow disposal.
    /// </exception>
    public void Dispose()
    {
        if (AssetStatus == AssetStatus.Disposed)
        {
            throw new InvalidOperationException("The asset has already been disposed.");
        }

        if (AssignedEmployeeNumber is not null)
        {
            throw new InvalidOperationException("The asset must be returned by the employee before it can be disposed.");
        }

        if (AssignedUserId.HasValue)
        {
            throw new InvalidOperationException("The asset must be unassigned from its user before it can be disposed.");
        }

        if (AssetStatus is not (
            AssetStatus.In_Store or
            AssetStatus.In_HQ or
            AssetStatus.In_Service))
        {
            throw new InvalidOperationException("Only an asset located in a store, at headquarters, or at an external service provider can be disposed.");
        }

        AssetStatus = AssetStatus.Disposed;
    }
}
