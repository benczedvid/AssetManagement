using AssetManagement.Domain.Entities.ServiceDesk;

namespace AssetManagement.Tests.Domain.ServiceDesk;

[TestClass]
public sealed class ServiceDeskReconciliationAuditTests
{
    private static readonly Guid OutboxId = Guid.NewGuid();
    private static readonly Guid CaseId = Guid.NewGuid();
    private static readonly Guid OperationId = Guid.NewGuid();
    private static readonly Guid UserId = Guid.NewGuid();
    private static readonly DateTime VerifiedAt = new(2026, 11, 17, 8, 0, 0, DateTimeKind.Utc);

    [TestMethod]
    public void Create_WithValidArguments_SetsAllProperties()
    {
        var audit = CreateAudit(
            externalIdentifier: "  EXT-123  ",
            userName: "  Verifier  ",
            comment: "  Verified in ServiceDesk  ");

        Assert.AreNotEqual(Guid.Empty, audit.Id);
        Assert.AreEqual(ServiceDeskReconciliationOperationType.RequestCreation, audit.Operation);
        Assert.AreEqual(OutboxId, audit.OutboxId);
        Assert.AreEqual(CaseId, audit.ServiceCaseId);
        Assert.AreEqual(OperationId, audit.ProcessingOperationId);
        Assert.AreEqual(ServiceDeskReconciliationAuditOutcome.Completed, audit.Outcome);
        Assert.AreEqual("EXT-123", audit.ExternalIdentifier);
        Assert.AreEqual(UserId, audit.VerifiedByUserId);
        Assert.AreEqual("Verifier", audit.VerifiedByUserName);
        Assert.AreEqual(VerifiedAt, audit.VerifiedAtUtc);
        Assert.AreEqual(DateTimeKind.Utc, audit.VerifiedAtUtc.Kind);
        Assert.AreEqual("Verified in ServiceDesk", audit.Comment);
    }

    [TestMethod]
    public void Create_CalledTwice_GeneratesDifferentIds()
    {
        var first = CreateAudit();
        var second = CreateAudit();

        Assert.AreNotEqual(first.Id, second.Id);
    }

    [TestMethod]
    public void Create_WithDefinedOperationsAndOutcomes_PreservesValues()
    {
        foreach (var operation in Enum.GetValues<ServiceDeskReconciliationOperationType>())
        {
            foreach (var outcome in Enum.GetValues<ServiceDeskReconciliationAuditOutcome>())
            {
                var audit = CreateAudit(operation: operation, outcome: outcome);

                Assert.AreEqual(operation, audit.Operation);
                Assert.AreEqual(outcome, audit.Outcome);
            }
        }
    }

    [TestMethod]
    [DataRow("outboxId")]
    [DataRow("serviceCaseId")]
    [DataRow("processingOperationId")]
    [DataRow("verifiedByUserId")]
    public void Create_WithEmptyIdentifier_Throws(string parameter)
    {
        var exception = Assert.ThrowsExactly<ArgumentException>(
            () => ServiceDeskReconciliationAudit.Create(
                ServiceDeskReconciliationOperationType.RequestCreation,
                parameter == "outboxId" ? Guid.Empty : OutboxId,
                parameter == "serviceCaseId" ? Guid.Empty : CaseId,
                parameter == "processingOperationId" ? Guid.Empty : OperationId, ServiceDeskReconciliationAuditOutcome.Completed, "EXT-123",
                parameter == "verifiedByUserId" ? Guid.Empty : UserId, "Verifier", VerifiedAt, "Verified"));

        Assert.AreEqual("Audit identifiers cannot be empty.", exception.Message);
        Assert.IsNull(exception.ParamName);
    }

    [TestMethod]
    [DataRow(null)]
    [DataRow("")]
    [DataRow(" ")]
    [DataRow("\t\r\n")]
    public void Create_WithInvalidVerifierName_Throws(string? userName)
    {
        var exception = Assert.ThrowsExactly<ArgumentException>(() => CreateAudit(userName: userName!));

        Assert.AreEqual("verifiedByUserName", exception.ParamName);
    }

    [TestMethod]
    [DataRow(null)]
    [DataRow("")]
    [DataRow(" ")]
    [DataRow("\t\r\n")]
    public void Create_WithInvalidComment_Throws(string? comment)
    {
        var exception = Assert.ThrowsExactly<ArgumentException>(() => CreateAudit(comment: comment!));

        Assert.AreEqual("comment", exception.ParamName);
    }

    [TestMethod]
    public void Create_WithNonUtcDate_Throws()
    {
        foreach (var date in new[]
                 {
                     default,
                     DateTime.SpecifyKind(VerifiedAt, DateTimeKind.Local),
                     DateTime.SpecifyKind(VerifiedAt, DateTimeKind.Unspecified)
                 })
        {
            var exception = Assert.ThrowsExactly<ArgumentException>(() => CreateAudit(verifiedAt: date));

            Assert.AreEqual("verifiedAtUtc", exception.ParamName);
        }
    }

    [TestMethod]
    [DataRow(null, null)]
    [DataRow("", "")]
    [DataRow("   ", "")]
    [DataRow("  EXT-123  ", "EXT-123")]
    public void Create_NormalizesOptionalExternalIdentifier(string? externalIdentifier, string? expected)
    {
        var audit = CreateAudit(externalIdentifier: externalIdentifier);

        Assert.AreEqual(expected, audit.ExternalIdentifier);
    }

    private static ServiceDeskReconciliationAudit CreateAudit(
        ServiceDeskReconciliationOperationType operation = ServiceDeskReconciliationOperationType.RequestCreation,
        ServiceDeskReconciliationAuditOutcome outcome = ServiceDeskReconciliationAuditOutcome.Completed,
        string? externalIdentifier = null,
        string userName = "Verifier",
        DateTime? verifiedAt = null,
        string comment = "Verified")
    {
        return ServiceDeskReconciliationAudit.Create(
            operation,
            OutboxId,
            CaseId,
            OperationId,
            outcome,
            externalIdentifier,
            UserId,
            userName,
            verifiedAt ?? VerifiedAt,
            comment);
    }
}