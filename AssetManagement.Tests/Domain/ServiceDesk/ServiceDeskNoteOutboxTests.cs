using System;
using AssetManagement.Domain.Entities.ServiceDesk;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace AssetManagement.Tests.Domain.ServiceDesk;

[TestClass]
public sealed class ServiceDeskNoteOutboxTests
{
    private static readonly Guid CaseId = Guid.NewGuid();
    private static readonly Guid EventId = Guid.NewGuid();
    private static readonly Guid OperationId = Guid.NewGuid();
    private static readonly DateTime CreatedAt = new(2026, 11, 17, 8, 0, 0, DateTimeKind.Utc);
    private static readonly DateTime StartedAt = CreatedAt.AddMinutes(1);
    private static readonly DateTime FinishedAt = CreatedAt.AddMinutes(2);

    [TestMethod]
    public void Create_WithValidArguments_SetsInitialProperties()
    {
        var entry = CreateEntry(description: "  Note description  ");

        Assert.AreNotEqual(Guid.Empty, entry.Id);
        Assert.AreEqual(CaseId, entry.ServiceCaseId);
        Assert.AreEqual(EventId, entry.ServiceCaseEventId);
        Assert.AreEqual("Note description", entry.Description);
        Assert.AreEqual(ServiceDeskNoteOutboxStatus.Pending, entry.Status);
        Assert.AreEqual(CreatedAt, entry.CreatedAtUtc);
        Assert.AreEqual(DateTimeKind.Utc, entry.CreatedAtUtc.Kind);
        Assert.IsNull(entry.ServiceDeskNoteId);
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
            () => ServiceDeskNoteOutbox.Create(
                parameter == "serviceCaseId" ? Guid.Empty : CaseId,
                parameter == "serviceCaseEventId" ? Guid.Empty : EventId,
                "Note description",
                CreatedAt),
            parameter);
    }

    [TestMethod]
    [DataRow(null)]
    [DataRow("")]
    [DataRow(" ")]
    [DataRow("\t\r\n")]
    public void Create_WithInvalidDescription_Throws(string? description)
    {
        AssertArgument(() => CreateEntry(description: description!), "description");
    }

    [TestMethod]
    public void Create_WithNonUtcDate_Throws()
    {
        foreach (var date in InvalidDates())
        {
            AssertArgument(() => CreateEntry(createdAt: date), "createdAtUtc");
        }
    }

    [TestMethod]
    public void BeginProcessing_WithValidArguments_SetsProcessingState()
    {
        var entry = CreateEntry();

        entry.BeginProcessing(OperationId, StartedAt);

        Assert.AreEqual(ServiceDeskNoteOutboxStatus.Processing, entry.Status);
        Assert.AreEqual(OperationId, entry.ProcessingOperationId);
        Assert.AreEqual(StartedAt, entry.ProcessingStartedAtUtc);
        Assert.IsNull(entry.ProcessedAtUtc);
        Assert.IsNull(entry.Error);
        Assert.IsNull(entry.ServiceDeskNoteId);
    }

    [TestMethod]
    public void BeginProcessing_WithEmptyOperationId_ThrowsWithoutMutation()
    {
        var entry = CreateEntry();

        AssertArgument(() => entry.BeginProcessing(Guid.Empty, StartedAt), "operationId");

        Assert.AreEqual(ServiceDeskNoteOutboxStatus.Pending, entry.Status);
        Assert.IsNull(entry.ProcessingOperationId);
        Assert.IsNull(entry.ProcessingStartedAtUtc);
    }

    [TestMethod]
    public void BeginProcessing_WithNonUtcDate_ThrowsWithoutMutation()
    {
        foreach (var date in InvalidDates())
        {
            var entry = CreateEntry();

            AssertArgument(() => entry.BeginProcessing(OperationId, date), "startedAtUtc");

            Assert.AreEqual(ServiceDeskNoteOutboxStatus.Pending, entry.Status);
            Assert.IsNull(entry.ProcessingOperationId);
            Assert.IsNull(entry.ProcessingStartedAtUtc);
        }
    }

    [TestMethod]
    public void BeginProcessing_WhenAlreadyProcessing_Throws()
    {
        var entry = CreateProcessingEntry();

        Assert.ThrowsExactly<InvalidOperationException>(() => entry.BeginProcessing(Guid.NewGuid(), FinishedAt));

        Assert.AreEqual(OperationId, entry.ProcessingOperationId);
        Assert.AreEqual(StartedAt, entry.ProcessingStartedAtUtc);
        Assert.AreEqual(ServiceDeskNoteOutboxStatus.Processing, entry.Status);
    }

    [TestMethod]
    public void Complete_WithValidArguments_SetsCompletedState()
    {
        var entry = CreateProcessingEntry();

        entry.Complete(OperationId, "  NOTE-123  ", FinishedAt);

        Assert.AreEqual(ServiceDeskNoteOutboxStatus.Completed, entry.Status);
        Assert.AreEqual("NOTE-123", entry.ServiceDeskNoteId);
        Assert.AreEqual(FinishedAt, entry.ProcessedAtUtc);
        Assert.AreEqual(OperationId, entry.ProcessingOperationId);
        Assert.AreEqual(StartedAt, entry.ProcessingStartedAtUtc);
        Assert.IsNull(entry.Error);
    }

    [TestMethod]
    public void Complete_WithMaximumTrimmedNoteIdLength_AcceptsValue()
    {
        var entry = CreateProcessingEntry();
        var noteId = new string('N', 100);

        entry.Complete(OperationId, $" {noteId} ", FinishedAt);

        Assert.AreEqual(noteId, entry.ServiceDeskNoteId);
    }

    [TestMethod]
    [DataRow(null)]
    [DataRow("")]
    [DataRow(" ")]
    [DataRow("\t\r\n")]
    public void Complete_WithInvalidNoteId_ThrowsWithoutMutation(string? noteId)
    {
        var entry = CreateProcessingEntry();

        AssertArgument(() => entry.Complete(OperationId, noteId!, FinishedAt), "serviceDeskNoteId");
        AssertStillProcessing(entry);
    }

    [TestMethod]
    public void Complete_WithTooLongNoteId_ThrowsWithoutMutation()
    {
        var entry = CreateProcessingEntry();

        AssertArgument(() => entry.Complete(OperationId, new string('N', 101), FinishedAt), "serviceDeskNoteId");

        AssertStillProcessing(entry);
    }

    [TestMethod]
    [DataRow("Complete", "completedAtUtc")]
    [DataRow("Fail", "failedAtUtc")]
    public void Result_WithNonUtcDate_ThrowsWithoutMutation(string operation, string parameter)
    {
        foreach (var date in InvalidDates())
        {
            var entry = CreateProcessingEntry();

            AssertArgument(() => ApplyResult(entry, operation, OperationId, date: date), parameter);
            AssertStillProcessing(entry);
        }
    }

    [TestMethod]
    [DataRow("Fail", ServiceDeskNoteOutboxStatus.Failed)]
    [DataRow("Reconcile", ServiceDeskNoteOutboxStatus.RequiresReconciliation)]
    public void UnsuccessfulResult_TrimsReasonAndSetsState(string operation, ServiceDeskNoteOutboxStatus expectedStatus)
    {
        var entry = CreateProcessingEntry();

        ApplyResult(entry, operation, OperationId, reason: "  External API error  ");

        Assert.AreEqual(expectedStatus, entry.Status);
        Assert.AreEqual("External API error", entry.Error);
        Assert.AreEqual(OperationId, entry.ProcessingOperationId);
        Assert.AreEqual(StartedAt, entry.ProcessingStartedAtUtc);
        Assert.IsNull(entry.ServiceDeskNoteId);

        if (operation == "Fail")
        {
            Assert.AreEqual(FinishedAt, entry.ProcessedAtUtc);
        }
        else
        {
            Assert.IsNull(entry.ProcessedAtUtc);
        }
    }

    [TestMethod]
    [DataRow("Fail")]
    [DataRow("Reconcile")]
    public void UnsuccessfulResult_WithLongReason_TruncatesAfterTrimming(string operation)
    {
        var entry = CreateProcessingEntry();
        var expectedError = new string('E', 1000);

        ApplyResult(
            entry,
            operation,
            OperationId,
            reason: $"  {expectedError}EXTRA  ");

        Assert.AreEqual(expectedError, entry.Error);
    }

    [TestMethod]
    [DataRow("Fail")]
    [DataRow("Reconcile")]
    public void UnsuccessfulResult_WithInvalidReason_Throws(string operation)
    {
        foreach (var reason in new string?[] { null, "", " ", "\t\r\n" })
        {
            var entry = CreateProcessingEntry();

            AssertArgument(() => ApplyResult(entry, operation, OperationId, reason: reason), "reason");
        }
    }

    [TestMethod]
    [DataRow("Complete")]
    [DataRow("Fail")]
    [DataRow("Reconcile")]
    public void Result_WithEmptyOperationId_Throws(string operation)
    {
        var entry = CreateProcessingEntry();

        AssertArgument(() => ApplyResult(entry, operation, Guid.Empty), "operationId");
        AssertStillProcessing(entry);
    }

    [TestMethod]
    [DataRow("Complete")]
    [DataRow("Fail")]
    [DataRow("Reconcile")]
    public void Result_WithDifferentOperationId_ThrowsWithoutMutation(string operation)
    {
        var entry = CreateProcessingEntry();

        Assert.ThrowsExactly<InvalidOperationException>(() => ApplyResult(entry, operation, Guid.NewGuid()));
        AssertStillProcessing(entry);
    }

    [TestMethod]
    [DataRow("Complete")]
    [DataRow("Fail")]
    [DataRow("Reconcile")]
    public void Result_WhenPending_Throws(string operation)
    {
        var entry = CreateEntry();

        Assert.ThrowsExactly<InvalidOperationException>(() => ApplyResult(entry, operation, OperationId));
        Assert.AreEqual(ServiceDeskNoteOutboxStatus.Pending, entry.Status);
        Assert.IsNull(entry.ProcessedAtUtc);
        Assert.IsNull(entry.Error);
    }

    [TestMethod]
    [DataRow("Complete")]
    [DataRow("Fail")]
    [DataRow("Reconcile")]
    public void TerminalState_RejectsRestartAndFurtherResults(
        string terminalOperation)
    {
        var entry = CreateProcessingEntry();
        ApplyResult(entry, terminalOperation, OperationId);

        var expectedStatus = entry.Status;
        var expectedError = entry.Error;
        var expectedProcessedAt = entry.ProcessedAtUtc;
        var expectedNoteId = entry.ServiceDeskNoteId;

        Assert.ThrowsExactly<InvalidOperationException>(() => entry.BeginProcessing(Guid.NewGuid(), FinishedAt));

        foreach (var operation in new[] { "Complete", "Fail", "Reconcile" })
        {
            Assert.ThrowsExactly<InvalidOperationException>(() => ApplyResult(entry, operation, OperationId));
        }

        Assert.AreEqual(expectedStatus, entry.Status);
        Assert.AreEqual(expectedError, entry.Error);
        Assert.AreEqual(expectedProcessedAt, entry.ProcessedAtUtc);
        Assert.AreEqual(expectedNoteId, entry.ServiceDeskNoteId);
        Assert.AreEqual(OperationId, entry.ProcessingOperationId);
    }

    private static ServiceDeskNoteOutbox CreateEntry(string description = "Note description", DateTime? createdAt = null)
    {
        return ServiceDeskNoteOutbox.Create(CaseId, EventId, description, createdAt ?? CreatedAt);
    }

    private static ServiceDeskNoteOutbox CreateProcessingEntry()
    {
        var entry = CreateEntry();
        entry.BeginProcessing(OperationId, StartedAt);
        return entry;
    }

    private static void ApplyResult(
        ServiceDeskNoteOutbox entry,
        string operation,
        Guid operationId,
        string? reason = "External API error",
        DateTime? date = null)
    {
        switch (operation)
        {
            case "Complete":
                entry.Complete(operationId, "NOTE-123", date ?? FinishedAt);
                break;

            case "Fail":
                entry.Fail(operationId, reason!, date ?? FinishedAt);
                break;

            case "Reconcile":
                entry.RequireReconciliation(operationId, reason!);
                break;

            default:
                throw new ArgumentOutOfRangeException(nameof(operation));
        }
    }

    private static void AssertStillProcessing(ServiceDeskNoteOutbox entry)
    {
        Assert.AreEqual(ServiceDeskNoteOutboxStatus.Processing, entry.Status);
        Assert.AreEqual(OperationId, entry.ProcessingOperationId);
        Assert.AreEqual(StartedAt, entry.ProcessingStartedAtUtc);
        Assert.IsNull(entry.ProcessedAtUtc);
        Assert.IsNull(entry.ServiceDeskNoteId);
        Assert.IsNull(entry.Error);
    }

    private static DateTime[] InvalidDates()
    {
        return
        [
            default,
            DateTime.SpecifyKind(CreatedAt, DateTimeKind.Local),
            DateTime.SpecifyKind(CreatedAt, DateTimeKind.Unspecified)
        ];
    }

    private static void AssertArgument(Action action, string parameter)
    {
        var exception = Assert.ThrowsExactly<ArgumentException>(action);
        Assert.AreEqual(parameter, exception.ParamName);
    }
}