using System;
using System.Linq;
using AssetManagement.Domain.Entities.Packages;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace AssetManagement.Tests.Domain.Entities.Packages;

[TestClass]
public sealed class PackageEventRecordingTests
{
    private static readonly Guid SourceStoreId = Guid.NewGuid();
    private static readonly Guid DestinationStoreId = Guid.NewGuid();
    private static readonly Guid CreatorId = Guid.NewGuid();
    private static readonly Guid OperatorId = Guid.NewGuid();
    private static readonly Guid CourierId = Guid.NewGuid();
    private static readonly Guid ReplacementCourierId = Guid.NewGuid();
    private static readonly Guid AssetId = Guid.NewGuid();

    private static readonly DateTime CreatedAt = new(2026, 11, 17, 8, 0, 0, DateTimeKind.Utc);
    private static readonly DateTime OperationAt = CreatedAt.AddMinutes(10).AddTicks(1234);

    [TestMethod]
    public void Create_RecordsCreatedEventWithAllFields()
    {
        // Act
        var package = CreatePackage(comment: "  Initial comment  ");

        // Assert
        AssertEvent(
            package.Events.Single(),
            package.Id,
            PackageEventType.Created,
            previousStatus: null,
            newStatus: PackageStatus.Created,
            relatedAssetId: null,
            relatedUserId: null,
            comment: "Initial comment",
            performedByUserId: CreatorId,
            performedByUserName: "Creator",
            occurredAtUtc: CreatedAt);
    }

    [TestMethod]
    public void Create_WithCourier_RecordsCreatedThenCourierAssigned()
    {
        // Act
        var package = CreatePackage(courierUserId: CourierId, comment: "Initial comment");
        var events = package.Events.ToArray();

        // Assert
        Assert.AreEqual(2, events.Length);

        AssertEvent(
            events[0],
            package.Id,
            PackageEventType.Created,
            previousStatus: null,
            newStatus: PackageStatus.Created,
            relatedAssetId: null,
            relatedUserId: null,
            comment: "Initial comment",
            performedByUserId: CreatorId,
            performedByUserName: "Creator",
            occurredAtUtc: CreatedAt);

        AssertEvent(
            events[1],
            package.Id,
            PackageEventType.CourierAssigned,
            previousStatus: PackageStatus.Created,
            newStatus: PackageStatus.Created,
            relatedAssetId: null,
            relatedUserId: CourierId,
            comment: null,
            performedByUserId: CreatorId,
            performedByUserName: "Creator",
            occurredAtUtc: CreatedAt);

        Assert.AreNotEqual(events[0].Id, events[1].Id);
    }

    [TestMethod]
    public void AddAsset_StoresAllPackageItemFields()
    {
        // Arrange
        var package = CreatePackage();

        // Act
        package.AddAsset(
            AssetId,
            OperatorId,
            "Operator",
            OperationAt);

        // Assert
        var item = package.Items.Single();

        Assert.AreNotEqual(Guid.Empty, item.Id);
        Assert.AreEqual(package.Id, item.PackageId);
        Assert.AreEqual(AssetId, item.AssetId);
        Assert.AreEqual(OperatorId, item.AddedByUserId);
        Assert.AreEqual(OperationAt.Ticks, item.AddedAtUtc.Ticks);
        Assert.AreEqual(DateTimeKind.Utc, item.AddedAtUtc.Kind);
    }

    [TestMethod]
    public void AddAsset_WithDifferentAssets_GeneratesDifferentItemIds()
    {
        // Arrange
        var package = CreatePackage();

        // Act
        package.AddAsset(AssetId, OperatorId, "Operator", OperationAt);
        package.AddAsset(Guid.NewGuid(), OperatorId, "Operator", OperationAt);

        // Assert
        var items = package.Items.ToArray();

        Assert.AreEqual(2, items.Length);
        Assert.AreNotEqual(Guid.Empty, items[0].Id);
        Assert.AreNotEqual(Guid.Empty, items[1].Id);
        Assert.AreNotEqual(items[0].Id, items[1].Id);
    }

    [TestMethod]
    [DataRow(PackageEventType.AssetAdded)]
    [DataRow(PackageEventType.AssetRemoved)]
    [DataRow(PackageEventType.CourierAssigned)]
    [DataRow(PackageEventType.CourierChanged)]
    [DataRow(PackageEventType.CourierRemoved)]
    [DataRow(PackageEventType.Dispatched)]
    [DataRow(PackageEventType.Received)]
    [DataRow(PackageEventType.Cancelled)]
    public void Operation_RecordsEventWithAllFields(PackageEventType eventType)
    {
        // Arrange
        var package = PreparePackage(eventType);
        var existingEvents = package.Events.ToArray();

        // Act
        PerformOperation(package, eventType);

        // Assert
        var events = package.Events.ToArray();

        Assert.HasCount(existingEvents.Length + 1, events);

        // Existing events must remain unchanged and in the same order.
        for (var index = 0; index < existingEvents.Length; index++)
        {
            Assert.AreSame(existingEvents[index], events[index]);
        }

        var expectedPreviousStatus = eventType == PackageEventType.Received ? PackageStatus.InTransit : PackageStatus.Created;

        var expectedNewStatus = eventType switch
        {
            PackageEventType.Dispatched => PackageStatus.InTransit,
            PackageEventType.Received => PackageStatus.Received,
            PackageEventType.Cancelled => PackageStatus.Cancelled,
            _ => PackageStatus.Created
        };

        Guid? expectedAssetId =
            eventType is PackageEventType.AssetAdded
                or PackageEventType.AssetRemoved
                ? AssetId
                : null;

        Guid? expectedUserId = eventType switch
        {
            PackageEventType.CourierAssigned => CourierId,
            PackageEventType.CourierChanged => ReplacementCourierId,
            PackageEventType.CourierRemoved => CourierId,
            _ => null
        };

        var expectedComment =
            eventType is PackageEventType.AssetAdded
                or PackageEventType.AssetRemoved
                ? null
                : "Operation comment";

        AssertEvent(
            events[^1],
            package.Id,
            eventType,
            expectedPreviousStatus,
            expectedNewStatus,
            expectedAssetId,
            expectedUserId,
            expectedComment,
            OperatorId,
            "Operator",
            OperationAt);

        Assert.AreEqual(
            events.Length,
            events.Select(packageEvent => packageEvent.Id)
                .Distinct()
                .Count());
    }

    [TestMethod]
    public void FullLifecycle_RecordsEventsInOperationOrder()
    {
        // Arrange
        var package = CreatePackage(courierUserId: CourierId);

        // Act
        package.AddAsset(
            AssetId,
            OperatorId,
            "Operator",
            CreatedAt.AddMinutes(1));

        package.AssignCourier(
            ReplacementCourierId,
            null,
            OperatorId,
            "Operator",
            CreatedAt.AddMinutes(2));

        package.Dispatch(
            null,
            OperatorId,
            "Operator",
            CreatedAt.AddMinutes(3));

        package.Receive(
            DestinationStoreId,
            null,
            OperatorId,
            "Operator",
            CreatedAt.AddMinutes(4));

        // Assert
        CollectionAssert.AreEqual(
            new[]
            {
                PackageEventType.Created,
                PackageEventType.CourierAssigned,
                PackageEventType.AssetAdded,
                PackageEventType.CourierChanged,
                PackageEventType.Dispatched,
                PackageEventType.Received
            },
            package.Events.Select(packageEvent => packageEvent.Type).ToArray());

        Assert.AreEqual(PackageStatus.Received, package.Status);
        Assert.AreEqual(ReplacementCourierId, package.CourierUserId);
        Assert.HasCount(1, package.Items);
    }

    private static Package CreatePackage(Guid? courierUserId = null, string? comment = null)
    {
        return Package.Create(
            packageNumber: "PKG-2026-0001",
            sourceStoreId: SourceStoreId,
            destinationStoreId: DestinationStoreId,
            courierUserId: courierUserId,
            comment: comment,
            createdAtUtc: CreatedAt,
            createdByUserId: CreatorId,
            createdByUserName: "  Creator  ");
    }

    private static Package PreparePackage(PackageEventType eventType)
    {
        var needsExistingCourier = eventType is PackageEventType.CourierChanged or PackageEventType.CourierRemoved;
        var package = CreatePackage(courierUserId: needsExistingCourier ? CourierId : null);

        if (eventType 
            is PackageEventType.AssetRemoved
            or PackageEventType.Dispatched
            or PackageEventType.Received)
        {
            package.AddAsset(
                AssetId,
                CreatorId,
                "Creator",
                CreatedAt);
        }

        if (eventType == PackageEventType.Received)
        {
            package.Dispatch(
                null,
                CreatorId,
                "Creator",
                CreatedAt.AddMinutes(1));
        }

        return package;
    }

    private static void PerformOperation(Package package, PackageEventType eventType)
    {
        const string userName = "  Operator  ";
        const string comment = "  Operation comment  ";

        switch (eventType)
        {
            case PackageEventType.AssetAdded:
                package.AddAsset(AssetId, OperatorId, userName, OperationAt);
                break;

            case PackageEventType.AssetRemoved:
                package.RemoveAsset(AssetId, OperatorId, userName, OperationAt);
                break;

            case PackageEventType.CourierAssigned:
                package.AssignCourier(
                    CourierId,
                    comment,
                    OperatorId,
                    userName,
                    OperationAt);
                break;

            case PackageEventType.CourierChanged:
                package.AssignCourier(
                    ReplacementCourierId,
                    comment,
                    OperatorId,
                    userName,
                    OperationAt);
                break;

            case PackageEventType.CourierRemoved:
                package.RemoveCourier(comment, OperatorId, userName, OperationAt);
                break;

            case PackageEventType.Dispatched:
                package.Dispatch(comment, OperatorId, userName, OperationAt);
                break;

            case PackageEventType.Received:
                package.Receive(
                    DestinationStoreId,
                    comment,
                    OperatorId,
                    userName,
                    OperationAt);
                break;

            case PackageEventType.Cancelled:
                package.Cancel(comment, OperatorId, userName, OperationAt);
                break;

            default:
                throw new ArgumentOutOfRangeException(nameof(eventType));
        }
    }

    private static void AssertEvent(
        PackageEvent actual,
        Guid packageId,
        PackageEventType type,
        PackageStatus? previousStatus,
        PackageStatus newStatus,
        Guid? relatedAssetId,
        Guid? relatedUserId,
        string? comment,
        Guid performedByUserId,
        string performedByUserName,
        DateTime occurredAtUtc)
    {
        Assert.AreNotEqual(Guid.Empty, actual.Id);
        Assert.AreEqual(packageId, actual.PackageId);
        Assert.AreEqual(type, actual.Type);
        Assert.AreEqual(previousStatus, actual.PreviousStatus);
        Assert.AreEqual(newStatus, actual.NewStatus);
        Assert.AreEqual(relatedAssetId, actual.RelatedAssetId);
        Assert.AreEqual(relatedUserId, actual.RelatedUserId);
        Assert.AreEqual(comment, actual.Comment);
        Assert.AreEqual(performedByUserId, actual.PerformedByUserId);
        Assert.AreEqual(performedByUserName, actual.PerformedByUserName);
        Assert.AreEqual(occurredAtUtc.Ticks, actual.OccurredAtUtc.Ticks);
        Assert.AreEqual(DateTimeKind.Utc, actual.OccurredAtUtc.Kind);
    }
}