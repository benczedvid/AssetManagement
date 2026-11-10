using AssetManagement.Application.Common.Authorization;
using AssetManagement.Application.Common.Interfaces.ServiceCases;
using AssetManagement.Application.Common.Interfaces.Users;
using AssetManagement.Application.ServiceCases.Exceptions;
using AssetManagement.Domain.Entities.ServiceCases;
using Microsoft.Extensions.Logging;

namespace AssetManagement.Application.ServiceDesk.ReconcileServiceDeskRequest;

public sealed class ReconcileServiceDeskRequestHandler
{
    private readonly IServiceCaseRepository _serviceCaseRepository;
    private readonly ICurrentApplicationUserResolver _currentApplicationUserResolver;
    private readonly ILogger<ReconcileServiceDeskRequestHandler> _logger;

    public ReconcileServiceDeskRequestHandler(
        IServiceCaseRepository serviceCaseRepository,
        ICurrentApplicationUserResolver currentApplicationUserResolver,
        ILogger<ReconcileServiceDeskRequestHandler> logger)
    {
        ArgumentNullException.ThrowIfNull(serviceCaseRepository);
        ArgumentNullException.ThrowIfNull(currentApplicationUserResolver);
        ArgumentNullException.ThrowIfNull(logger);

        _serviceCaseRepository = serviceCaseRepository;
        _currentApplicationUserResolver = currentApplicationUserResolver;
        _logger = logger;
    }

    /// <summary>
    /// Reconciles a ServiceDesk synchronization operation
    /// after its external result has been verified.
    /// </summary>
    /// <remarks>
    /// This operation never creates a new ServiceDesk request.
    ///
    /// It can only resolve a synchronization operation
    /// that requires reconciliation.
    ///
    /// The caller must provide the result of an independently
    /// verified external ServiceDesk request lookup.
    ///
    /// Only users with permission to manage all service cases
    /// can perform this operation.
    /// </remarks>
    public async Task<ReconcileServiceDeskResponse> HandleAsync(ReconcileServiceDeskRequest request, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(request);

        ValidateRequest(request);

        cancellationToken.ThrowIfCancellationRequested();

        var currentUser = await _currentApplicationUserResolver.ResolveAsync(cancellationToken);

        ServiceCaseAuthorization.EnsureCanManageAll(currentUser);

        var serviceCase = await _serviceCaseRepository.GetByIdAsync(request.ServiceCaseId, cancellationToken);

        if (serviceCase is null)
        {
            throw new ServiceCaseNotFoundException(request.ServiceCaseId);
        }

        if (serviceCase.ServiceDeskSyncStatus != ServiceDeskSyncStatus.RequiresReconciliation)
        {
            throw new InvalidOperationException("The service case does not require ServiceDesk synchronization reconciliation.");
        }

        if (serviceCase.ServiceDeskOperationId != request.OperationId)
        {
            throw new InvalidOperationException("The ServiceDesk operation identifier does not match.");
        }

        if (!string.IsNullOrWhiteSpace(serviceCase.ServiceDeskRequestId))
        {
            throw new InvalidOperationException("The service case already has a ServiceDesk request.");
        }

        if (request.RequestExists)
        {
            return await CompleteReconciliationAsync(request, cancellationToken);
        }

        return await FailReconciliationAsync(request, cancellationToken);
    }

    private async Task<ReconcileServiceDeskResponse>CompleteReconciliationAsync(ReconcileServiceDeskRequest request, CancellationToken cancellationToken)
    {
        var requestId = request.ServiceDeskRequestId!.Trim();
        var completedAtUtc = DateTime.UtcNow;

        var updated = await _serviceCaseRepository
                .TryCompleteServiceDeskReconciliationAsync(
                    request.ServiceCaseId,
                    request.OperationId,
                    requestId,
                    completedAtUtc,
                    cancellationToken);

        if (!updated)
        {
            throw new InvalidOperationException("The ServiceDesk reconciliation could not be completed. The synchronization state may have changed.");
        }

        _logger.LogInformation(
            "ServiceDesk reconciliation completed. " +
            "ServiceCaseId: {ServiceCaseId}, " +
            "OperationId: {OperationId}, " +
            "ServiceDeskRequestId: {ServiceDeskRequestId}, " +
            "Comment: {Comment}",
            request.ServiceCaseId,
            request.OperationId,
            requestId,
            request.Comment);

        return new ReconcileServiceDeskResponse(
            ServiceCaseId: request.ServiceCaseId,
            OperationId: request.OperationId,
            Status: ServiceDeskSyncStatus.Completed,
            ServiceDeskRequestId: requestId,
            CompletedAtUtc: completedAtUtc);
    }

    private async Task<ReconcileServiceDeskResponse>FailReconciliationAsync(ReconcileServiceDeskRequest request, CancellationToken cancellationToken)
    {
        var reason = string.IsNullOrWhiteSpace(request.Comment)
            ? "The external ServiceDesk request was confirmed not to exist."
            : request.Comment.Trim();

        var updated = await _serviceCaseRepository
                .TryFailServiceDeskReconciliationAsync(
                    request.ServiceCaseId,
                    request.OperationId,
                    reason,
                    cancellationToken);

        if (!updated)
        {
            throw new InvalidOperationException("The ServiceDesk reconciliation could not be completed. The synchronization state may have changed.");
        }

        _logger.LogInformation(
            "ServiceDesk reconciliation marked as failed. " +
            "ServiceCaseId: {ServiceCaseId}, " +
            "OperationId: {OperationId}, " +
            "Comment: {Comment}",
            request.ServiceCaseId,
            request.OperationId,
            reason);

        return new ReconcileServiceDeskResponse(
            ServiceCaseId: request.ServiceCaseId,
            OperationId: request.OperationId,
            Status: ServiceDeskSyncStatus.Failed,
            ServiceDeskRequestId: null,
            CompletedAtUtc: null);
    }

    private static void ValidateRequest(ReconcileServiceDeskRequest request)
    {
        if (request.ServiceCaseId == Guid.Empty)
        {
            throw new ArgumentException("The service case identifier cannot be empty.", nameof(request.ServiceCaseId));
        }

        if (request.OperationId == Guid.Empty)
        {
            throw new ArgumentException("The ServiceDesk operation identifier cannot be empty.", nameof(request.OperationId));
        }

        if (request.RequestExists)
        {
            if (string.IsNullOrWhiteSpace(request.ServiceDeskRequestId))
            {
                throw new ArgumentException("The ServiceDesk request identifier is required when the external request exists.",nameof(request.ServiceDeskRequestId));
            }

            if (request.ServiceDeskRequestId.Trim().Length > 100)
            {
                throw new ArgumentException("The ServiceDesk request identifier cannot exceed 100 characters.", nameof(request.ServiceDeskRequestId));
            }
        }
        else if (!string.IsNullOrWhiteSpace(request.ServiceDeskRequestId))
        {
            throw new ArgumentException("The ServiceDesk request identifier must be empty when the external request does not exist.", nameof(request.ServiceDeskRequestId));
        }

        if (request.Comment is not null && request.Comment.Trim().Length > 1000)
        {
            throw new ArgumentException("The reconciliation comment cannot exceed 1000 characters.", nameof(request.Comment));
        }
    }
}