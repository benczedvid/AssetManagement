using AssetManagement.Domain.Entities.ServiceCases;

namespace AssetManagement.Tests.Domain.Entities.ServiceCases;

[TestClass]
public sealed class ServiceCaseSyncTests
{
    private static readonly DateTime Now = new(2026, 11, 17, 8, 0, 0, DateTimeKind.Utc);

    [TestMethod]
    public void Create_InitializesSyncState()
    {
        var serviceCase = CreateCase();

        Assert.AreEqual(ServiceDeskSyncStatus.NotStarted, serviceCase.ServiceDeskSyncStatus);
        Assert.IsNull(serviceCase.ServiceDeskRequestId);
        Assert.IsNull(serviceCase.ServiceDeskOperationId);
        Assert.IsNull(serviceCase.ServiceDeskSyncStartedAtUtc);
        Assert.IsNull(serviceCase.ServiceDeskSyncCompletedAtUtc);
        Assert.IsNull(serviceCase.ServiceDeskSyncError);
    }

    [TestMethod]
    public void SetServiceDeskRequestId_TrimsAndStoresValue()
    {
        var serviceCase = CreateCase();

        serviceCase.SetServiceDeskRequestId("  12345  ");

        Assert.AreEqual("12345", serviceCase.ServiceDeskRequestId);
    }

    [TestMethod]
    [DataRow(null)]
    [DataRow("")]
    [DataRow(" ")]
    [DataRow("\t\r\n")]
    public void SetServiceDeskRequestId_WithInvalidValue_Throws(string? requestId)
    {
        var serviceCase = CreateCase();
        var exception = Assert.ThrowsExactly<ArgumentException>(() => serviceCase.SetServiceDeskRequestId(requestId!));

        Assert.AreEqual("requestId", exception.ParamName);
        Assert.IsNull(serviceCase.ServiceDeskRequestId);
    }

    [TestMethod]
    public void SetServiceDeskRequestId_WhenAlreadyAssigned_Throws()
    {
        var serviceCase = CreateCase();
        serviceCase.SetServiceDeskRequestId("12345");

        Assert.ThrowsExactly<InvalidOperationException>(() => serviceCase.SetServiceDeskRequestId("67890"));
        Assert.AreEqual("12345", serviceCase.ServiceDeskRequestId);
    }

    [TestMethod]
    public void BeginServiceDeskSync_SetsPendingState()
    {
        var serviceCase = CreateCase();

        serviceCase.BeginServiceDeskSync(Now);

        Assert.AreEqual(ServiceDeskSyncStatus.Pending, serviceCase.ServiceDeskSyncStatus);
        Assert.IsTrue(serviceCase.ServiceDeskOperationId.HasValue);
        Assert.AreNotEqual(Guid.Empty, serviceCase.ServiceDeskOperationId!.Value);
        Assert.AreEqual(Now, serviceCase.ServiceDeskSyncStartedAtUtc);
        Assert.IsNull(serviceCase.ServiceDeskSyncCompletedAtUtc);
        Assert.IsNull(serviceCase.ServiceDeskSyncError);
    }

    [TestMethod]
    public void BeginServiceDeskSync_WithExistingRequest_Throws()
    {
        var serviceCase = CreateCase();
        serviceCase.SetServiceDeskRequestId("12345");

        Assert.ThrowsExactly<InvalidOperationException>(() => serviceCase.BeginServiceDeskSync(Now));
        Assert.AreEqual(ServiceDeskSyncStatus.NotStarted, serviceCase.ServiceDeskSyncStatus);
        Assert.IsNull(serviceCase.ServiceDeskOperationId);
    }

    [TestMethod]
    public void CompleteServiceDeskSync_SetsCompletedState()
    {
        var serviceCase = CreateCase();
        serviceCase.BeginServiceDeskSync(Now);
        var operationId = serviceCase.ServiceDeskOperationId!.Value;
        var completedAt = Now.AddMinutes(1);

        serviceCase.CompleteServiceDeskSync(
            operationId, "  12345  ", completedAt);

        Assert.AreEqual(ServiceDeskSyncStatus.Completed, serviceCase.ServiceDeskSyncStatus);
        Assert.AreEqual("12345", serviceCase.ServiceDeskRequestId);
        Assert.AreEqual(operationId, serviceCase.ServiceDeskOperationId);
        Assert.AreEqual(Now, serviceCase.ServiceDeskSyncStartedAtUtc);
        Assert.AreEqual(completedAt, serviceCase.ServiceDeskSyncCompletedAtUtc);
        Assert.IsNull(serviceCase.ServiceDeskSyncError);
    }

    [TestMethod]
    [DataRow(null)]
    [DataRow("")]
    [DataRow(" ")]
    [DataRow("\t\r\n")]
    public void CompleteServiceDeskSync_WithInvalidRequestId_Throws(string? requestId)
    {
        var serviceCase = CreateCase();
        serviceCase.BeginServiceDeskSync(Now);

        var exception = Assert.ThrowsExactly<ArgumentException>(
            () => serviceCase.CompleteServiceDeskSync(
                serviceCase.ServiceDeskOperationId!.Value,
                requestId!,
                Now.AddMinutes(1)));

        Assert.AreEqual("requestId", exception.ParamName);
        Assert.AreEqual(ServiceDeskSyncStatus.Pending, serviceCase.ServiceDeskSyncStatus);
        Assert.IsNull(serviceCase.ServiceDeskRequestId);
        Assert.IsNull(serviceCase.ServiceDeskSyncCompletedAtUtc);
    }

    [TestMethod]
    public void CompleteServiceDeskSync_WithExistingRequest_Throws()
    {
        var serviceCase = CreateCase();
        serviceCase.BeginServiceDeskSync(Now);
        serviceCase.SetServiceDeskRequestId("12345");

        Assert.ThrowsExactly<InvalidOperationException>(
            () => serviceCase.CompleteServiceDeskSync(serviceCase.ServiceDeskOperationId!.Value, "67890", Now));

        Assert.AreEqual("12345", serviceCase.ServiceDeskRequestId);
        Assert.AreEqual(ServiceDeskSyncStatus.Pending, serviceCase.ServiceDeskSyncStatus);
    }

    [TestMethod]
    [DataRow("Complete")]
    [DataRow("Fail")]
    [DataRow("Reconcile")]
    public void SyncResult_WithWrongOperationId_Throws(string operation)
    {
        // Arrange
        var serviceCase = CreateCase();
        serviceCase.BeginServiceDeskSync(Now);

        var expectedOperationId = serviceCase.ServiceDeskOperationId;

        // Act and assert
        foreach (var wrongId in new[] { Guid.Empty, Guid.NewGuid() })
        {
            Assert.ThrowsExactly<InvalidOperationException>(() => ApplyResult(serviceCase, operation, wrongId));
        }

        Assert.AreEqual(expectedOperationId, serviceCase.ServiceDeskOperationId);
        Assert.AreEqual(ServiceDeskSyncStatus.Pending, serviceCase.ServiceDeskSyncStatus);
        Assert.IsNull(serviceCase.ServiceDeskSyncError);
        Assert.IsNull(serviceCase.ServiceDeskRequestId);
    }

    [TestMethod]
    [DataRow("Complete")]
    [DataRow("Fail")]
    [DataRow("Reconcile")]
    public void SyncResult_WhenNotStarted_Throws(string operation)
    {
        var serviceCase = CreateCase();

        Assert.ThrowsExactly<InvalidOperationException>(() => ApplyResult(serviceCase, operation, Guid.NewGuid()));

        Assert.AreEqual(ServiceDeskSyncStatus.NotStarted, serviceCase.ServiceDeskSyncStatus);
    }

    [TestMethod]
    [DataRow("Complete", ServiceDeskSyncStatus.Completed)]
    [DataRow("Fail", ServiceDeskSyncStatus.Failed)]
    [DataRow("Reconcile", ServiceDeskSyncStatus.RequiresReconciliation)]
    public void SyncResult_TransitionsAndRejectsFurtherResults(string operation, ServiceDeskSyncStatus expectedStatus)
    {
        var serviceCase = CreateCase();
        serviceCase.BeginServiceDeskSync(Now);
        var operationId = serviceCase.ServiceDeskOperationId!.Value;

        ApplyResult(serviceCase, operation, operationId);

        Assert.AreEqual(expectedStatus, serviceCase.ServiceDeskSyncStatus);

        if (operation != "Complete")
        {
            Assert.AreEqual("Sync error", serviceCase.ServiceDeskSyncError);
            Assert.IsNull(serviceCase.ServiceDeskSyncCompletedAtUtc);
        }

        foreach (var next in new[] { "Complete", "Fail", "Reconcile" })
        {
            Assert.ThrowsExactly<InvalidOperationException>(() => ApplyResult(serviceCase, next, operationId));
        }

        Assert.AreEqual(expectedStatus, serviceCase.ServiceDeskSyncStatus);
    }

    [TestMethod]
    public void BeginServiceDeskSync_AfterFailure_StartsNewOperation()
    {
        var serviceCase = CreateCase();
        serviceCase.BeginServiceDeskSync(Now);
        var previousId = serviceCase.ServiceDeskOperationId!.Value;
        serviceCase.FailServiceDeskSync(previousId, "Temporary failure");

        var retryAt = Now.AddMinutes(5);
        serviceCase.BeginServiceDeskSync(retryAt);

        Assert.AreEqual(ServiceDeskSyncStatus.Pending, serviceCase.ServiceDeskSyncStatus);
        Assert.AreNotEqual(previousId, serviceCase.ServiceDeskOperationId);
        Assert.AreEqual(retryAt, serviceCase.ServiceDeskSyncStartedAtUtc);
        Assert.IsNull(serviceCase.ServiceDeskSyncError);
        Assert.IsNull(serviceCase.ServiceDeskSyncCompletedAtUtc);
        Assert.ThrowsExactly<InvalidOperationException>(() => serviceCase.CompleteServiceDeskSync(previousId, "12345", retryAt));
    }

    [TestMethod]
    [DataRow("Pending")]
    [DataRow("Complete")]
    [DataRow("Reconcile")]
    public void BeginServiceDeskSync_InBlockedState_Throws(string state)
    {
        var serviceCase = CreateCase();
        serviceCase.BeginServiceDeskSync(Now);
        var operationId = serviceCase.ServiceDeskOperationId!.Value;

        if (state != "Pending")
        {
            ApplyResult(serviceCase, state, operationId);
        }

        var previousStatus = serviceCase.ServiceDeskSyncStatus;

        Assert.ThrowsExactly<InvalidOperationException>(() => serviceCase.BeginServiceDeskSync(Now.AddMinutes(5)));
        Assert.AreEqual(previousStatus, serviceCase.ServiceDeskSyncStatus);
        Assert.AreEqual(operationId, serviceCase.ServiceDeskOperationId);
    }

    private static void ApplyResult(ServiceCase serviceCase, string operation, Guid operationId)
    {
        switch (operation)
        {
            case "Complete":
                serviceCase.CompleteServiceDeskSync(operationId, "12345", Now.AddMinutes(1));
                break;
            case "Fail":
                serviceCase.FailServiceDeskSync(operationId, "Sync error");
                break;
            case "Reconcile":
                serviceCase.RequireServiceDeskReconciliation(operationId, "Sync error");
                break;
            default:
                throw new ArgumentOutOfRangeException(nameof(operation));
        }
    }

    private static ServiceCase CreateCase()
    {
        return ServiceCase.Create(
            "SC-2026-0001",
            Guid.NewGuid(),
            ServiceCaseStatus.UnderCentralInspection,
            ServiceIssueType.DisplayIssue,
            null,
            "Display does not work.",
            null,
            Guid.NewGuid(),
            "Test User",
            Now);
    }
}