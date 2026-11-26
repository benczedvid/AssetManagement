using AssetManagement.Domain.Entities.ServiceDesk;

namespace AssetManagement.Tests.Domain.ServiceDesk;

[TestClass]
public sealed class ServiceDeskRequestClosureOutboxTests
{
    private static readonly Guid CaseId = Guid.NewGuid();
    private static readonly Guid EventId = Guid.NewGuid();
    private static readonly DateTime CreatedAt = new(2026, 11, 17, 8, 0, 0, DateTimeKind.Utc);

    [TestMethod]
    public void Create_WithValidArguments_SetsInitialProperties()
    {
        var entry = ServiceDeskRequestClosureOutbox.Create(CaseId, EventId, CreatedAt);

        Assert.AreNotEqual(Guid.Empty, entry.Id);
        Assert.AreEqual(CaseId, entry.ServiceCaseId);
        Assert.AreEqual(EventId, entry.ServiceCaseEventId);
        Assert.AreEqual(ServiceDeskRequestClosureOutboxStatus.Pending, entry.Status);
        Assert.AreEqual(CreatedAt, entry.CreatedAtUtc);
        Assert.AreEqual(DateTimeKind.Utc, entry.CreatedAtUtc.Kind);
        Assert.IsNull(entry.ProcessingOperationId);
        Assert.IsNull(entry.ProcessingStartedAtUtc);
        Assert.IsNull(entry.ProcessedAtUtc);
        Assert.IsNull(entry.Error);
    }

    [TestMethod]
    public void Create_CalledTwice_GeneratesDifferentIds()
    {
        var first = ServiceDeskRequestClosureOutbox.Create(CaseId, EventId, CreatedAt);
        var second = ServiceDeskRequestClosureOutbox.Create(CaseId, EventId, CreatedAt);

        Assert.AreNotEqual(first.Id, second.Id);
    }

    [TestMethod]
    [DataRow("serviceCaseId")]
    [DataRow("serviceCaseEventId")]
    public void Create_WithEmptyIdentifier_Throws(string parameter)
    {
        var exception = Assert.ThrowsExactly<ArgumentException>(
            () => ServiceDeskRequestClosureOutbox.Create(
                parameter == "serviceCaseId" ? Guid.Empty : CaseId,
                parameter == "serviceCaseEventId" ? Guid.Empty : EventId,
                CreatedAt));

        Assert.AreEqual(parameter, exception.ParamName);
    }

    [TestMethod]
    public void Create_WithNonUtcDate_Throws()
    {
        foreach (var date in new[]
                 {
                     default,
                     DateTime.SpecifyKind(CreatedAt, DateTimeKind.Local),
                     DateTime.SpecifyKind(CreatedAt, DateTimeKind.Unspecified)
                 })
        {
            var exception = Assert.ThrowsExactly<ArgumentException>(() => ServiceDeskRequestClosureOutbox.Create(CaseId, EventId, date));

            Assert.AreEqual("createdAtUtc", exception.ParamName);
        }
    }
}