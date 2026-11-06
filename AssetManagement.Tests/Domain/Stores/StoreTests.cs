using AssetManagement.Domain.Entities.ValueObjects.Address;
using AssetManagement.Tests.Builders;

namespace AssetManagement.Tests.Domain.Stores
{
    [TestClass]
    public sealed class StoreTests
    {
        [TestMethod]
        public void Create_Should_Create_Store_With_Valid_Data()
        {
            const string storeNumber = "321";
            const string name = "Budapest M3";
            const CountryCodes countryCode = CountryCodes.HUN;
            const string postalCode = "1152";
            const string city = "Budapest";
            const string street = "Városkapu";
            const PublicSpaces publicSpace = PublicSpaces.Street;
            const string houseNumber = "5";

            var store = new StoreBuilder()
                .WithStoreNumber(storeNumber)
                .WithName(name)
                .WithCountryCode(countryCode)
                .WithPostalCode(postalCode)
                .WithCity(city)
                .WithStreet(street)
                .WithPublicSpace(publicSpace)
                .WithHouseNumber(houseNumber)
                .Build();

            Assert.AreNotEqual(Guid.Empty, store.Id);
            Assert.AreEqual(storeNumber, store.StoreNumber);
            Assert.AreEqual(name, store.Name);
            Assert.IsNotNull(store.Address);
            Assert.AreEqual(countryCode, store.Address.CountryCode);
            Assert.AreEqual(postalCode, store.Address.PostalCode);
            Assert.AreEqual(city, store.Address.City);
            Assert.AreEqual(street, store.Address.Street);
            Assert.AreEqual(publicSpace, store.Address.PublicSpace);
            Assert.AreEqual(houseNumber, store.Address.HouseNumber);
        }

        [TestMethod]
        public void Create_Should_Generate_Unique_Id()
        {
            var firstStore = new StoreBuilder().Build();
            var secondStore = new StoreBuilder().Build();

            Assert.AreNotEqual(Guid.Empty, firstStore.Id);
            Assert.AreNotEqual(Guid.Empty, secondStore.Id);
            Assert.AreNotEqual(firstStore.Id, secondStore.Id);
        }

        [TestMethod]
        public void Create_Should_Trim_StoreNumber_And_Name()
        {
            var store = new StoreBuilder()
                .WithStoreNumber("  321  ")
                .WithName("  Budapest M3  ")
                .Build();

            Assert.AreEqual("321", store.StoreNumber);
            Assert.AreEqual("Budapest M3", store.Name);
        }

        [TestMethod]
        public void Create_Should_Trim_Address_Values()
        {
            var store = new StoreBuilder()
                .WithPostalCode("  1152  ")
                .WithCity("  Budapest  ")
                .WithStreet("  Városkapu  ")
                .WithHouseNumber("  5  ")
                .Build();

            Assert.AreEqual("1152", store.Address.PostalCode);
            Assert.AreEqual("Budapest", store.Address.City);
            Assert.AreEqual("Városkapu", store.Address.Street);
            Assert.AreEqual("5", store.Address.HouseNumber);
        }

        [TestMethod]
        public void Create_Should_Throw_When_StoreNumber_Is_Null()
        {
            var builder = new StoreBuilder()
                .WithStoreNumber(null);

            var exception = Assert.ThrowsExactly<ArgumentNullException>(() => builder.Build());

            Assert.AreEqual("storeNumber", exception.ParamName);
        }

        [TestMethod]
        [DataRow("")]
        [DataRow(" ")]
        [DataRow("  ")]
        [DataRow("\t")]
        [DataRow("\r\n")]
        public void Create_Should_Throw_When_StoreNumber_Is_Empty_Or_Whitespace(string invalidStoreNumber)
        {
            var builder = new StoreBuilder()
                .WithStoreNumber(invalidStoreNumber);

            var exception = Assert.ThrowsExactly<ArgumentException>(() => builder.Build());

            Assert.AreEqual("storeNumber", exception.ParamName);
        }

        [TestMethod]
        public void Create_Should_Throw_When_Name_Is_Null()
        {
            var builder = new StoreBuilder()
                .WithName(null);

            var exception = Assert.ThrowsExactly<ArgumentNullException>(() => builder.Build());

            Assert.AreEqual("name", exception.ParamName);
        }

        [TestMethod]
        [DataRow("")]
        [DataRow(" ")]
        [DataRow("  ")]
        [DataRow("\t")]
        [DataRow("\r\n")]
        public void Create_Should_Throw_When_Name_Is_Empty_Or_Whitespace(string invalidName)
        {
            var builder = new StoreBuilder()
                .WithName(invalidName);

            var exception = Assert.ThrowsExactly<ArgumentException>(() => builder.Build());

            Assert.AreEqual("name", exception.ParamName);
        }

        [TestMethod]
        public void Create_Should_Throw_When_CountryCode_Is_Unknown()
        {
            var builder = new StoreBuilder()
                .WithCountryCode(CountryCodes.Unknown);

            var exception = Assert.ThrowsExactly<ArgumentOutOfRangeException>(() => builder.Build());

            Assert.AreEqual("countryCode", exception.ParamName);
        }

        [TestMethod]
        public void Create_Should_Throw_When_CountryCode_Is_Not_Defined()
        {
            var builder = new StoreBuilder()
                .WithCountryCode((CountryCodes)999);

            var exception = Assert.ThrowsExactly<ArgumentOutOfRangeException>(() => builder.Build());

            Assert.AreEqual("countryCode", exception.ParamName);
        }

        [TestMethod]
        public void Create_Should_Throw_When_PostalCode_Is_Null()
        {
            var builder = new StoreBuilder()
                .WithPostalCode(null);

            var exception = Assert.ThrowsExactly<ArgumentNullException>(() => builder.Build());

            Assert.AreEqual("postalCode", exception.ParamName);
        }

        [TestMethod]
        [DataRow("")]
        [DataRow(" ")]
        [DataRow("  ")]
        [DataRow("\t")]
        [DataRow("\r\n")]
        public void Create_Should_Throw_When_PostalCode_Is_Empty_Or_Whitespace(string invalidPostalCode)
        {
            var builder = new StoreBuilder()
                .WithPostalCode(invalidPostalCode);

            var exception = Assert.ThrowsExactly<ArgumentException>(() => builder.Build());

            Assert.AreEqual("postalCode", exception.ParamName);
        }

        [TestMethod]
        public void Create_Should_Throw_When_City_Is_Null()
        {
            var builder = new StoreBuilder()
                .WithCity(null);

            var exception = Assert.ThrowsExactly<ArgumentNullException>(() => builder.Build());

            Assert.AreEqual("city", exception.ParamName);
        }

        [TestMethod]
        [DataRow("")]
        [DataRow(" ")]
        [DataRow("  ")]
        [DataRow("\t")]
        [DataRow("\r\n")]
        public void Create_Should_Throw_When_City_Is_Empty_Or_Whitespace(string invalidCity)
        {
            var builder = new StoreBuilder()
                .WithCity(invalidCity);

            var exception = Assert.ThrowsExactly<ArgumentException>(() => builder.Build());

            Assert.AreEqual("city", exception.ParamName);
        }

        [TestMethod]
        public void Create_Should_Throw_When_Street_Is_Null()
        {
            var builder = new StoreBuilder()
                .WithStreet(null);

            var exception = Assert.ThrowsExactly<ArgumentNullException>(() => builder.Build());

            Assert.AreEqual("street", exception.ParamName);
        }

        [TestMethod]
        [DataRow("")]
        [DataRow(" ")]
        [DataRow("  ")]
        [DataRow("\t")]
        [DataRow("\r\n")]
        public void Create_Should_Throw_When_Street_Is_Empty_Or_Whitespace(string invalidStreet)
        {
            var builder = new StoreBuilder()
                .WithStreet(invalidStreet);

            var exception = Assert.ThrowsExactly<ArgumentException>(() => builder.Build());

            Assert.AreEqual("street", exception.ParamName);
        }

        [TestMethod]
        public void Create_Should_Throw_When_PublicSpace_Is_Unknown()
        {
            var builder = new StoreBuilder()
                .WithPublicSpace(PublicSpaces.Unknown);

            var exception =Assert.ThrowsExactly<ArgumentOutOfRangeException>(() => builder.Build());

            Assert.AreEqual("publicSpace", exception.ParamName);
        }

        [TestMethod]
        public void Create_Should_Throw_When_PublicSpace_Is_Not_Defined()
        {
            var builder = new StoreBuilder()
                .WithPublicSpace((PublicSpaces)999);

            var exception =Assert.ThrowsExactly<ArgumentOutOfRangeException>(() => builder.Build());

            Assert.AreEqual("publicSpace", exception.ParamName);
        }

        [TestMethod]
        public void Create_Should_Throw_When_HouseNumber_Is_Null()
        {
            var builder = new StoreBuilder()
                .WithHouseNumber(null);

            var exception = Assert.ThrowsExactly<ArgumentNullException>(() => builder.Build());

            Assert.AreEqual("houseNumber", exception.ParamName);
        }

        [TestMethod]
        [DataRow("")]
        [DataRow(" ")]
        [DataRow("  ")]
        [DataRow("\t")]
        [DataRow("\r\n")]
        public void Create_Should_Throw_When_HouseNumber_Is_Empty_Or_Whitespace(string invalidHouseNumber)
        {
            var builder = new StoreBuilder()
                .WithHouseNumber(invalidHouseNumber);

            var exception = Assert.ThrowsExactly<ArgumentException>(() => builder.Build());

            Assert.AreEqual("houseNumber", exception.ParamName);
        }

        [TestMethod]
        public void UpdateDetails_Should_Update_Name_And_Address()
        {
            var store = new StoreBuilder().Build();

            store.UpdateDetails(
                name: "Pécs",
                countryCode: CountryCodes.HUN,
                postalCode: "7634",
                city: "Pécs",
                street: "Makay István",
                publicSpace: PublicSpaces.Street,
                houseNumber: "11");

            Assert.AreEqual("Pécs", store.Name);
            Assert.AreEqual(CountryCodes.HUN, store.Address.CountryCode);
            Assert.AreEqual("7634", store.Address.PostalCode);
            Assert.AreEqual("Pécs", store.Address.City);
            Assert.AreEqual("Makay István", store.Address.Street);
            Assert.AreEqual(PublicSpaces.Street, store.Address.PublicSpace);
            Assert.AreEqual("11", store.Address.HouseNumber);
        }

        [TestMethod]
        public void UpdateDetails_Should_Preserve_Id_And_StoreNumber()
        {
            var store = new StoreBuilder().Build();

            var originalId = store.Id;
            var originalStoreNumber = store.StoreNumber;

            store.UpdateDetails(
                name: "Pécs",
                countryCode: CountryCodes.HUN,
                postalCode: "7634",
                city: "Pécs",
                street: "Makay István",
                publicSpace: PublicSpaces.Street,
                houseNumber: "11");

            Assert.AreEqual(originalId, store.Id);
            Assert.AreEqual(originalStoreNumber, store.StoreNumber);
        }

        [TestMethod]
        public void UpdateDetails_Should_Trim_Name_And_Address_Values()
        {
            var store = new StoreBuilder().Build();

            store.UpdateDetails(
                name: "  Pécs  ",
                countryCode: CountryCodes.HUN,
                postalCode: "  7634  ",
                city: "  Pécs  ",
                street: "  Makay István  ",
                publicSpace: PublicSpaces.Street,
                houseNumber: "  11  ");

            Assert.AreEqual("Pécs", store.Name);
            Assert.AreEqual("7634", store.Address.PostalCode);
            Assert.AreEqual("Pécs", store.Address.City);
            Assert.AreEqual("Makay István", store.Address.Street);
            Assert.AreEqual("11", store.Address.HouseNumber);
        }

        [TestMethod]
        public void UpdateDetails_Should_Replace_Address_Instance()
        {
            var store = new StoreBuilder().Build();
            var originalAddress = store.Address;

            store.UpdateDetails(
                name: "Pécs",
                countryCode: CountryCodes.HUN,
                postalCode: "7634",
                city: "Pécs",
                street: "Makay István",
                publicSpace: PublicSpaces.Street,
                houseNumber: "11");

            Assert.AreNotSame(originalAddress, store.Address);
        }

        [TestMethod]
        public void UpdateDetails_Should_Throw_When_Name_Is_Null()
        {
            var store = new StoreBuilder().Build();

            var exception = Assert.ThrowsExactly<ArgumentNullException>(
                () => store.UpdateDetails(
                    name: null!,
                    countryCode: CountryCodes.HUN,
                    postalCode: "7634",
                    city: "Pécs",
                    street: "Makay István",
                    publicSpace: PublicSpaces.Street,
                    houseNumber: "11"));

            Assert.AreEqual("name", exception.ParamName);
        }

        [TestMethod]
        [DataRow("")]
        [DataRow(" ")]
        [DataRow("  ")]
        [DataRow("\t")]
        [DataRow("\r\n")]
        public void UpdateDetails_Should_Throw_When_Name_Is_Empty_Or_Whitespace(string invalidName)
        {
            var store = new StoreBuilder().Build();

            var exception = Assert.ThrowsExactly<ArgumentException>(
                () => store.UpdateDetails(
                    name: invalidName,
                    countryCode: CountryCodes.HUN,
                    postalCode: "7634",
                    city: "Pécs",
                    street: "Makay István",
                    publicSpace: PublicSpaces.Street,
                    houseNumber: "11"));

            Assert.AreEqual("name", exception.ParamName);
        }

        [TestMethod]
        public void UpdateDetails_Should_Throw_When_CountryCode_Is_Unknown()
        {
            var store = new StoreBuilder().Build();

            var exception =
                Assert.ThrowsExactly<ArgumentOutOfRangeException>(
                    () => store.UpdateDetails(
                        name: "Pécs",
                        countryCode: CountryCodes.Unknown,
                        postalCode: "7634",
                        city: "Pécs",
                        street: "Makay István",
                        publicSpace: PublicSpaces.Street,
                        houseNumber: "11"));

            Assert.AreEqual("countryCode", exception.ParamName);
        }

        [TestMethod]
        public void UpdateDetails_Should_Throw_When_CountryCode_Is_Not_Defined()
        {
            var store = new StoreBuilder().Build();

            var exception =
                Assert.ThrowsExactly<ArgumentOutOfRangeException>(
                    () => store.UpdateDetails(
                        name: "Pécs",
                        countryCode: (CountryCodes)999,
                        postalCode: "7634",
                        city: "Pécs",
                        street: "Makay István",
                        publicSpace: PublicSpaces.Street,
                        houseNumber: "11"));

            Assert.AreEqual("countryCode", exception.ParamName);
        }

        [TestMethod]
        public void UpdateDetails_Should_Throw_When_PostalCode_Is_Null()
        {
            var store = new StoreBuilder().Build();

            var exception = Assert.ThrowsExactly<ArgumentNullException>(
                () => store.UpdateDetails(
                    name: "Pécs",
                    countryCode: CountryCodes.HUN,
                    postalCode: null!,
                    city: "Pécs",
                    street: "Makay István",
                    publicSpace: PublicSpaces.Street,
                    houseNumber: "11"));

            Assert.AreEqual("postalCode", exception.ParamName);
        }

        [TestMethod]
        [DataRow("")]
        [DataRow(" ")]
        [DataRow("  ")]
        [DataRow("\t")]
        [DataRow("\r\n")]
        public void UpdateDetails_Should_Throw_When_PostalCode_Is_Empty_Or_Whitespace(string invalidPostalCode)
        {
            var store = new StoreBuilder().Build();

            var exception = Assert.ThrowsExactly<ArgumentException>(
                () => store.UpdateDetails(
                    name: "Pécs",
                    countryCode: CountryCodes.HUN,
                    postalCode: invalidPostalCode,
                    city: "Pécs",
                    street: "Makay István",
                    publicSpace: PublicSpaces.Street,
                    houseNumber: "11"));

            Assert.AreEqual("postalCode", exception.ParamName);
        }

        [TestMethod]
        public void UpdateDetails_Should_Throw_When_City_Is_Null()
        {
            var store = new StoreBuilder().Build();

            var exception = Assert.ThrowsExactly<ArgumentNullException>(
                () => store.UpdateDetails(
                    name: "Pécs",
                    countryCode: CountryCodes.HUN,
                    postalCode: "7634",
                    city: null!,
                    street: "Makay István",
                    publicSpace: PublicSpaces.Street,
                    houseNumber: "11"));

            Assert.AreEqual("city", exception.ParamName);
        }

        [TestMethod]
        [DataRow("")]
        [DataRow(" ")]
        [DataRow("  ")]
        [DataRow("\t")]
        [DataRow("\r\n")]
        public void UpdateDetails_Should_Throw_When_City_Is_Empty_Or_Whitespace(
            string invalidCity)
        {
            var store = new StoreBuilder().Build();

            var exception = Assert.ThrowsExactly<ArgumentException>(
                () => store.UpdateDetails(
                    name: "Pécs",
                    countryCode: CountryCodes.HUN,
                    postalCode: "7634",
                    city: invalidCity,
                    street: "Makay István",
                    publicSpace: PublicSpaces.Street,
                    houseNumber: "11"));

            Assert.AreEqual("city", exception.ParamName);
        }

        [TestMethod]
        public void UpdateDetails_Should_Throw_When_Street_Is_Null()
        {
            var store = new StoreBuilder().Build();

            var exception = Assert.ThrowsExactly<ArgumentNullException>(
                () => store.UpdateDetails(
                    name: "Pécs",
                    countryCode: CountryCodes.HUN,
                    postalCode: "7634",
                    city: "Pécs",
                    street: null!,
                    publicSpace: PublicSpaces.Street,
                    houseNumber: "11"));

            Assert.AreEqual("street", exception.ParamName);
        }

        [TestMethod]
        [DataRow("")]
        [DataRow(" ")]
        [DataRow("  ")]
        [DataRow("\t")]
        [DataRow("\r\n")]
        public void UpdateDetails_Should_Throw_When_Street_Is_Empty_Or_Whitespace(string invalidStreet)
        {
            var store = new StoreBuilder().Build();

            var exception = Assert.ThrowsExactly<ArgumentException>(
                () => store.UpdateDetails(
                    name: "Pécs",
                    countryCode: CountryCodes.HUN,
                    postalCode: "7634",
                    city: "Pécs",
                    street: invalidStreet,
                    publicSpace: PublicSpaces.Street,
                    houseNumber: "11"));

            Assert.AreEqual("street", exception.ParamName);
        }

        [TestMethod]
        public void UpdateDetails_Should_Throw_When_PublicSpace_Is_Unknown()
        {
            var store = new StoreBuilder().Build();

            var exception =
                Assert.ThrowsExactly<ArgumentOutOfRangeException>(
                    () => store.UpdateDetails(
                        name: "Pécs",
                        countryCode: CountryCodes.HUN,
                        postalCode: "7634",
                        city: "Pécs",
                        street: "Makay István",
                        publicSpace: PublicSpaces.Unknown,
                        houseNumber: "11"));

            Assert.AreEqual("publicSpace", exception.ParamName);
        }

        [TestMethod]
        public void UpdateDetails_Should_Throw_When_PublicSpace_Is_Not_Defined()
        {
            var store = new StoreBuilder().Build();

            var exception =
                Assert.ThrowsExactly<ArgumentOutOfRangeException>(
                    () => store.UpdateDetails(
                        name: "Pécs",
                        countryCode: CountryCodes.HUN,
                        postalCode: "7634",
                        city: "Pécs",
                        street: "Makay István",
                        publicSpace: (PublicSpaces)999,
                        houseNumber: "11"));

            Assert.AreEqual("publicSpace", exception.ParamName);
        }

        [TestMethod]
        public void UpdateDetails_Should_Throw_When_HouseNumber_Is_Null()
        {
            var store = new StoreBuilder().Build();

            var exception = Assert.ThrowsExactly<ArgumentNullException>(
                () => store.UpdateDetails(
                    name: "Pécs",
                    countryCode: CountryCodes.HUN,
                    postalCode: "7634",
                    city: "Pécs",
                    street: "Makay István",
                    publicSpace: PublicSpaces.Street,
                    houseNumber: null!));

            Assert.AreEqual("houseNumber", exception.ParamName);
        }

        [TestMethod]
        [DataRow("")]
        [DataRow(" ")]
        [DataRow("  ")]
        [DataRow("\t")]
        [DataRow("\r\n")]
        public void UpdateDetails_Should_Throw_When_HouseNumber_Is_Empty_Or_Whitespace(string invalidHouseNumber)
        {
            var store = new StoreBuilder().Build();

            var exception = Assert.ThrowsExactly<ArgumentException>(
                () => store.UpdateDetails(
                    name: "Pécs",
                    countryCode: CountryCodes.HUN,
                    postalCode: "7634",
                    city: "Pécs",
                    street: "Makay István",
                    publicSpace: PublicSpaces.Street,
                    houseNumber: invalidHouseNumber));

            Assert.AreEqual("houseNumber", exception.ParamName);
        }

        [TestMethod]
        public void UpdateDetails_Should_Not_Modify_Store_When_Address_Is_Invalid()
        {
            var store = new StoreBuilder().Build();

            var originalName = store.Name;
            var originalAddress = store.Address;

            Assert.ThrowsExactly<ArgumentNullException>(
                () => store.UpdateDetails(
                    name: "Updated store",
                    countryCode: CountryCodes.HUN,
                    postalCode: "7634",
                    city: null!,
                    street: "Makay István",
                    publicSpace: PublicSpaces.Street,
                    houseNumber: "11"));

            Assert.AreEqual(originalName, store.Name);
            Assert.AreSame(originalAddress, store.Address);
        }
    }
}