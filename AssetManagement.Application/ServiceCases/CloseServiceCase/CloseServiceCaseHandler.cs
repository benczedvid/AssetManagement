using AssetManagement.Application.Common.Authorization;
using AssetManagement.Application.Common.Interfaces.Assets;
using AssetManagement.Application.Common.Interfaces.ServiceCases;
using AssetManagement.Application.Common.Interfaces.ServiceDesk;
using AssetManagement.Application.Common.Interfaces.Users;
using AssetManagement.Application.ServiceCases.Exceptions;
using AssetManagement.Domain.Entities.ServiceCases;
using AssetManagement.Domain.Entities.ServiceDesk;

namespace AssetManagement.Application.ServiceCases.CloseServiceCase;

public sealed class CloseServiceCaseHandler
{
    private readonly IServiceCaseRepository _serviceCaseRepository;
    private readonly IServiceDeskRequestClosureOutboxRepository _closureOutboxRepository;
    private readonly ICurrentApplicationUserResolver _currentApplicationUserResolver;
    private readonly IAssetOperationExecutor _assetOperationExecutor;

    public CloseServiceCaseHandler(
        IServiceCaseRepository serviceCaseRepository,
        IServiceDeskRequestClosureOutboxRepository closureOutboxRepository,
        ICurrentApplicationUserResolver currentApplicationUserResolver,
        IAssetOperationExecutor assetOperationExecutor)
    {
        ArgumentNullException.ThrowIfNull(serviceCaseRepository);
        ArgumentNullException.ThrowIfNull(closureOutboxRepository);
        ArgumentNullException.ThrowIfNull(currentApplicationUserResolver);
        ArgumentNullException.ThrowIfNull(assetOperationExecutor);

        _serviceCaseRepository = serviceCaseRepository;
        _closureOutboxRepository = closureOutboxRepository;
        _currentApplicationUserResolver = currentApplicationUserResolver;
        _assetOperationExecutor = assetOperationExecutor;
    }

    /// <summary>
    /// Closes a resolved service case within a transaction protected
    /// by an exclusive lock for the associated asset.
    /// </summary>
    /// <remarks>
    /// A preliminary no-tracking lookup identifies the asset to lock.
    /// The service case is reloaded after acquiring the asset lock.
    ///
    /// Authorization, state validation, closure, event creation,
    /// ServiceDesk task outbox creation, and external request
    /// closure outbox creation occur inside the protected transaction.
    ///
    /// External ServiceDesk API calls are not executed
    /// inside the database transaction.
    /// </remarks>
    public async Task<CloseServiceCaseResponse> HandleAsync(
        Guid serviceCaseId,
        CloseServiceCaseRequest request,
        CancellationToken cancellationToken = default)
    {
        ValidateServiceCaseId(serviceCaseId);
        ArgumentNullException.ThrowIfNull(request);
        ValidateComment(request.Comment);

        // Preliminary lookup must remain no-tracking.
        var serviceCaseSnapshot = await _serviceCaseRepository.GetByIdAsync(
                serviceCaseId,
                cancellationToken)
            ?? throw new ServiceCaseNotFoundException(serviceCaseId);

        var assetId = serviceCaseSnapshot.AssetId;

        if (assetId == Guid.Empty)
        {
            throw new ServiceCaseStateConflictException(serviceCaseId, "The service case does not reference a valid asset.");
        }

        return await _assetOperationExecutor.ExecuteAsync<CloseServiceCaseResponse>(
                assetIds: new[] { assetId },
                operation: token => HandleCoreAsync(
                    serviceCaseId,
                    assetId,
                    request,
                    token),
                cancellationToken: cancellationToken);
    }

    private async Task<CloseServiceCaseResponse> HandleCoreAsync(
        Guid serviceCaseId,
        Guid lockedAssetId,
        CloseServiceCaseRequest request,
        CancellationToken cancellationToken = default)
    {
        ValidateServiceCaseId(serviceCaseId);
        ArgumentNullException.ThrowIfNull(request);
        ValidateComment(request.Comment);

        var currentUser = await _currentApplicationUserResolver.ResolveAsync(cancellationToken);

        ServiceCaseAuthorization.EnsureCanClose(currentUser);

        var serviceCase = await _serviceCaseRepository.GetForUpdateByIdAsync(serviceCaseId, cancellationToken)
            ?? throw new ServiceCaseNotFoundException(serviceCaseId);

        if (serviceCase.AssetId != lockedAssetId)
        {
            throw new ServiceCaseStateConflictException(serviceCaseId,
                "The asset associated with the service case changed before the operation acquired its lock.");
        }

        if (serviceCase.Status != ServiceCaseStatus.Resolved)
        {
            throw new ServiceCaseStateConflictException(serviceCaseId,
                "Only a resolved service case can be closed.");
        }

        var previousStatus = serviceCase.Status;
        var closedAtUtc = DateTime.UtcNow;

        // Close the local service case.
        serviceCase.Close(
            closedByUserId: currentUser.Id,
            closedAtUtc: closedAtUtc);

        // Create the local service case closure event.
        var serviceCaseEvent = ServiceCaseEvent.Create(
            serviceCaseId: serviceCase.Id,
            type: ServiceCaseEventType.Closed,
            previousStatus: previousStatus,
            newStatus: serviceCase.Status,
            comment: request.Comment,
            performedByUserId: currentUser.Id,
            performedByUserName: currentUser.DisplayName,
            occurredAtUtc: closedAtUtc);

        // Register the service case closure event.
        await _serviceCaseRepository.AddEventAsync(serviceCaseEvent, cancellationToken);

        // Register the external ServiceDesk request closure operation.
        // No new task is created for the Closed event.
        var closureOutbox = ServiceDeskRequestClosureOutbox.Create(
            serviceCaseId: serviceCase.Id,
            serviceCaseEventId: serviceCaseEvent.Id,
            createdAtUtc: closedAtUtc);

        await _closureOutboxRepository.AddAsync(closureOutbox, cancellationToken);

        // Persist the service case, closure event, and request closure outbox.
        await _serviceCaseRepository.SaveChangesAsync(cancellationToken);

        return new CloseServiceCaseResponse(
            ServiceCaseId: serviceCase.Id,
            CaseNumber: serviceCase.CaseNumber,
            AssetId: serviceCase.AssetId,
            ServiceCaseStatus: serviceCase.Status,
            ClosedAtUtc: closedAtUtc,
            ClosedByUserId: currentUser.Id,
            ClosedByUserName: currentUser.DisplayName);
    }

    private static void ValidateServiceCaseId(Guid serviceCaseId)
    {
        if (serviceCaseId == Guid.Empty)
        {
            throw new ArgumentException("The service case identifier cannot be empty.", nameof(serviceCaseId));
        }
    }

    private static void ValidateComment(string? comment)
    {
        if (comment is null)
        {
            return;
        }

        if (string.IsNullOrWhiteSpace(comment))
        {
            throw new ArgumentException("Comment cannot be empty or whitespace.", nameof(comment));
        }

        if (comment.Length > 2000)
        {
            throw new ArgumentException("Comment cannot exceed 2000 characters.", nameof(comment));
        }
    }
}