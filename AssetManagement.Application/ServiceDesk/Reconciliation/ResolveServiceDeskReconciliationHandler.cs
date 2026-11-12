using AssetManagement.Application.Common.Authorization;
using AssetManagement.Application.Common.Interfaces.ServiceDesk;
using AssetManagement.Application.Common.Interfaces.Users;
using AssetManagement.Application.ServiceDesk.Exceptions;

namespace AssetManagement.Application.ServiceDesk.Reconciliation;

/// <summary>
/// Handles administrative reconciliation of uncertain
/// ServiceDesk request creation and closure operations.
/// </summary>
/// <remarks>
/// Only users authorized to manage all service cases
/// can perform reconciliation.
///
/// External verification must be completed before
/// invoking this handler.
///
/// This handler never creates or closes an external
/// ServiceDesk request.
/// </remarks>
public sealed class ResolveServiceDeskReconciliationHandler
{
    private const int MaximumRequestIdLength = 100;
    private const int MaximumCommentLength = 1000;
    private const int MaximumNoteIdLength = 100;

    private readonly IServiceDeskRequestOutboxRepository _requestOutboxRepository;
    private readonly IServiceDeskRequestClosureOutboxRepository _closureOutboxRepository;
    private readonly ICurrentApplicationUserResolver _currentApplicationUserResolver;
    private readonly IServiceDeskNoteOutboxRepository _noteOutboxRepository;

    public ResolveServiceDeskReconciliationHandler(
        IServiceDeskRequestOutboxRepository requestOutboxRepository,
        IServiceDeskRequestClosureOutboxRepository closureOutboxRepository,
        ICurrentApplicationUserResolver currentApplicationUserResolver,
        IServiceDeskNoteOutboxRepository serviceDeskNoteOutboxRepository)
    {
        ArgumentNullException.ThrowIfNull(requestOutboxRepository);
        ArgumentNullException.ThrowIfNull(closureOutboxRepository);
        ArgumentNullException.ThrowIfNull(currentApplicationUserResolver);
        ArgumentNullException.ThrowIfNull(serviceDeskNoteOutboxRepository);

        _requestOutboxRepository = requestOutboxRepository;
        _closureOutboxRepository = closureOutboxRepository;
        _currentApplicationUserResolver = currentApplicationUserResolver;
        _noteOutboxRepository = serviceDeskNoteOutboxRepository;
    }

    /// <summary>
    /// Resolves an uncertain ServiceDesk synchronization operation
    /// using an independently verified external result.
    /// </summary>
    /// <param name="request">
    /// The reconciliation operation, outcome, identifiers,
    /// and verification comment.
    /// </param>
    /// <param name="cancellationToken">
    /// Cancellation token.
    /// </param>
    /// <returns>
    /// The completed reconciliation result.
    /// </returns>
    public async Task<ResolveServiceDeskReconciliationResponse> HandleAsync(
        ResolveServiceDeskReconciliationRequest request,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(request);

        ValidateRequest(request);

        cancellationToken.ThrowIfCancellationRequested();

        var currentUser = await _currentApplicationUserResolver.ResolveAsync(cancellationToken);

        ServiceCaseAuthorization.EnsureCanManageAll(currentUser);

        var verifiedAtUtc = DateTime.UtcNow;

        var updated = (request.Operation, request.Outcome) switch
        {
            (ServiceDeskReconciliationOperation.RequestCreation,  ServiceDeskReconciliationOutcome.Completed) => await _requestOutboxRepository
                .TryCompleteReconciliationAsync(
                    request.OutboxId,
                    request.ProcessingOperationId,
                    request.ServiceDeskRequestId!,
                    currentUser.Id,
                    currentUser.DisplayName,
                    verifiedAtUtc,
                    request.VerificationComment,
                    cancellationToken),

            (ServiceDeskReconciliationOperation.RequestCreation, ServiceDeskReconciliationOutcome.Failed) => await _requestOutboxRepository
                .TryFailReconciliationAsync(
                    request.OutboxId,
                    request.ProcessingOperationId,
                    currentUser.Id,
                    currentUser.DisplayName,
                    verifiedAtUtc,
                    request.VerificationComment,
                    cancellationToken),

            (ServiceDeskReconciliationOperation.RequestClosure, ServiceDeskReconciliationOutcome.Completed) => await _closureOutboxRepository
                .TryCompleteReconciliationAsync(
                    request.OutboxId,
                    request.ProcessingOperationId,
                    currentUser.Id,
                    currentUser.DisplayName,
                    verifiedAtUtc,
                    request.VerificationComment,
                    cancellationToken),

            (ServiceDeskReconciliationOperation.RequestClosure, ServiceDeskReconciliationOutcome.Failed) => await _closureOutboxRepository
                .TryFailReconciliationAsync(
                    request.OutboxId,
                    request.ProcessingOperationId,
                    currentUser.Id,
                    currentUser.DisplayName,
                    verifiedAtUtc,
                    request.VerificationComment,
                    cancellationToken),

            (ServiceDeskReconciliationOperation.NoteCreation, ServiceDeskReconciliationOutcome.Completed) => await _noteOutboxRepository
                .TryCompleteReconciliationAsync(
                    request.OutboxId,
                    request.ProcessingOperationId,
                    request.ServiceDeskNoteId!,
                    currentUser.Id,
                    currentUser.DisplayName,
                    verifiedAtUtc,
                    request.VerificationComment,
                    cancellationToken),

            (ServiceDeskReconciliationOperation.NoteCreation, ServiceDeskReconciliationOutcome.Failed) => await _noteOutboxRepository
                .TryFailReconciliationAsync(
                    request.OutboxId,
                    request.ProcessingOperationId,
                    currentUser.Id,
                    currentUser.DisplayName,
                    verifiedAtUtc,
                    request.VerificationComment,
                    cancellationToken),

            _ => throw new ArgumentOutOfRangeException(nameof(request), "The reconciliation operation or outcome is invalid.")
        };

        if (!updated)
        {
            throw new ServiceDeskReconciliationConflictException(
                "The reconciliation could not be completed. The outbox state or processing operation identifier has changed.");
        }

        return new ResolveServiceDeskReconciliationResponse(
            request.Operation,
            request.OutboxId,
            request.Outcome,
            verifiedAtUtc);
    }

    /// <summary>
    /// Validates the reconciliation request.
    /// </summary>
    private static void ValidateRequest(ResolveServiceDeskReconciliationRequest request)
    {
        if (request.OutboxId == Guid.Empty)
        {
            throw new ArgumentException("The outbox identifier cannot be empty.", nameof(request.OutboxId));
        }

        if (request.ProcessingOperationId == Guid.Empty)
        {
            throw new ArgumentException("The processing operation identifier cannot be empty.",  nameof(request.ProcessingOperationId));
        }

        if (string.IsNullOrWhiteSpace(request.VerificationComment))
        {
            throw new ArgumentException("A verification comment is required.", nameof(request.VerificationComment));
        }

        if (request.VerificationComment.Trim().Length > MaximumCommentLength)
        {
            throw new ArgumentException(
                $"The verification comment cannot exceed {MaximumCommentLength} characters.", nameof(request.VerificationComment));
        }

        if (request.ServiceDeskRequestId is not null && request.ServiceDeskRequestId.Trim().Length > MaximumRequestIdLength)
        {
            throw new ArgumentException(
                $"The ServiceDesk request identifier cannot exceed {MaximumRequestIdLength} characters.", nameof(request.ServiceDeskRequestId));
        }

        if (request.Operation == ServiceDeskReconciliationOperation.RequestCreation &&
            request.Outcome == ServiceDeskReconciliationOutcome.Completed &&
            string.IsNullOrWhiteSpace(request.ServiceDeskRequestId))
        {
            throw new ArgumentException("A completed request creation requires the verified ServiceDesk request identifier.",
                nameof(request.ServiceDeskRequestId));
        }

        if (request.Outcome == ServiceDeskReconciliationOutcome.Failed && !string.IsNullOrWhiteSpace(request.ServiceDeskRequestId))
        {
            throw new ArgumentException("The ServiceDesk request identifier must be empty when reconciliation is marked as Failed.", nameof(request.ServiceDeskRequestId));
        }

        if (request.Operation == ServiceDeskReconciliationOperation.RequestClosure && !string.IsNullOrWhiteSpace(request.ServiceDeskRequestId))
        {
            throw new ArgumentException("The ServiceDesk request identifier must not be provided for request closure reconciliation.",
                nameof(request.ServiceDeskRequestId));
        }

        if (!Enum.IsDefined(request.Operation))
        {
            throw new ArgumentOutOfRangeException(nameof(request.Operation),
                "The reconciliation operation is invalid.");
        }

        if (!Enum.IsDefined(request.Outcome))
        {
            throw new ArgumentOutOfRangeException(nameof(request.Outcome), "The reconciliation outcome is invalid.");
        }

        if (request.ServiceDeskNoteId is not null && request.ServiceDeskNoteId.Trim().Length > MaximumNoteIdLength)
        {
            throw new ArgumentException($"The ServiceDesk note identifier cannot exceed {MaximumNoteIdLength} characters.",
                nameof(request.ServiceDeskNoteId));
        }

        if (request.Operation == ServiceDeskReconciliationOperation.NoteCreation)
        {
            if (!string.IsNullOrWhiteSpace(request.ServiceDeskRequestId))
            {
                throw new ArgumentException("The ServiceDesk request identifier must not be provided for note creation reconciliation.",
                    nameof(request.ServiceDeskRequestId));
            }

            if (request.Outcome == ServiceDeskReconciliationOutcome.Completed &&
                string.IsNullOrWhiteSpace(request.ServiceDeskNoteId))
            {
                throw new ArgumentException("Completed note creation reconciliation requires the verified ServiceDesk note identifier.",
                    nameof(request.ServiceDeskNoteId));
            }

            if (request.Outcome == ServiceDeskReconciliationOutcome.Failed &&
                !string.IsNullOrWhiteSpace(request.ServiceDeskNoteId))
            {
                throw new ArgumentException("The ServiceDesk note identifier must be empty when reconciliation is marked as Failed.",
                    nameof(request.ServiceDeskNoteId));
            }
        }
        else if (!string.IsNullOrWhiteSpace(request.ServiceDeskNoteId))
        {
            throw new ArgumentException("The ServiceDesk note identifier must only be provided for note creation reconciliation.",
                nameof(request.ServiceDeskNoteId));
        }
    }
}