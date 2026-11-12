using AssetManagement.Application.Common.Authorization;
using AssetManagement.Application.Common.Interfaces.ServiceDesk;
using AssetManagement.Application.Common.Interfaces.Users;
using AssetManagement.Application.ServiceDesk.Exceptions;

namespace AssetManagement.Application.ServiceDesk.RequestServiceDeskNoteRetry;

public sealed class RequestServiceDeskNoteRetryHandler
{
    private const int MaximumCommentLength = 1000;

    private readonly IServiceDeskNoteOutboxRepository _noteOutboxRepository;
    private readonly ICurrentApplicationUserResolver _currentApplicationUserResolver;

    public RequestServiceDeskNoteRetryHandler(
        IServiceDeskNoteOutboxRepository noteOutboxRepository,
        ICurrentApplicationUserResolver currentApplicationUserResolver)
    {
        ArgumentNullException.ThrowIfNull(noteOutboxRepository);
        ArgumentNullException.ThrowIfNull(currentApplicationUserResolver);

        _noteOutboxRepository = noteOutboxRepository;
        _currentApplicationUserResolver = currentApplicationUserResolver;
    }

    public async Task<RequestServiceDeskNoteRetryResponse> HandleAsync(RequestServiceDeskNoteRetryRequest request, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(request);
        cancellationToken.ThrowIfCancellationRequested();

        var currentUser = await _currentApplicationUserResolver.ResolveAsync(cancellationToken);

        ServiceCaseAuthorization.EnsureCanManageAll(currentUser);

        ValidateRequest(request);

        var requestedAtUtc = DateTime.UtcNow;

        var updated = await _noteOutboxRepository.TryRequestRetryAsync(
            outboxId: request.OutboxId,
            operationId: request.ProcessingOperationId,
            requestedByUserId: currentUser.Id,
            requestedByUserName: currentUser.DisplayName,
            requestedAtUtc: requestedAtUtc,
            comment: request.Comment.Trim(),
            cancellationToken: cancellationToken);

        if (!updated)
        {
            throw new ServiceDeskReconciliationConflictException(
                "The note retry could not be requested. " +
                "The entry must be Failed, have no external note identifier, " +
                "and have a matching failed manual reconciliation audit " +
                "for the supplied processing operation identifier. " +
                "The entry may also have changed since it was retrieved.");
        }

        return new RequestServiceDeskNoteRetryResponse(OutboxId: request.OutboxId, RequestedAtUtc: requestedAtUtc);
    }

    private static void ValidateRequest(RequestServiceDeskNoteRetryRequest request)
    {
        if (request.OutboxId == Guid.Empty)
        {
            throw new ArgumentException("The outbox identifier cannot be empty.", nameof(request.OutboxId));
        }

        if (request.ProcessingOperationId == Guid.Empty)
        {
            throw new ArgumentException("The processing operation identifier cannot be empty.", nameof(request.ProcessingOperationId));
        }

        if (string.IsNullOrWhiteSpace(request.Comment))
        {
            throw new ArgumentException("A retry comment is required.", nameof(request.Comment));
        }

        if (request.Comment.Trim().Length > MaximumCommentLength)
        {
            throw new ArgumentException($"The retry comment cannot exceed {MaximumCommentLength} characters.", nameof(request.Comment));
        }
    }
}