using System;
using AssetManagement.Domain.Entities.AssetHistories;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace AssetManagement.Tests.Domain.Entities.AssetHistories;

[TestClass]
public sealed class AssetHistoryTests
{
    private static readonly Guid ValidAssetId = Guid.Parse("11111111-1111-1111-1111-111111111111");
    private static readonly Guid ValidUserId = Guid.Parse("22222222-2222-2222-2222-222222222222");
    private static readonly DateTime ValidChangedAtUtc = new(2026, 11, 17, 8, 30, 0, DateTimeKind.Utc);

    [TestMethod]
    public void Constructor_WithValidArguments_SetsAllProperties()
    {
        // Act
        var history = CreateHistory();

        // Assert
        Assert.AreNotEqual(Guid.Empty, history.Id);
        Assert.AreEqual(ValidAssetId, history.AssetId);
        Assert.AreEqual("Updated", history.Action);
        Assert.AreEqual("SerialNumber", history.PropertyName);
        Assert.AreEqual("OLD-001", history.OldValue);
        Assert.AreEqual("NEW-001", history.NewValue);
        Assert.AreEqual(ValidUserId, history.ChangedByUserId);
        Assert.AreEqual("Test User", history.ChangedByUserName);
        Assert.AreEqual(ValidChangedAtUtc, history.ChangedAtUtc);
        Assert.AreEqual(DateTimeKind.Utc, history.ChangedAtUtc.Kind);
    }

    [TestMethod]
    public void Constructor_CalledTwice_GeneratesDifferentIds()
    {
        // Act
        var first = CreateHistory();
        var second = CreateHistory();

        // Assert
        Assert.AreNotEqual(Guid.Empty, first.Id);
        Assert.AreNotEqual(Guid.Empty, second.Id);
        Assert.AreNotEqual(first.Id, second.Id);
    }

    [TestMethod]
    public void Constructor_WithEmptyAssetId_ThrowsArgumentException()
    {
        AssertArgumentException(() => CreateHistory(assetId: Guid.Empty), "assetId", "Asset ID cannot be empty.");
    }

    [TestMethod]
    [DataRow(null)]
    [DataRow("")]
    [DataRow(" ")]
    [DataRow("\t\r\n")]
    public void Constructor_WithInvalidAction_ThrowsArgumentException(string? action)
    {
        AssertArgumentException(() => CreateHistory(action: action!), "action", "Action cannot be empty.");
    }

    [TestMethod]
    [DataRow(null)]
    [DataRow("")]
    [DataRow(" ")]
    [DataRow("\t\r\n")]
    public void Constructor_WithInvalidPropertyName_ThrowsArgumentException(string? propertyName)
    {
        AssertArgumentException(() => CreateHistory(propertyName: propertyName!), "propertyName", "Property name cannot be empty.");
    }

    [TestMethod]
    [DataRow(null, null)]
    [DataRow("", "")]
    [DataRow("same", "same")]
    [DataRow(" ", " ")]
    [DataRow("Törés", "Törés")]
    public void Constructor_WithEqualValues_ThrowsArgumentException(string? oldValue, string? newValue)
    {
        AssertArgumentException(() => CreateHistory(
            oldValue: oldValue,
            newValue: newValue),
            "newValue", "Old value and new value cannot be the same.");
    }

    [TestMethod]
    public void Constructor_WithEmptyChangedByUserId_ThrowsArgumentException()
    {
        AssertArgumentException(() => CreateHistory(changedByUserId: Guid.Empty),
            "changedByUserId", "Changed by user ID cannot be empty.");
    }

    [TestMethod]
    [DataRow(null)]
    [DataRow("")]
    [DataRow(" ")]
    [DataRow("\t\r\n")]
    public void Constructor_WithInvalidChangedByUserName_ThrowsArgumentException(string? changedByUserName)
    {
        AssertArgumentException(() => CreateHistory(changedByUserName: changedByUserName!),
            "changedByUserName", "Changed by user name cannot be empty.");
    }

    [TestMethod]
    public void Constructor_WithDefaultChangedAtUtc_ThrowsArgumentException()
    {
        AssertArgumentException(() => CreateHistory(changedAtUtc: default(DateTime)),
            "changedAtUtc", "Changed date cannot be empty.");
    }

    [TestMethod]
    [DataRow(DateTimeKind.Local)]
    [DataRow(DateTimeKind.Unspecified)]
    public void Constructor_WithNonUtcChangedAtUtc_ThrowsArgumentException(DateTimeKind kind)
    {
        // Arrange
        var changedAt = DateTime.SpecifyKind(ValidChangedAtUtc, kind);

        // Act and assert
        AssertArgumentException(() => CreateHistory(changedAtUtc: changedAt),
            "changedAtUtc", "Changed date must be in UTC.");
    }

    [TestMethod]
    public void Constructor_WithUtcMinimumDate_ThrowsArgumentException()
    {
        // Arrange
        var changedAt = DateTime.SpecifyKind(DateTime.MinValue, DateTimeKind.Utc);

        // Act and assert
        // DateTime equality ignores Kind, so this also matches default(DateTime).
        AssertArgumentException(() => CreateHistory(changedAtUtc: changedAt),
            "changedAtUtc", "Changed date cannot be empty.");
    }

    [TestMethod]
    public void Constructor_WithSurroundingWhitespace_TrimsMetadata()
    {
        // Act
        var history = CreateHistory(
            action: " \tUpdated\r\n ",
            propertyName: " \tSerialNumber\r\n ",
            changedByUserName: " \tTest User\r\n ");

        // Assert
        Assert.AreEqual("Updated", history.Action);
        Assert.AreEqual("SerialNumber", history.PropertyName);
        Assert.AreEqual("Test User", history.ChangedByUserName);
    }

    [TestMethod]
    public void Constructor_WithInternalWhitespace_PreservesInternalWhitespace()
    {
        // Act
        var history = CreateHistory(
            action: "  Asset  Updated  ",
            propertyName: "  Serial  Number  ",
            changedByUserName: "  Test  User  ");

        // Assert
        Assert.AreEqual("Asset  Updated", history.Action);
        Assert.AreEqual("Serial  Number", history.PropertyName);
        Assert.AreEqual("Test  User", history.ChangedByUserName);
    }

    [TestMethod]
    [DataRow(null, "new")]
    [DataRow("old", null)]
    [DataRow(null, "")]
    [DataRow("", null)]
    [DataRow("", "new")]
    [DataRow("old", "")]
    [DataRow("value", "VALUE")]
    [DataRow("value", " value ")]
    [DataRow(" ", "\t")]
    [DataRow("\u00E9", "e\u0301")]
    public void Constructor_WithDifferentValues_PreservesValues(string? oldValue, string? newValue)
    {
        // Act
        var history = CreateHistory(oldValue: oldValue, newValue: newValue);

        // Assert
        Assert.AreEqual(oldValue, history.OldValue);
        Assert.AreEqual(newValue, history.NewValue);
    }

    [TestMethod]
    public void Constructor_WithWhitespaceInValues_DoesNotTrimValues()
    {
        // Arrange
        const string oldValue = " \tOLD-001\r\n ";
        const string newValue = " \tNEW-001\r\n ";

        // Act
        var history = CreateHistory(oldValue: oldValue, newValue: newValue);

        // Assert
        Assert.AreEqual(oldValue, history.OldValue);
        Assert.AreEqual(newValue, history.NewValue);
    }

    [TestMethod]
    public void Constructor_WithPreciseUtcTimestamp_PreservesTicks()
    {
        // Arrange
        var changedAt = ValidChangedAtUtc.AddTicks(1234567);

        // Act
        var history = CreateHistory(changedAtUtc: changedAt);

        // Assert
        Assert.AreEqual(changedAt.Ticks, history.ChangedAtUtc.Ticks);
        Assert.AreEqual(DateTimeKind.Utc, history.ChangedAtUtc.Kind);
    }

    private static AssetHistory CreateHistory(
        Guid? assetId = null,
        string action = "Updated",
        string propertyName = "SerialNumber",
        string? oldValue = "OLD-001",
        string? newValue = "NEW-001",
        Guid? changedByUserId = null,
        string changedByUserName = "Test User",
        DateTime? changedAtUtc = null)
    {
        return new AssetHistory(
            assetId ?? ValidAssetId,
            action,
            propertyName,
            oldValue,
            newValue,
            changedByUserId ?? ValidUserId,
            changedByUserName,
            changedAtUtc ?? ValidChangedAtUtc);
    }

    private static void AssertArgumentException(Action action, string expectedParamName, string expectedMessage)
    {
        ArgumentException? exception = null;

        try
        {
            action();
        }
        catch (ArgumentException caught)
        {
            exception = caught;
        }

        Assert.IsNotNull(exception);
        Assert.AreEqual(typeof(ArgumentException), exception.GetType());
        Assert.AreEqual(expectedParamName, exception.ParamName);
        Assert.StartsWith(expectedMessage, exception.Message);
    }
}