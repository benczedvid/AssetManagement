using System;
using System.Linq;
using AssetManagement.Domain.Entities.Packages;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace AssetManagement.Tests.Domain.Entities.Packages;

[TestClass]
public sealed class PackageTests
{
    private static readonly Guid SourceStoreId = Guid.NewGuid();
    private static readonly Guid DestinationStoreId = Guid.NewGuid();
    private static readonly Guid CreatorId = Guid.NewGuid();
    private static readonly Guid OperatorId = Guid.NewGuid();
    private static readonly Guid CourierId = Guid.NewGuid();
    private static readonly Guid AssetId = Guid.NewGuid();
    private static readonly DateTime CreatedAt = new(2026, 11, 17, 8, 0, 0, DateTimeKind.Utc);
    private static readonly DateTime OperationAt = CreatedAt.AddMinutes(10);
    private static readonly DateTime ReceivedAt = CreatedAt.AddMinutes(20);

    [TestMethod]
    public void Create_WithValidArguments_SetsInitialState()
    {
        var package = CreatePackage();

        Assert.AreNotEqual(Guid.Empty, package.Id);
        Assert.AreEqual("PKG-2026-0001", package.PackageNumber);
        Assert.AreEqual(SourceStoreId, package.SourceStoreId);
        Assert.AreEqual(DestinationStoreId, package.DestinationStoreId);
        Assert.AreEqual(PackageStatus.Created, package.Status);
        Assert.AreEqual(CreatedAt, package.CreatedAtUtc);
        Assert.AreEqual(DateTimeKind.Utc, package.CreatedAtUtc.Kind);
        Assert.AreEqual(CreatorId, package.CreatedByUserId);
        Assert.IsNull(package.CourierUserId);
        Assert.IsNull(package.Comment);
        Assert.IsNull(package.DispatchedAtUtc);
        Assert.IsNull(package.DispatchedByUserId);
        Assert.IsNull(package.ReceivedAtUtc);
        Assert.IsNull(package.ReceivedByUserId);
        Assert.IsNull(package.CancelledAtUtc);
        Assert.IsNull(package.CancelledByUserId);
        Assert.IsEmpty(package.Items);
        Assert.HasCount(1, package.Events);
    }

    [TestMethod]
    public void Create_CalledTwice_GeneratesDifferentIds()
    {
        var first = CreatePackage();
        var second = CreatePackage();

        Assert.AreNotEqual(first.Id, second.Id);
    }

    [TestMethod]
    public void Create_WithCourier_AssignsCourierAndAddsSecondEvent()
    {
        var package = CreatePackage(courierUserId: CourierId);

        Assert.AreEqual(CourierId, package.CourierUserId);
        Assert.HasCount(2, package.Events);
    }

    [TestMethod]
    public void Create_WithPaddedText_TrimsPackageNumberAndComment()
    {
        var package = CreatePackage(
            packageNumber: " \tPKG-2026-0001\r\n ",
            comment: " \tFragile package\r\n ");

        Assert.AreEqual("PKG-2026-0001", package.PackageNumber);
        Assert.AreEqual("Fragile package", package.Comment);
    }

    [TestMethod]
    public void Create_WithMaximumTextLengths_AcceptsTrimmedLengths()
    {
        // Arrange
        var number = new string('P', 50);
        var comment = new string('C', 2000);
        var userName = new string('U', 250);

        // Act
        var package = CreatePackage(
            packageNumber: $" {number} ",
            comment: comment,
            createdByUserName: userName);

        // Assert
        Assert.AreEqual(number, package.PackageNumber);
        Assert.AreEqual(comment, package.Comment);
        Assert.HasCount(1, package.Events);
    }

    [TestMethod]
    [DataRow(null)]
    [DataRow("")]
    [DataRow(" ")]
    [DataRow("\t\r\n")]
    public void Create_WithInvalidPackageNumber_Throws(string? number)
    {
        AssertArgument(() => CreatePackage(packageNumber: number!), "packageNumber");
    }

    [TestMethod]
    public void Create_WithTooLongPackageNumber_Throws()
    {
        AssertArgument(() => CreatePackage(packageNumber: new string('P', 51)), "packageNumber");
    }

    [TestMethod]
    [DataRow("sourceStoreId")]
    [DataRow("destinationStoreId")]
    [DataRow("courierUserId")]
    [DataRow("createdByUserId")]
    public void Create_WithEmptyIdentifier_Throws(string parameter)
    {
        AssertArgument(
            () => CreatePackage(
                sourceStoreId: parameter == "sourceStoreId"
                    ? Guid.Empty : SourceStoreId,
                destinationStoreId: parameter == "destinationStoreId"
                    ? Guid.Empty : DestinationStoreId,
                courierUserId: parameter == "courierUserId"
                    ? Guid.Empty : null,
                createdByUserId: parameter == "createdByUserId"
                    ? Guid.Empty : CreatorId),
            parameter);
    }

    [TestMethod]
    public void Create_WithSameSourceAndDestination_Throws()
    {
        AssertArgument(() => CreatePackage(destinationStoreId: SourceStoreId), "destinationStoreId");
    }

    [TestMethod]
    [DataRow("")]
    [DataRow(" ")]
    [DataRow("\t\r\n")]
    public void Create_WithWhitespaceComment_Throws(string comment)
    {
        AssertArgument(() => CreatePackage(comment: comment), "comment");
    }

    [TestMethod]
    public void Create_WithTooLongComment_Throws()
    {
        AssertArgument(() => CreatePackage(comment: new string('C', 2001)), "comment");
    }

    [TestMethod]
    [DataRow(null)]
    [DataRow("")]
    [DataRow(" ")]
    [DataRow("\t\r\n")]
    public void Create_WithInvalidUserName_Throws(string? userName)
    {
        AssertArgument(() => CreatePackage(createdByUserName: userName!), "createdByUserName");
    }

    [TestMethod]
    public void Create_WithTooLongUserName_Throws()
    {
        AssertArgument(() => CreatePackage(createdByUserName: new string('U', 251)), "createdByUserName");
    }

    [TestMethod]
    [DataRow("Default")]
    [DataRow("Local")]
    [DataRow("Unspecified")]
    public void Create_WithInvalidDate_Throws(string dateKind)
    {
        AssertArgument(() => CreatePackage(createdAtUtc: InvalidDate(dateKind)), "createdAtUtc");
    }

    [TestMethod]
    public void AddAsset_WithValidArguments_AddsItemAndEvent()
    {
        var package = CreatePackage();

        package.AddAsset(AssetId, OperatorId, "Operator", OperationAt);

        Assert.HasCount(1, package.Items);
        Assert.AreEqual(AssetId, package.Items.Single().AssetId);
        Assert.HasCount(2, package.Events);
        Assert.AreEqual(PackageStatus.Created, package.Status);
    }

    [TestMethod]
    public void AddAsset_WithDifferentAssets_AddsBoth()
    {
        var package = CreatePackage();
        var secondAssetId = Guid.NewGuid();

        package.AddAsset(AssetId, OperatorId, "Operator", OperationAt);
        package.AddAsset(secondAssetId, OperatorId, "Operator", OperationAt);

        CollectionAssert.AreEquivalent(
            new[] { AssetId, secondAssetId },
            package.Items.Select(item => item.AssetId).ToArray());

        Assert.HasCount(3, package.Events);
    }

    [TestMethod]
    public void AddAsset_WithDuplicateAsset_ThrowsWithoutChangingCollections()
    {
        var package = CreatePackage();
        package.AddAsset(AssetId, OperatorId, "Operator", OperationAt);

        Assert.ThrowsExactly<InvalidOperationException>(() => package.AddAsset(AssetId, OperatorId, "Operator", OperationAt));

        Assert.AreEqual(1, package.Items.Count);
        Assert.AreEqual(2, package.Events.Count);
    }

    [TestMethod]
    [DataRow("AddAsset")]
    [DataRow("RemoveAsset")]
    public void AssetOperation_WithEmptyAssetId_Throws(string operation)
    {
        var package = CreatePackage();

        AssertArgument(
            () =>
            {
                if (operation == "AddAsset")
                {
                    package.AddAsset(Guid.Empty, OperatorId, "Operator", OperationAt);
                }
                else
                {
                    package.RemoveAsset(Guid.Empty, OperatorId, "Operator", OperationAt);
                }
            },
            "assetId");

        Assert.AreEqual(0, package.Items.Count);
        Assert.AreEqual(1, package.Events.Count);
    }

    [TestMethod]
    public void RemoveAsset_WithExistingAsset_RemovesOnlySelectedItem()
    {
        var package = CreatePackage();
        var secondAssetId = Guid.NewGuid();

        package.AddAsset(AssetId, OperatorId, "Operator", OperationAt);
        package.AddAsset(secondAssetId, OperatorId, "Operator", OperationAt);

        package.RemoveAsset(AssetId, OperatorId, "Operator", OperationAt);

        Assert.AreEqual(secondAssetId, package.Items.Single().AssetId);
        Assert.HasCount(4, package.Events);
        Assert.AreEqual(PackageStatus.Created, package.Status);
    }

    [TestMethod]
    public void RemoveAsset_WithMissingAsset_ThrowsWithoutAddingEvent()
    {
        var package = CreatePackage();

        Assert.ThrowsExactly<InvalidOperationException>(() => package.RemoveAsset(AssetId, OperatorId, "Operator", OperationAt));

        Assert.IsEmpty(package.Items);
        Assert.HasCount(1, package.Events);
    }

    [TestMethod]
    public void AddAsset_AfterRemoval_AllowsAddingSameAssetAgain()
    {
        var package = CreatePackage();

        package.AddAsset(AssetId, OperatorId, "Operator", OperationAt);
        package.RemoveAsset(AssetId, OperatorId, "Operator", OperationAt);
        package.AddAsset(AssetId, OperatorId, "Operator", OperationAt);

        Assert.AreEqual(AssetId, package.Items.Single().AssetId);
        Assert.HasCount(4, package.Events);
    }

    [TestMethod]
    public void AssignCourier_WithoutExistingCourier_AssignsAndAddsEvent()
    {
        var package = CreatePackage();

        package.AssignCourier(CourierId, null, OperatorId, "Operator", OperationAt);

        Assert.AreEqual(CourierId, package.CourierUserId);
        Assert.HasCount(2, package.Events);
    }

    [TestMethod]
    public void AssignCourier_WithDifferentCourier_ReplacesCourier()
    {
        var package = CreatePackage(courierUserId: CourierId);
        var replacementId = Guid.NewGuid();

        package.AssignCourier(replacementId, "Replacement", OperatorId, "Operator", OperationAt);

        Assert.AreEqual(replacementId, package.CourierUserId);
        Assert.HasCount(3, package.Events);
    }

    [TestMethod]
    public void AssignCourier_WithSameCourier_ThrowsWithoutAddingEvent()
    {
        var package = CreatePackage(courierUserId: CourierId);

        Assert.ThrowsExactly<InvalidOperationException>(() => package.AssignCourier(CourierId, null, OperatorId, "Operator", OperationAt));

        Assert.AreEqual(CourierId, package.CourierUserId);
        Assert.HasCount(2, package.Events);
    }

    [TestMethod]
    public void AssignCourier_WithEmptyCourierId_Throws()
    {
        var package = CreatePackage();

        AssertArgument(() => package.AssignCourier(Guid.Empty, null, OperatorId, "Operator", OperationAt), "courierUserId");

        Assert.IsNull(package.CourierUserId);
        Assert.HasCount(1, package.Events);
    }

    [TestMethod]
    public void RemoveCourier_WithAssignedCourier_ClearsCourierAndAddsEvent()
    {
        var package = CreatePackage(courierUserId: CourierId);

        package.RemoveCourier(null, OperatorId, "Operator", OperationAt);

        Assert.IsNull(package.CourierUserId);
        Assert.HasCount(3, package.Events);
    }

    [TestMethod]
    public void RemoveCourier_WithoutCourier_ThrowsWithoutAddingEvent()
    {
        var package = CreatePackage();

        Assert.ThrowsExactly<InvalidOperationException>(() => package.RemoveCourier(null, OperatorId, "Operator", OperationAt));

        Assert.IsNull(package.CourierUserId);
        Assert.HasCount(1, package.Events);
    }

    [TestMethod]
    public void Dispatch_WithAssetAndNoCourier_SetsDispatchState()
    {
        var package = CreatePackage();
        package.AddAsset(AssetId, OperatorId, "Operator", CreatedAt);

        package.Dispatch(null, OperatorId, "Operator", OperationAt);

        Assert.AreEqual(PackageStatus.InTransit, package.Status);
        Assert.AreEqual(OperationAt, package.DispatchedAtUtc);
        Assert.AreEqual(OperatorId, package.DispatchedByUserId);
        Assert.IsNull(package.CourierUserId);
        Assert.IsNull(package.ReceivedAtUtc);
        Assert.IsNull(package.CancelledAtUtc);
        Assert.AreEqual(1, package.Items.Count);
        Assert.AreEqual(3, package.Events.Count);
    }

    [TestMethod]
    public void Dispatch_WithoutAssets_ThrowsWithoutChangingState()
    {
        var package = CreatePackage();

        Assert.ThrowsExactly<InvalidOperationException>(() => package.Dispatch(null, OperatorId, "Operator", OperationAt));

        Assert.AreEqual(PackageStatus.Created, package.Status);
        Assert.IsNull(package.DispatchedAtUtc);
        Assert.IsNull(package.DispatchedByUserId);
        Assert.HasCount(1, package.Events);
    }

    [TestMethod]
    public void Dispatch_BeforeCreation_ThrowsWithoutChangingState()
    {
        var package = PrepareOperation("Dispatch");

        AssertArgument(() => package.Dispatch(null, OperatorId, "Operator", CreatedAt.AddTicks(-1)), "dispatchedAtUtc");

        Assert.AreEqual(PackageStatus.Created, package.Status);
        Assert.IsNull(package.DispatchedAtUtc);
        Assert.IsNull(package.DispatchedByUserId);
        Assert.HasCount(2, package.Events);
    }

    [TestMethod]
    public void Dispatch_AtCreationTime_IsAllowed()
    {
        var package = PrepareOperation("Dispatch");

        package.Dispatch(null, OperatorId, "Operator", CreatedAt);

        Assert.AreEqual(PackageStatus.InTransit, package.Status);
        Assert.AreEqual(CreatedAt, package.DispatchedAtUtc);
    }

    [TestMethod]
    public void Receive_AtDestination_SetsReceiptState()
    {
        var package = PrepareOperation("Receive");

        package.Receive(DestinationStoreId, null, OperatorId, "Operator", ReceivedAt);

        Assert.AreEqual(PackageStatus.Received, package.Status);
        Assert.AreEqual(ReceivedAt, package.ReceivedAtUtc);
        Assert.AreEqual(OperatorId, package.ReceivedByUserId);
        Assert.AreEqual(OperationAt, package.DispatchedAtUtc);
        Assert.IsNull(package.CancelledAtUtc);
        Assert.HasCount(1, package.Items);
        Assert.HasCount(4, package.Events);
    }

    [TestMethod]
    public void Receive_WithEmptyReceivingStoreId_Throws()
    {
        var package = PrepareOperation("Receive");

        AssertArgument(() => package.Receive(Guid.Empty, null, OperatorId, "Operator", ReceivedAt), "receivingStoreId");

        Assert.AreEqual(PackageStatus.InTransit, package.Status);
        Assert.HasCount(3, package.Events);
    }

    [TestMethod]
    public void Receive_AtWrongStore_ThrowsWithoutChangingState()
    {
        var package = PrepareOperation("Receive");

        Assert.ThrowsExactly<InvalidOperationException>(() => package.Receive(SourceStoreId, null, OperatorId, "Operator", ReceivedAt));

        Assert.AreEqual(PackageStatus.InTransit, package.Status);
        Assert.IsNull(package.ReceivedAtUtc);
        Assert.IsNull(package.ReceivedByUserId);
        Assert.HasCount(3, package.Events);
    }

    [TestMethod]
    public void Receive_BeforeDispatch_ThrowsWithoutChangingState()
    {
        var package = PrepareOperation("Receive");

        AssertArgument(
            () => package.Receive(
                DestinationStoreId,
                null,
                OperatorId,
                "Operator",
                OperationAt.AddTicks(-1)),
            "receivedAtUtc");

        Assert.AreEqual(PackageStatus.InTransit, package.Status);
        Assert.IsNull(package.ReceivedAtUtc);
        Assert.IsNull(package.ReceivedByUserId);
        Assert.HasCount(3, package.Events);
    }

    [TestMethod]
    public void Receive_AtDispatchTime_IsAllowed()
    {
        var package = PrepareOperation("Receive");

        package.Receive(DestinationStoreId, null, OperatorId, "Operator", OperationAt);

        Assert.AreEqual(PackageStatus.Received, package.Status);
        Assert.AreEqual(OperationAt, package.ReceivedAtUtc);
    }

    [TestMethod]
    public void Cancel_WithValidArguments_SetsCancellationState()
    {
        var package = CreatePackage(courierUserId: CourierId);
        package.AddAsset(AssetId, OperatorId, "Operator", CreatedAt);
        package.Cancel("No longer needed", OperatorId, "Operator", OperationAt);

        Assert.AreEqual(PackageStatus.Cancelled, package.Status);
        Assert.AreEqual(OperationAt, package.CancelledAtUtc);
        Assert.AreEqual(OperatorId, package.CancelledByUserId);
        Assert.IsNull(package.DispatchedAtUtc);
        Assert.IsNull(package.ReceivedAtUtc);
        Assert.AreEqual(CourierId, package.CourierUserId);
        Assert.HasCount(1, package.Items);
        Assert.HasCount(4, package.Events);
    }

    [TestMethod]
    public void Cancel_WithoutAssetsOrCourier_IsAllowed()
    {
        var package = CreatePackage();

        package.Cancel("Cancelled", OperatorId, "Operator", OperationAt);

        Assert.AreEqual(PackageStatus.Cancelled, package.Status);
        Assert.HasCount(2, package.Events);
    }

    [TestMethod]
    public void Cancel_BeforeCreation_ThrowsWithoutChangingState()
    {
        var package = CreatePackage();

        AssertArgument(() => package.Cancel("Cancelled", OperatorId, "Operator", CreatedAt.AddTicks(-1)), "cancelledAtUtc");

        Assert.AreEqual(PackageStatus.Created, package.Status);
        Assert.IsNull(package.CancelledAtUtc);
        Assert.IsNull(package.CancelledByUserId);
        Assert.HasCount(1, package.Events);
    }

    [TestMethod]
    public void Cancel_AtCreationTime_IsAllowed()
    {
        var package = CreatePackage();

        package.Cancel("Cancelled", OperatorId, "Operator", CreatedAt);

        Assert.AreEqual(PackageStatus.Cancelled, package.Status);
        Assert.AreEqual(CreatedAt, package.CancelledAtUtc);
    }

    [TestMethod]
    [DataRow("AddAsset")]
    [DataRow("RemoveAsset")]
    [DataRow("AssignCourier")]
    [DataRow("RemoveCourier")]
    [DataRow("Dispatch")]
    [DataRow("Receive")]
    [DataRow("Cancel")]
    public void Operation_WithInvalidUserOrDate_ThrowsWithoutMutation(string operation)
    {
        AssertRejected(
            operation,
            package => Invoke(package, operation, userId: Guid.Empty), "performedByUserId");

        foreach (var userName in new string?[]
                 {
                     null, "", " ", "\t\r\n", new string('U', 251)
                 })
        {
            AssertRejected(
                operation,
                package => Invoke(package, operation, userName: userName), "performedByUserName");
        }

        foreach (var kind in new[] { "Default", "Local", "Unspecified" })
        {
            AssertRejected(
                operation,
                package => Invoke(package, operation, date: InvalidDate(kind)), DateParameter(operation));
        }
    }

    [TestMethod]
    [DataRow("AssignCourier")]
    [DataRow("RemoveCourier")]
    [DataRow("Dispatch")]
    [DataRow("Receive")]
    [DataRow("Cancel")]
    public void Operation_WithInvalidComment_ThrowsWithoutMutation(string operation)
    {
        foreach (var comment in new[]
                 {
                     "", " ", "\t\r\n", new string('C', 2001)
                 })
        {
            AssertRejected(
                operation,
                package => Invoke(package, operation, comment: comment), "comment");
        }
    }

    [TestMethod]
    public void Cancel_WithNullComment_ThrowsWithoutMutation()
    {
        AssertRejected("Cancel", package => Invoke(package, "Cancel", comment: null), "comment");
    }

    [TestMethod]
    [DataRow("AssignCourier")]
    [DataRow("RemoveCourier")]
    [DataRow("Dispatch")]
    [DataRow("Receive")]
    [DataRow("Cancel")]
    public void Operation_WithMaximumTextLengths_IsAllowed(string operation)
    {
        // Arrange
        var package = PrepareOperation(operation);
        var eventCount = package.Events.Count;

        // Act
        Invoke(
            package,
            operation,
            userName: new string('U', 250),
            comment: new string('C', 2000));

        // Assert
        Assert.HasCount(eventCount + 1, package.Events);
    }

    [TestMethod]
    [DataRow("AddAsset")]
    [DataRow("RemoveAsset")]
    [DataRow("AssignCourier")]
    [DataRow("RemoveCourier")]
    [DataRow("Dispatch")]
    [DataRow("Cancel")]
    public void CreatedOnlyOperation_InOtherStates_ThrowsWithoutMutation(string operation)
    {
        foreach (var status in new[]
                 {
                     PackageStatus.InTransit,
                     PackageStatus.Received,
                     PackageStatus.Cancelled
                 })
        {
            var package = CreateInStatus(status);
            var before = Snapshot(package);

            Assert.ThrowsExactly<InvalidOperationException>(() => Invoke(package, operation));
            Assert.AreEqual(before, Snapshot(package));
        }
    }

    [TestMethod]
    [DataRow(PackageStatus.Created)]
    [DataRow(PackageStatus.Received)]
    [DataRow(PackageStatus.Cancelled)]
    public void Receive_WhenNotInTransit_ThrowsWithoutMutation(PackageStatus status)
    {
        var package = CreateInStatus(status);
        var before = Snapshot(package);

        Assert.ThrowsExactly<InvalidOperationException>(() => Invoke(package, "Receive"));
        Assert.AreEqual(before, Snapshot(package));
    }

    private static Package CreatePackage(
        string packageNumber = "PKG-2026-0001",
        Guid? sourceStoreId = null,
        Guid? destinationStoreId = null,
        Guid? courierUserId = null,
        string? comment = null,
        DateTime? createdAtUtc = null,
        Guid? createdByUserId = null,
        string createdByUserName = "Creator")
    {
        return Package.Create(
            packageNumber,
            sourceStoreId ?? SourceStoreId,
            destinationStoreId ?? DestinationStoreId,
            courierUserId,
            comment,
            createdAtUtc ?? CreatedAt,
            createdByUserId ?? CreatorId,
            createdByUserName);
    }

    private static Package PrepareOperation(string operation)
    {
        var package = CreatePackage(courierUserId: operation == "RemoveCourier" ? CourierId : null);

        if (operation is "RemoveAsset" or "Dispatch" or "Receive")
        {
            package.AddAsset(AssetId, OperatorId, "Operator", CreatedAt);
        }

        if (operation == "Receive")
        {
            package.Dispatch(null, OperatorId, "Operator", OperationAt);
        }

        return package;
    }

    private static Package CreateInStatus(PackageStatus status)
    {
        var package = CreatePackage(courierUserId: CourierId);
        package.AddAsset(AssetId, OperatorId, "Operator", CreatedAt);

        switch (status)
        {
            case PackageStatus.Created:
                break;

            case PackageStatus.InTransit:
                package.Dispatch(null, OperatorId, "Operator", OperationAt);
                break;

            case PackageStatus.Received:
                package.Dispatch(null, OperatorId, "Operator", OperationAt);
                package.Receive(DestinationStoreId, null, OperatorId, "Operator", ReceivedAt);
                break;

            case PackageStatus.Cancelled:
                package.Cancel("Cancelled", OperatorId, "Operator", OperationAt);
                break;

            default:
                throw new ArgumentOutOfRangeException(nameof(status));
        }

        return package;
    }

    private static void Invoke(
        Package package,
        string operation,
        Guid? userId = null,
        string? userName = "Operator",
        DateTime? date = null,
        string? comment = "Operation comment")
    {
        var actorId = userId ?? OperatorId;
        var at = date ?? ReceivedAt;

        switch (operation)
        {
            case "AddAsset":
                package.AddAsset(AssetId, actorId, userName!, at);
                break;

            case "RemoveAsset":
                package.RemoveAsset(AssetId, actorId, userName!, at);
                break;

            case "AssignCourier":
                package.AssignCourier(
                    CourierId, comment, actorId, userName!, at);
                break;

            case "RemoveCourier":
                package.RemoveCourier(comment, actorId, userName!, at);
                break;

            case "Dispatch":
                package.Dispatch(comment, actorId, userName!, at);
                break;

            case "Receive":
                package.Receive(
                    DestinationStoreId, comment, actorId, userName!, at);
                break;

            case "Cancel":
                package.Cancel(comment!, actorId, userName!, at);
                break;

            default:
                throw new ArgumentOutOfRangeException(nameof(operation));
        }
    }

    private static void AssertRejected(string operation, Action<Package> action, string expectedParameter)
    {
        var package = PrepareOperation(operation);
        var before = Snapshot(package);

        AssertArgument(() => action(package), expectedParameter);

        Assert.AreEqual(before, Snapshot(package));
    }

    private static void AssertArgument(Action action, string expectedParameter)
    {
        var exception = Assert.ThrowsExactly<ArgumentException>(action);

        Assert.AreEqual(expectedParameter, exception.ParamName);
    }

    private static DateTime InvalidDate(string kind)
    {
        return kind switch
        {
            "Default" => default,
            "Local" => DateTime.SpecifyKind(OperationAt, DateTimeKind.Local),
            "Unspecified" => DateTime.SpecifyKind(OperationAt, DateTimeKind.Unspecified),
            _ => throw new ArgumentOutOfRangeException(nameof(kind))
        };
    }

    private static string DateParameter(string operation)
    {
        return operation switch
        {
            "Dispatch" => "dispatchedAtUtc",
            "Receive" => "receivedAtUtc",
            "Cancel" => "cancelledAtUtc",
            _ => "occurredAtUtc"
        };
    }

    private static (
        PackageStatus Status,
        Guid? Courier,
        DateTime? DispatchedAt,
        Guid? DispatchedBy,
        DateTime? ReceivedAt,
        Guid? ReceivedBy,
        DateTime? CancelledAt,
        Guid? CancelledBy,
        int EventCount,
        string AssetIds) Snapshot(Package package)
    {
        return (
            package.Status,
            package.CourierUserId,
            package.DispatchedAtUtc,
            package.DispatchedByUserId,
            package.ReceivedAtUtc,
            package.ReceivedByUserId,
            package.CancelledAtUtc,
            package.CancelledByUserId,
            package.Events.Count,
            string.Join(
                ",",
                package.Items
                    .Select(item => item.AssetId)
                    .OrderBy(id => id)));
    }
}