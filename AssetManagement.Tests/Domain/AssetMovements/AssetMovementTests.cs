using AssetManagement.Domain.Entities.AssetMovements;

namespace AssetManagement.Tests.Domain.AssetMovements
{
    [TestClass]
    public sealed class AssetMovementTests
    {
        [TestMethod]
        public void Create_Should_Create_AssetMovement_With_Valid_Data()
        {
            var assetId = Guid.NewGuid();
            var storeId = Guid.NewGuid();
            var movementType = GetValidMovementType();

            const string employeeNumber = "103549";

            var beforeCreation = DateTime.UtcNow;

            var movement = AssetMovement.Create(
                assetId: assetId,
                storeId: storeId,
                employeeNumber: employeeNumber,
                type: movementType);

            var afterCreation = DateTime.UtcNow;

            Assert.AreNotEqual(Guid.Empty, movement.Id);
            Assert.AreEqual(assetId, movement.AssetId);
            Assert.AreEqual(storeId, movement.StoreId);
            Assert.AreEqual(employeeNumber, movement.EmployeeNumber);
            Assert.AreEqual(movementType, movement.Type);

            Assert.IsTrue(movement.CreatedAtUtc >= beforeCreation);
            Assert.IsTrue(movement.CreatedAtUtc <= afterCreation);
        }

        [TestMethod]
        public void Create_Should_Generate_Unique_Id_For_Each_Movement()
        {
            var assetId = Guid.NewGuid();
            var storeId = Guid.NewGuid();
            var movementType = GetValidMovementType();

            var firstMovement = AssetMovement.Create(
                assetId: assetId,
                storeId: storeId,
                employeeNumber: "103549",
                type: movementType);

            var secondMovement = AssetMovement.Create(
                assetId: assetId,
                storeId: storeId,
                employeeNumber: "103549",
                type: movementType);

            Assert.AreNotEqual(Guid.Empty, firstMovement.Id);
            Assert.AreNotEqual(Guid.Empty, secondMovement.Id);
            Assert.AreNotEqual(firstMovement.Id, secondMovement.Id);
        }

        [TestMethod]
        public void Create_Should_Trim_EmployeeNumber()
        {
            var movement = AssetMovement.Create(
                assetId: Guid.NewGuid(),
                storeId: Guid.NewGuid(),
                employeeNumber: "  103549  ",
                type: GetValidMovementType());

            Assert.AreEqual("103549", movement.EmployeeNumber);
        }

        [TestMethod]
        public void Create_Should_Throw_When_AssetId_Is_Empty()
        {
            var exception = Assert.ThrowsExactly<ArgumentException>(
                () => AssetMovement.Create(
                    assetId: Guid.Empty,
                    storeId: Guid.NewGuid(),
                    employeeNumber: "103549",
                    type: GetValidMovementType()));

            Assert.AreEqual("assetId", exception.ParamName);
        }

        [TestMethod]
        public void Create_Should_Throw_When_StoreId_Is_Empty()
        {
            var exception = Assert.ThrowsExactly<ArgumentException>(
                () => AssetMovement.Create(
                    assetId: Guid.NewGuid(),
                    storeId: Guid.Empty,
                    employeeNumber: "103549",
                    type: GetValidMovementType()));

            Assert.AreEqual("storeId", exception.ParamName);
        }

        [TestMethod]
        public void Create_Should_Throw_When_EmployeeNumber_Is_Null()
        {
            var exception = Assert.ThrowsExactly<ArgumentException>(
                () => AssetMovement.Create(
                    assetId: Guid.NewGuid(),
                    storeId: Guid.NewGuid(),
                    employeeNumber: null!,
                    type: GetValidMovementType()));

            Assert.AreEqual("employeeNumber", exception.ParamName);
        }

        [TestMethod]
        [DataRow("")]
        [DataRow(" ")]
        [DataRow("  ")]
        [DataRow("   ")]
        [DataRow("\t")]
        [DataRow("\r\n")]
        public void Create_Should_Throw_When_EmployeeNumber_Is_Empty_Or_Whitespace(
            string invalidEmployeeNumber)
        {
            var exception = Assert.ThrowsExactly<ArgumentException>(
                () => AssetMovement.Create(
                    assetId: Guid.NewGuid(),
                    storeId: Guid.NewGuid(),
                    employeeNumber: invalidEmployeeNumber,
                    type: GetValidMovementType()));

            Assert.AreEqual("employeeNumber", exception.ParamName);
        }

        [TestMethod]
        public void Create_Should_Throw_When_MovementType_Is_Not_Defined()
        {
            var invalidMovementType =(AssetMovementType)int.MaxValue;

            var exception =
                Assert.ThrowsExactly<ArgumentOutOfRangeException>(
                    () => AssetMovement.Create(
                        assetId: Guid.NewGuid(),
                        storeId: Guid.NewGuid(),
                        employeeNumber: "103549",
                        type: invalidMovementType));

            Assert.AreEqual("type", exception.ParamName);
            Assert.AreEqual(invalidMovementType, exception.ActualValue);
        }

        [TestMethod]
        public void Create_Should_Preserve_Provided_Identifiers()
        {
            var assetId = Guid.NewGuid();
            var storeId = Guid.NewGuid();

            var movement = AssetMovement.Create(
                assetId: assetId,
                storeId: storeId,
                employeeNumber: "103549",
                type: GetValidMovementType());

            Assert.AreEqual(assetId, movement.AssetId);
            Assert.AreEqual(storeId, movement.StoreId);
        }

        private static AssetMovementType GetValidMovementType()
        {
            return Enum.GetValues<AssetMovementType>()[0];
        }
    }
}