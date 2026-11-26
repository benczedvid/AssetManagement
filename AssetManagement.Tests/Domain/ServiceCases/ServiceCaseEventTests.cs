using System;
using AssetManagement.Domain.Entities.ServiceCases;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace AssetManagement.Tests.Domain.Entities.ServiceCases;

[TestClass]
public sealed class ServiceCaseEventTests
{
    private static readonly Guid CaseId = Guid.NewGuid();
    private static readonly Guid UserId = Guid.NewGuid();
    private static readonly DateTime OccurredAt = new DateTime(2026, 11, 17, 8, 0, 0, DateTimeKind.Utc).AddTicks(1234);

    [TestMethod]
    public void Create_WithValidArguments_SetsAllProperties()
    {
        var item = CreateEvent(comment: "  Inspection started  ", userName: "  Test User  ");

        Assert.AreNotEqual(Guid.Empty, item.Id);
        Assert.AreEqual(CaseId, item.ServiceCaseId);
        Assert.AreEqual(ServiceCaseEventType.ReceivedAtHeadquarters, item.Type);
        Assert.AreEqual(ServiceCaseStatus.AwaitingCentralReceipt, item.PreviousStatus);
        Assert.AreEqual(ServiceCaseStatus.UnderCentralInspection, item.NewStatus);
        Assert.AreEqual("Inspection started", item.Comment);
        Assert.AreEqual(UserId, item.PerformedByUserId);
        Assert.AreEqual("Test User", item.PerformedByUserName);
        Assert.AreEqual(OccurredAt.Ticks, item.OccurredAtUtc.Ticks);
        Assert.AreEqual(DateTimeKind.Utc, item.OccurredAtUtc.Kind);
    }

    [TestMethod]
    public void Create_CalledTwice_GeneratesDifferentIds()
    {
        Assert.AreNotEqual(CreateEvent().Id, CreateEvent().Id);
    }

    [TestMethod]
    public void Create_WithNullComment_PreservesNull()
    {
        Assert.IsNull(CreateEvent().Comment);
    }

    [TestMethod]
    public void Create_WithMaximumTextLengths_AcceptsValues()
    {
        var item = CreateEvent(
            comment: new string('C', 2000),
            userName: new string('U', 250));

        Assert.AreEqual(2000, item.Comment!.Length);
        Assert.AreEqual(250, item.PerformedByUserName.Length);
    }

    [TestMethod]
    public void Create_CreationEvent_RequiresNullPreviousStatus()
    {
        var item = CreateEvent(
            type: ServiceCaseEventType.Created,
            previousStatus: null);

        Assert.IsNull(item.PreviousStatus);

        AssertParameter<ArgumentException>(() => CreateEvent(type: ServiceCaseEventType.Created), "previousStatus");
    }

    [TestMethod]
    public void Create_NonCreationEvents_RequirePreviousAndDifferentNewStatus()
    {
        foreach (var type in Enum.GetValues<ServiceCaseEventType>())
        {
            if (type is ServiceCaseEventType.Unknown or ServiceCaseEventType.Created)
            {
                continue;
            }

            AssertParameter<ArgumentException>(() => CreateEvent(type: type, previousStatus: null), "previousStatus");

            AssertParameter<ArgumentException>(
                () => CreateEvent(
                    type: type,
                    previousStatus: ServiceCaseStatus.UnderCentralInspection,
                    newStatus: ServiceCaseStatus.UnderCentralInspection),
                "newStatus");

            var item = CreateEvent(type: type);
            Assert.AreEqual(type, item.Type);
        }
    }

    [TestMethod]
    public void Create_WithInvalidEventType_Throws()
    {
        foreach (var type in new[]
                 {
                     ServiceCaseEventType.Unknown,
                     (ServiceCaseEventType)999
                 })
        {
            AssertParameter<ArgumentOutOfRangeException>(
                () => CreateEvent(type: type),
                "type");
        }
    }

    [TestMethod]
    public void Create_WithInvalidStatuses_Throws()
    {
        foreach (var status in new[]
                 {
                     ServiceCaseStatus.Unknown,
                     (ServiceCaseStatus)999
                 })
        {
            AssertParameter<ArgumentOutOfRangeException>(() => CreateEvent(previousStatus: status), "previousStatus");
            AssertParameter<ArgumentOutOfRangeException>(() => CreateEvent(newStatus: status), "newStatus");
        }
    }

    [TestMethod]
    public void Create_WithEmptyIdentifiers_Throws()
    {
        AssertParameter<ArgumentException>(() => CreateEvent(serviceCaseId: Guid.Empty), "serviceCaseId");
        AssertParameter<ArgumentException>(() => CreateEvent(userId: Guid.Empty), "performedByUserId");
    }

    [TestMethod]
    [DataRow("")]
    [DataRow(" ")]
    [DataRow("\t\r\n")]
    public void Create_WithWhitespaceComment_Throws(string comment)
    {
        AssertParameter<ArgumentException>(() => CreateEvent(comment: comment), "comment");
    }

    [TestMethod]
    public void Create_WithTooLongComment_Throws()
    {
        AssertParameter<ArgumentException>(() => CreateEvent(comment: new string('C', 2001)), "comment");
    }

    [TestMethod]
    public void Create_WithNullUserName_ThrowsArgumentNullException()
    {
        AssertParameter<ArgumentNullException>(() => CreateEvent(userName: null!), "performedByUserName");
    }

    [TestMethod]
    [DataRow("")]
    [DataRow(" ")]
    [DataRow("\t\r\n")]
    public void Create_WithWhitespaceUserName_Throws(string userName)
    {
        AssertParameter<ArgumentException>(() => CreateEvent(userName: userName), "performedByUserName");
    }

    [TestMethod]
    public void Create_WithTooLongUserName_Throws()
    {
        AssertParameter<ArgumentException>(() => CreateEvent(userName: new string('U', 251)), "performedByUserName");
    }

    [TestMethod]
    public void Create_WithInvalidDate_Throws()
    {
        foreach (var date in new[]
                 {
                     default,
                     DateTime.SpecifyKind(OccurredAt, DateTimeKind.Local),
                     DateTime.SpecifyKind(OccurredAt, DateTimeKind.Unspecified)
                 })
        {
            AssertParameter<ArgumentException>(() => CreateEvent(occurredAt: date), "occurredAtUtc");
        }
    }

    private static ServiceCaseEvent CreateEvent(
        Guid? serviceCaseId = null,
        ServiceCaseEventType type = ServiceCaseEventType.ReceivedAtHeadquarters,
        ServiceCaseStatus? previousStatus = ServiceCaseStatus.AwaitingCentralReceipt,
        ServiceCaseStatus newStatus = ServiceCaseStatus.UnderCentralInspection,
        string? comment = null,
        Guid? userId = null,
        string userName = "Test User",
        DateTime? occurredAt = null)
    {
        return ServiceCaseEvent.Create(
            serviceCaseId ?? CaseId,
            type,
            previousStatus,
            newStatus,
            comment,
            userId ?? UserId,
            userName,
            occurredAt ?? OccurredAt);
    }

    private static void AssertParameter<TException>(Action action, string parameter)
        where TException : ArgumentException
    {
        var exception = Assert.ThrowsExactly<TException>(action);
        Assert.AreEqual(parameter, exception.ParamName);
    }
}