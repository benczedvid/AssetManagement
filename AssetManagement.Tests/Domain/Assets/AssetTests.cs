using AssetManagement.Domain.Entities.Assets;
using AssetManagement.Tests.Builders;

namespace AssetManagement.Tests.Domain.Assets
{
    [TestClass]
    public sealed class AssetTests
    {
        [TestMethod]
        public void Create_Should_Create_Asset_With_All_Data()
        {
            const string assetName = "HUHQLAP0287";
            const AssetType assetType = AssetType.Notebook;
            const AssetStatus assetStatus = AssetStatus.In_HQ;
            const string manufacturer = "Lenovo";
            const string model = "21JN0008HV";
            const string serialNumber = "PF4KVG84";
            const string rfidTagId = "RFID-0287";
            const string employeeNumber = "103549";
            const string macAddress = "74:5D:22:3A:76:C7";
            const string wifiMacAddress = "00:00:00:00:00:00";
            const string imei = "123456789123456";
            const string operatingSystem = "Windows";
            const string operatingSystemVersion = "24H2";

            var assignedStoreId = Guid.NewGuid();
            var assignedUserId = Guid.NewGuid();
            var assignedVendorId = Guid.NewGuid();

            var beforeCreation = DateTime.UtcNow;

            var asset = new AssetBuilder()
                .WithAssetName(assetName)
                .WithAssetType(assetType)
                .WithAssetStatus(assetStatus)
                .WithManufacturer(manufacturer)
                .WithModel(model)
                .WithSerialNumber(serialNumber)
                .WithAssignedStoreId(assignedStoreId)
                .WithRfidTagId(rfidTagId)
                .WithAssignedVendorId(assignedVendorId)
                .WithAssignedEmployeeNumber(employeeNumber)
                .WithAssignedUserId(assignedUserId)
                .WithMacAddress(macAddress)
                .WithWiFiMacAddress(wifiMacAddress)
                .WithImei(imei)
                .WithOperatingSystem(operatingSystem)
                .WithOperatingSystemVersion(operatingSystemVersion)
                .Build();

            var afterCreation = DateTime.UtcNow;

            Assert.AreNotEqual(Guid.Empty, asset.Id);
            Assert.AreEqual(assetName, asset.AssetName);
            Assert.AreEqual(assetType, asset.AssetType);
            Assert.AreEqual(assetStatus, asset.AssetStatus);
            Assert.AreEqual(manufacturer, asset.Manufacturer);
            Assert.AreEqual(model, asset.Model);
            Assert.AreEqual(serialNumber, asset.SerialNumber);
            Assert.AreEqual(assignedStoreId, asset.AssignedStoreId);
            Assert.AreEqual(rfidTagId, asset.RfidTagId);
            Assert.AreEqual(employeeNumber, asset.AssignedEmployeeNumber);
            Assert.AreEqual(assignedUserId, asset.AssignedUserId);
            Assert.AreEqual(assignedVendorId, asset.AssignedVendorId);
            Assert.AreEqual(macAddress, asset.MacAddress);
            Assert.AreEqual(wifiMacAddress, asset.WiFiMacAddress);
            Assert.AreEqual(imei, asset.Imei);
            Assert.AreEqual(operatingSystem, asset.OperatingSystem);
            Assert.AreEqual(operatingSystemVersion, asset.OperatingSystemVersion);

            Assert.IsTrue(asset.CreatedAtUtc >= beforeCreation);
            Assert.IsTrue(asset.CreatedAtUtc <= afterCreation);

            Assert.IsNull(asset.AssignedStore);
            Assert.IsNull(asset.AssignedUser);
            Assert.IsNull(asset.AssignedVendor);
        }

        [TestMethod]
        public void Create_Should_Create_Asset_Without_Optional_Data()
        {
            var asset = new AssetBuilder()
                .WithoutRfidTag()
                .WithoutAssignedUser()
                .WithoutVendor()
                .WithAssignedEmployeeNumber(null)
                .WithMacAddress(null)
                .WithWiFiMacAddress(null)
                .WithImei(null)
                .WithOperatingSystem(null)
                .WithOperatingSystemVersion(null)
                .Build();

            Assert.IsNull(asset.RfidTagId);
            Assert.IsNull(asset.AssignedEmployeeNumber);
            Assert.IsNull(asset.AssignedUserId);
            Assert.IsNull(asset.AssignedUser);
            Assert.IsNull(asset.AssignedVendorId);
            Assert.IsNull(asset.AssignedVendor);
            Assert.IsNull(asset.MacAddress);
            Assert.IsNull(asset.WiFiMacAddress);
            Assert.IsNull(asset.Imei);
            Assert.IsNull(asset.OperatingSystem);
            Assert.IsNull(asset.OperatingSystemVersion);
        }

        [TestMethod]
        public void Create_Should_Generate_Unique_Id()
        {
            var firstAsset = new AssetBuilder().Build();
            var secondAsset = new AssetBuilder().Build();

            Assert.AreNotEqual(Guid.Empty, firstAsset.Id);
            Assert.AreNotEqual(Guid.Empty, secondAsset.Id);
            Assert.AreNotEqual(firstAsset.Id, secondAsset.Id);
        }

        [TestMethod]
        public void Create_Should_Trim_Text_Values()
        {
            var asset = new AssetBuilder()
                .WithAssetName("  HUHQLAP0287  ")
                .WithManufacturer("  Lenovo  ")
                .WithModel("  21JN0008HV  ")
                .WithSerialNumber("  PF4KVG84  ")
                .WithRfidTagId("  RFID-0287  ")
                .WithAssignedEmployeeNumber("  103549  ")
                .WithMacAddress("  74:5D:22:3A:76:C7  ")
                .WithWiFiMacAddress("  00:00:00:00:00:00  ")
                .WithImei("  123456789123456  ")
                .WithOperatingSystem("  Windows  ")
                .WithOperatingSystemVersion("  24H2  ")
                .Build();

            Assert.AreEqual("HUHQLAP0287", asset.AssetName);
            Assert.AreEqual("Lenovo", asset.Manufacturer);
            Assert.AreEqual("21JN0008HV", asset.Model);
            Assert.AreEqual("PF4KVG84", asset.SerialNumber);
            Assert.AreEqual("RFID-0287", asset.RfidTagId);
            Assert.AreEqual("103549",  asset.AssignedEmployeeNumber);
            Assert.AreEqual("74:5D:22:3A:76:C7", asset.MacAddress);
            Assert.AreEqual("00:00:00:00:00:00", asset.WiFiMacAddress);
            Assert.AreEqual("123456789123456", asset.Imei);
            Assert.AreEqual("Windows", asset.OperatingSystem);
            Assert.AreEqual("24H2", asset.OperatingSystemVersion);
        }

        [TestMethod]
        public void Create_Should_Throw_When_AssetName_Is_Null()
        {
            var builder = new AssetBuilder()
                .WithAssetName(null);

            var exception = Assert.ThrowsExactly<ArgumentNullException>(() => builder.Build());

            Assert.AreEqual("assetName", exception.ParamName);
        }

        [TestMethod]
        [DataRow("")]
        [DataRow(" ")]
        [DataRow("  ")]
        [DataRow("\t")]
        [DataRow("\r\n")]
        public void Create_Should_Throw_When_AssetName_Is_Whitespace(string invalidValue)
        {
            var builder = new AssetBuilder()
                .WithAssetName(invalidValue);

            var exception = Assert.ThrowsExactly<ArgumentException>(() => builder.Build());

            Assert.AreEqual("assetName", exception.ParamName);
        }

        [TestMethod]
        public void Create_Should_Throw_When_Manufacturer_Is_Null()
        {
            var builder = new AssetBuilder()
                .WithManufacturer(null);

            var exception = Assert.ThrowsExactly<ArgumentNullException>(() => builder.Build());

            Assert.AreEqual("manufacturer", exception.ParamName);
        }

        [TestMethod]
        [DataRow("")]
        [DataRow(" ")]
        [DataRow("\t")]
        public void Create_Should_Throw_When_Manufacturer_Is_Whitespace(string invalidValue)
        {
            var builder = new AssetBuilder()
                .WithManufacturer(invalidValue);

            var exception = Assert.ThrowsExactly<ArgumentException>(() => builder.Build());

            Assert.AreEqual("manufacturer", exception.ParamName);
        }

        [TestMethod]
        public void Create_Should_Throw_When_Model_Is_Null()
        {
            var builder = new AssetBuilder()
                .WithModel(null);

            var exception =  Assert.ThrowsExactly<ArgumentNullException>(() => builder.Build());

            Assert.AreEqual("model", exception.ParamName);
        }

        [TestMethod]
        [DataRow("")]
        [DataRow(" ")]
        [DataRow("\t")]
        public void Create_Should_Throw_When_Model_Is_Whitespace(string invalidValue)
        {
            var builder = new AssetBuilder()
                .WithModel(invalidValue);
            
            var exception = Assert.ThrowsExactly<ArgumentException>(() => builder.Build());

            Assert.AreEqual("model", exception.ParamName);
        }

        [TestMethod]
        public void Create_Should_Throw_When_SerialNumber_Is_Null()
        {
            var builder = new AssetBuilder()
                .WithSerialNumber(null);

            var exception = Assert.ThrowsExactly<ArgumentNullException>(() => builder.Build());

            Assert.AreEqual("serialNumber", exception.ParamName);
        }

        [TestMethod]
        [DataRow("")]
        [DataRow(" ")]
        [DataRow("\t")]
        public void Create_Should_Throw_When_SerialNumber_Is_Whitespace(string invalidValue)
        {
            var builder = new AssetBuilder()
                .WithSerialNumber(invalidValue);

            var exception = Assert.ThrowsExactly<ArgumentException>(() => builder.Build());

            Assert.AreEqual("serialNumber", exception.ParamName);
        }

        [TestMethod]
        public void Create_Should_Throw_When_AssetType_Is_Unknown()
        {
            var builder = new AssetBuilder()
                .WithAssetType(AssetType.Unknown);

            var exception = Assert.ThrowsExactly<ArgumentOutOfRangeException>(() => builder.Build());

            Assert.AreEqual("assetType", exception.ParamName);
        }

        [TestMethod]
        public void Create_Should_Throw_When_AssetType_Is_Not_Defined()
        {
            var builder = new AssetBuilder()
                .WithAssetType((AssetType)int.MaxValue);

            var exception = Assert.ThrowsExactly<ArgumentOutOfRangeException>(() => builder.Build());

            Assert.AreEqual("assetType", exception.ParamName);
        }

        [TestMethod]
        public void Create_Should_Throw_When_AssetStatus_Is_Unknown()
        {
            var builder = new AssetBuilder()
                .WithAssetStatus(AssetStatus.Unknown);
            
            var exception = Assert.ThrowsExactly<ArgumentOutOfRangeException>(() => builder.Build());

            Assert.AreEqual("assetStatus", exception.ParamName);
        }

        [TestMethod]
        public void Create_Should_Throw_When_AssetStatus_Is_Not_Defined()
        {
            var builder = new AssetBuilder()
                .WithAssetStatus((AssetStatus)int.MaxValue);

            var exception = Assert.ThrowsExactly<ArgumentOutOfRangeException>(() => builder.Build());

            Assert.AreEqual("assetStatus", exception.ParamName);
        }

        [TestMethod]
        public void Create_Should_Throw_When_AssignedStoreId_Is_Empty()
        {
            var builder = new AssetBuilder()
                .WithAssignedStoreId(Guid.Empty);

            var exception = Assert.ThrowsExactly<ArgumentException>(() => builder.Build());

            Assert.AreEqual("assignedStoreId", exception.ParamName);
        }

        [TestMethod]
        public void Create_Should_Throw_When_AssignedUserId_Is_Empty()
        {
            var builder = new AssetBuilder()
                .WithAssignedUserId(Guid.Empty);

            var exception = Assert.ThrowsExactly<ArgumentException>(() => builder.Build());

            Assert.AreEqual("assignedUserId", exception.ParamName);
        }

        [TestMethod]
        public void Create_Should_Throw_When_AssignedVendorId_Is_Empty()
        {
            var builder = new AssetBuilder()
                .WithAssignedVendorId(Guid.Empty);

            var exception = Assert.ThrowsExactly<ArgumentException>(() => builder.Build());

            Assert.AreEqual("assignedVendorId", exception.ParamName);
        }

        [TestMethod]
        [DataRow("")]
        [DataRow(" ")]
        [DataRow("\t")]
        public void Create_Should_Throw_When_RfidTagId_Is_Whitespace(string invalidValue)
        {
            var builder = new AssetBuilder()
                .WithRfidTagId(invalidValue);

            var exception = Assert.ThrowsExactly<ArgumentException>(() => builder.Build());

            Assert.AreEqual("rfidTagId", exception.ParamName);
        }

        [TestMethod]
        [DataRow("")]
        [DataRow(" ")]
        [DataRow("\t")]
        public void Create_Should_Throw_When_MacAddress_Is_Whitespace(string invalidValue)
        {
            var builder = new AssetBuilder()
                .WithMacAddress(invalidValue);

            var exception = Assert.ThrowsExactly<ArgumentException>(() => builder.Build());

            Assert.AreEqual("macAddress", exception.ParamName);
        }

        [TestMethod]
        [DataRow("")]
        [DataRow(" ")]
        [DataRow("\t")]
        public void Create_Should_Throw_When_WiFiMacAddress_Is_Whitespace(string invalidValue)
        {
            var builder = new AssetBuilder()
                .WithWiFiMacAddress(invalidValue);

            var exception = Assert.ThrowsExactly<ArgumentException>(() => builder.Build());

            Assert.AreEqual("wifiMacAddress", exception.ParamName);
        }

        [TestMethod]
        [DataRow("")]
        [DataRow(" ")]
        [DataRow("\t")]
        public void Create_Should_Throw_When_Imei_Is_Whitespace(string invalidValue)
        {
            var builder = new AssetBuilder()
                .WithImei(invalidValue);

            var exception = Assert.ThrowsExactly<ArgumentException>(() => builder.Build());

            Assert.AreEqual("imei", exception.ParamName);
        }

        [TestMethod]
        public void AssignToUser_Should_Set_AssignedUserId()
        {
            var asset = new AssetBuilder()
                .WithoutAssignedUser()
                .Build();

            var userId = Guid.NewGuid();

            asset.AssignToUser(userId);

            Assert.AreEqual(userId, asset.AssignedUserId);
        }

        [TestMethod]
        public void AssignToUser_Should_Throw_When_UserId_Is_Empty()
        {
            var asset = new AssetBuilder().Build();

            var exception = Assert.ThrowsExactly<ArgumentException>(() => asset.AssignToUser(Guid.Empty));

            Assert.AreEqual("userId", exception.ParamName);
        }

        [TestMethod]
        public void UnassignFromUser_Should_Clear_AssignedUserId()
        {
            var asset = new AssetBuilder()
                .WithAssignedUserId(Guid.NewGuid())
                .Build();

            asset.UnassignFromUser();

            Assert.IsNull(asset.AssignedUserId);
            Assert.IsNull(asset.AssignedUser);
        }

        [TestMethod]
        public void Checkout_Should_Check_Out_Pdt_To_Employee()
        {
            var asset = new AssetBuilder()
                .AsPdtInStore()
                .Build();

            asset.Checkout("  103549  ");

            Assert.AreEqual(AssetStatus.Used_In_Store, asset.AssetStatus);
            Assert.AreEqual("103549", asset.AssignedEmployeeNumber);
        }

        [TestMethod]
        public void Checkout_Should_Throw_When_EmployeeNumber_Is_Null()
        {
            var asset = new AssetBuilder()
                .AsPdtInStore()
                .Build();

            var exception = Assert.ThrowsExactly<ArgumentNullException>(() => asset.Checkout(null!));

            Assert.AreEqual("employeeNumber", exception.ParamName);
        }

        [TestMethod]
        [DataRow("")]
        [DataRow(" ")]
        [DataRow("\t")]
        public void Checkout_Should_Throw_When_EmployeeNumber_Is_Whitespace(
            string invalidValue)
        {
            var asset = new AssetBuilder()
                .AsPdtInStore()
                .Build();

            var exception = Assert.ThrowsExactly<ArgumentException>(() => asset.Checkout(invalidValue));

            Assert.AreEqual("employeeNumber",exception.ParamName);
        }

        [TestMethod]
        public void Checkout_Should_Throw_When_Asset_Is_Not_Pdt()
        {
            var asset = new AssetBuilder()
                .WithAssetType(AssetType.Notebook)
                .WithAssetStatus(AssetStatus.In_Store)
                .Build();

            Assert.ThrowsExactly<InvalidOperationException>(() => asset.Checkout("103549"));
            Assert.AreEqual(AssetStatus.In_Store, asset.AssetStatus);
            Assert.IsNull(asset.AssignedEmployeeNumber);
        }

        [TestMethod]
        public void Checkout_Should_Throw_When_Pdt_Is_Not_In_Store()
        {
            var asset = new AssetBuilder()
                .WithAssetType(AssetType.PDT)
                .WithAssetStatus(AssetStatus.In_HQ)
                .Build();

            Assert.ThrowsExactly<InvalidOperationException>(() => asset.Checkout("103549"));
        }

        [TestMethod]
        public void Return_Should_Return_Pdt_To_Store()
        {
            var asset = new AssetBuilder()
                .AsCheckedOutPdt("103549")
                .Build();

            asset.Return("  103549  ");

            Assert.AreEqual(AssetStatus.In_Store, asset.AssetStatus);
            Assert.IsNull(asset.AssignedEmployeeNumber);
        }

        [TestMethod]
        public void Return_Should_Match_EmployeeNumber_Ignoring_Case()
        {
            var asset = new AssetBuilder()
                .AsCheckedOutPdt("EMPLOYEE01")
                .Build();

            asset.Return("employee01");

            Assert.AreEqual( AssetStatus.In_Store, asset.AssetStatus);
            Assert.IsNull(asset.AssignedEmployeeNumber);
        }

        [TestMethod]
        public void Return_Should_Throw_For_Different_Employee()
        {
            var asset = new AssetBuilder()
                .AsCheckedOutPdt("103549")
                .Build();

            Assert.ThrowsExactly<InvalidOperationException>(() => asset.Return("999999"));

            Assert.AreEqual(AssetStatus.Used_In_Store, asset.AssetStatus);
            Assert.AreEqual("103549", asset.AssignedEmployeeNumber);
        }

        [TestMethod]
        public void Return_Should_Throw_When_Asset_Is_Not_Pdt()
        {
            var asset = new AssetBuilder()
                .WithAssetType(AssetType.Notebook)
                .WithAssetStatus(AssetStatus.Used_In_Store)
                .WithAssignedEmployeeNumber("103549")
                .Build();

            Assert.ThrowsExactly<InvalidOperationException>(() => asset.Return("103549"));
        }

        [TestMethod]
        public void Return_Should_Throw_When_Pdt_Is_Not_In_Use()
        {
            var asset = new AssetBuilder()
                .AsPdtInStore()
                .Build();

            Assert.ThrowsExactly<InvalidOperationException>(() => asset.Return("103549"));
        }

        [TestMethod]
        public void UpdateDetails_Should_Update_Editable_Properties()
        {
            var asset = new AssetBuilder().Build();

            var originalId = asset.Id;
            var originalSerialNumber = asset.SerialNumber;
            var originalCreatedAtUtc = asset.CreatedAtUtc;
            var storeId = Guid.NewGuid();
            var userId = Guid.NewGuid();

            asset.UpdateDetails(
                assetName: "  HUHQLAP9999  ",
                assetType: AssetType.Notebook,
                assetStatus: AssetStatus.In_HQ,
                manufacturer: "  Dell  ",
                model: "  Latitude 5550  ",
                assignedStoreId: storeId,
                rfidTagId: "  RFID-9999  ",
                assignedUserId: userId,
                macAddress: "  A1:A2:A3:A4:A5:A6  ",
                wifiMacAddress: null,
                imei: null,
                operatingSystem: "  Windows  ",
                operatingSystemVersion: "  24H2  ");

            Assert.AreEqual(originalId, asset.Id);
            Assert.AreEqual(originalSerialNumber, asset.SerialNumber);
            Assert.AreEqual(originalCreatedAtUtc, asset.CreatedAtUtc);
            Assert.AreEqual("HUHQLAP9999", asset.AssetName);
            Assert.AreEqual(AssetType.Notebook, asset.AssetType);
            Assert.AreEqual(AssetStatus.In_HQ, asset.AssetStatus);
            Assert.AreEqual("Dell", asset.Manufacturer);
            Assert.AreEqual("Latitude 5550", asset.Model);
            Assert.AreEqual(storeId, asset.AssignedStoreId);
            Assert.AreEqual(userId, asset.AssignedUserId);
            Assert.AreEqual("RFID-9999", asset.RfidTagId);
            Assert.AreEqual("A1:A2:A3:A4:A5:A6", asset.MacAddress);
            Assert.IsNull(asset.WiFiMacAddress);
            Assert.IsNull(asset.Imei);
            Assert.AreEqual("Windows", asset.OperatingSystem);
            Assert.AreEqual("24H2", asset.OperatingSystemVersion);
        }

        [TestMethod]
        public void UpdateDetails_Should_Allow_Null_Optional_Values()
        {
            var asset = new AssetBuilder().Build();

            asset.UpdateDetails(
                assetName: "Updated asset",
                assetType: AssetType.Notebook,
                assetStatus: AssetStatus.In_HQ,
                manufacturer: "Lenovo",
                model: "T14",
                assignedStoreId: Guid.NewGuid(),
                rfidTagId: null,
                assignedUserId: null,
                macAddress: null,
                wifiMacAddress: null,
                imei: null,
                operatingSystem: null,
                operatingSystemVersion: null);

            Assert.IsNull(asset.RfidTagId);
            Assert.IsNull(asset.AssignedUserId);
            Assert.IsNull(asset.AssignedUser);
            Assert.IsNull(asset.MacAddress);
            Assert.IsNull(asset.WiFiMacAddress);
            Assert.IsNull(asset.Imei);
            Assert.IsNull(asset.OperatingSystem);
            Assert.IsNull(asset.OperatingSystemVersion);
        }

        [TestMethod]
        public void AssignVendor_Should_Set_AssignedVendorId()
        {
            var asset = new AssetBuilder()
                .WithoutVendor()
                .Build();

            var vendorId = Guid.NewGuid();

            asset.AssignVendor(vendorId);

            Assert.AreEqual(vendorId, asset.AssignedVendorId);
        }

        [TestMethod]
        public void AssignVendor_Should_Throw_When_VendorId_Is_Empty()
        {
            var asset = new AssetBuilder().Build();

            var exception =
                Assert.ThrowsExactly<ArgumentException>(
                    () => asset.AssignVendor(Guid.Empty));

            Assert.AreEqual("vendorId", exception.ParamName);
        }

        [TestMethod]
        public void RemoveVendor_Should_Clear_AssignedVendorId()
        {
            var asset = new AssetBuilder()
                .WithAssignedVendorId(Guid.NewGuid())
                .Build();

            asset.RemoveVendor();

            Assert.IsNull(asset.AssignedVendorId);
            Assert.IsNull(asset.AssignedVendor);
        }

        [TestMethod]
        public void RemoveVendor_Should_Remain_Null_When_No_Vendor_Is_Assigned()
        {
            var asset = new AssetBuilder()
                .WithoutVendor()
                .Build();

            asset.RemoveVendor();

            Assert.IsNull(asset.AssignedVendorId);
            Assert.IsNull(asset.AssignedVendor);
        }
    }
}