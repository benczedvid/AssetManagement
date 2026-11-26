using AssetManagement.Domain.Entities.ServiceDesk;

namespace AssetManagement.Tests.Domain.ServiceDesk;

[TestClass]
public sealed class ServiceDeskRequestOutboxTests
{
    private static readonly Guid CaseId = Guid.NewGuid();
    private static readonly Guid EventId = Guid.NewGuid();
    private static readonly DateTime CreatedAt = new(2026, 11, 17, 8, 0, 0, DateTimeKind.Utc);

    [TestMethod]
    public void Create_WithValidArguments_SetsInitialProperties()
    {
        var entry = CreateEntry(
            subject: "  Request subject  ",
            description: "  Original description\r\n");

        Assert.AreNotEqual(Guid.Empty, entry.Id);
        Assert.AreEqual(CaseId, entry.ServiceCaseId);
        Assert.AreEqual(EventId, entry.ServiceCaseEventId);
        Assert.AreEqual("Request subject", entry.Subject);
        Assert.AreEqual("  Original description\r\n", entry.Description);
        Assert.AreEqual(ServiceDeskRequestOutboxStatus.Pending, entry.Status);
        Assert.AreEqual(CreatedAt, entry.CreatedAtUtc);
        Assert.AreEqual(DateTimeKind.Utc, entry.CreatedAtUtc.Kind);
        Assert.IsNull(entry.ServiceDeskRequestId);
        Assert.IsNull(entry.ProcessingOperationId);
        Assert.IsNull(entry.ProcessingStartedAtUtc);
        Assert.IsNull(entry.ProcessedAtUtc);
        Assert.IsNull(entry.Error);
    }

    [TestMethod]
    public void Create_CalledTwice_GeneratesDifferentIds()
    {
        var first = CreateEntry();
        var second = CreateEntry();

        Assert.AreNotEqual(first.Id, second.Id);
    }

    [TestMethod]
    [DataRow("serviceCaseId")]
    [DataRow("serviceCaseEventId")]
    public void Create_WithEmptyIdentifier_Throws(string parameter)
    {
        AssertArgument(
            () => ServiceDeskRequestOutbox.Create(
                parameter == "serviceCaseId" ? Guid.Empty : CaseId,
                parameter == "serviceCaseEventId" ? Guid.Empty : EventId,
                "Request subject",
                "Description",
                CreatedAt),
            parameter);
    }

    [TestMethod]
    [DataRow(null)]
    [DataRow("")]
    [DataRow(" ")]
    [DataRow("\t\r\n")]
    public void Create_WithInvalidSubject_Throws(string? subject)
    {
        AssertArgument(() => CreateEntry(subject: subject!), "subject");
    }

    [TestMethod]
    public void Create_WithMaximumTrimmedSubjectLength_AcceptsValue()
    {
        var subject = new string('S', 250);
        var entry = CreateEntry(subject: $"  {subject}  ");

        Assert.AreEqual(subject, entry.Subject);
    }

    [TestMethod]
    public void Create_WithTooLongSubject_Throws()
    {
        AssertArgument(() => CreateEntry(subject: new string('S', 251)), "subject");
    }

    [TestMethod]
    public void Create_WithNullDescription_ThrowsArgumentNullException()
    {
        var exception = Assert.ThrowsExactly<ArgumentNullException>(() => CreateEntry(description: null!));

        Assert.AreEqual("description", exception.ParamName);
    }

    [TestMethod]
    [DataRow("")]
    [DataRow(" ")]
    [DataRow("\t\r\n")]
    public void Create_WithEmptyOrWhitespaceDescription_PreservesValue(string description)
    {
        var entry = CreateEntry(description: description);

        Assert.AreEqual(description, entry.Description);
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
            AssertArgument(() => CreateEntry(createdAt: date), "createdAtUtc");
        }
    }

    private static ServiceDeskRequestOutbox CreateEntry(string subject = "Request subject", string description = "Description", DateTime? createdAt = null)
    {
        return ServiceDeskRequestOutbox.Create(
            CaseId,
            EventId,
            subject,
            description,
            createdAt ?? CreatedAt);
    }

    private static void AssertArgument(Action action, string parameter)
    {
        var exception = Assert.ThrowsExactly<ArgumentException>(action);
        Assert.AreEqual(parameter, exception.ParamName);
    }
}