using AssetManagement.Application.Common.Authorization;
using AssetManagement.Application.Common.Interfaces.Assets;
using AssetManagement.Application.Common.Interfaces.ServiceCases;
using AssetManagement.Application.Common.Interfaces.Users;
using AssetManagement.Application.ServiceCases.Exceptions;
using AssetManagement.Domain.Entities.ServiceCases;

namespace AssetManagement.Application.ServiceCases.CloseServiceCase;

public sealed class CloseServiceCaseHandler
{
    private readonly IServiceCaseRepository _serviceCaseRepository;
    private readonly ICurrentApplicationUserResolver _currentApplicationUserResolver;
    private readonly IAssetOperationExecutor _assetOperationExecutor;

    public CloseServiceCaseHandler(
        IServiceCaseRepository serviceCaseRepository,
        ICurrentApplicationUserResolver currentApplicationUserResolver,
        IAssetOperationExecutor assetOperationExecutor)
    {
        ArgumentNullException.ThrowIfNull(serviceCaseRepository);
        ArgumentNullException.ThrowIfNull(currentApplicationUserResolver);
        ArgumentNullException.ThrowIfNull(assetOperationExecutor);

        _serviceCaseRepository = serviceCaseRepository;
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
    /// and persistence occur inside the protected transaction.
    /// </remarks>
    /// <param name="serviceCaseId">
    /// The identifier of the service case.
    /// </param>
    /// <param name="request">
    /// The service case closure request.
    /// </param>
    /// <param name="cancellationToken">
    /// A token used to cancel lookup, lock acquisition, and execution.
    /// </param>
    /// <returns>
    /// The closure result after the transaction commits.
    /// </returns>
    /// <exception cref="ArgumentNullException">
    /// Thrown when the request is null.
    /// </exception>
    /// <exception cref="ArgumentException">
    /// Thrown when the service case identifier or comment is invalid.
    /// </exception>
    /// <exception cref="ServiceCaseNotFoundException">
    /// Thrown when the service case cannot be found.
    /// </exception>
    /// <exception cref="ServiceCaseStateConflictException">
    /// Thrown when the service case relationship or state is invalid.
    /// </exception>
    /// <exception cref="TimeoutException">
    /// Thrown when the asset lock cannot be acquired within the timeout.
    /// </exception>
    public async Task<CloseServiceCaseResponse> HandleAsync(
        Guid serviceCaseId,
        CloseServiceCaseRequest request,
        CancellationToken cancellationToken = default)
    {
        ValidateServiceCaseId(serviceCaseId);
        ArgumentNullException.ThrowIfNull(request);
        ValidateComment(request.Comment);

        var serviceCaseSnapshot =
            await _serviceCaseRepository.GetByIdAsync(
                serviceCaseId,
                cancellationToken)
            ?? throw new ServiceCaseNotFoundException(serviceCaseId);

        var assetId = serviceCaseSnapshot.AssetId;

        if (assetId == Guid.Empty)
        {
            throw new ServiceCaseStateConflictException(
                serviceCaseId,
                "The service case does not reference a valid asset.");
        }

        return await _assetOperationExecutor
            .ExecuteAsync<CloseServiceCaseResponse>(
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

        var serviceCase =
            await _serviceCaseRepository.GetForUpdateByIdAsync(
                serviceCaseId,
                cancellationToken)
            ?? throw new ServiceCaseNotFoundException(serviceCaseId);

        if (serviceCase.AssetId != lockedAssetId)
        {
            throw new ServiceCaseStateConflictException(
                serviceCaseId,
                "The asset associated with the service case changed " +
                "before the operation acquired its lock.");
        }

        if (serviceCase.Status != ServiceCaseStatus.Resolved)
        {
            throw new ServiceCaseStateConflictException(
                serviceCase.Id,
                "Only a resolved service case can be closed.");
        }

        var previousStatus = serviceCase.Status;
        var closedAtUtc = DateTime.UtcNow;

        serviceCase.Close(closedByUserId: currentUser.Id, closedAtUtc: closedAtUtc);

        var serviceCaseEvent = ServiceCaseEvent.Create(
            serviceCaseId: serviceCase.Id,
            type: ServiceCaseEventType.Closed,
            previousStatus: previousStatus,
            newStatus: serviceCase.Status,
            comment: request.Comment,
            performedByUserId: currentUser.Id,
            performedByUserName: currentUser.DisplayName,
            occurredAtUtc: closedAtUtc);

        var response = new CloseServiceCaseResponse(
            ServiceCaseId: serviceCase.Id,
            CaseNumber: serviceCase.CaseNumber,
            AssetId: serviceCase.AssetId,
            ServiceCaseStatus: serviceCase.Status,
            ClosedAtUtc: closedAtUtc,
            ClosedByUserId: currentUser.Id,
            ClosedByUserName: currentUser.DisplayName);

        await _serviceCaseRepository.AddEventAsync(serviceCaseEvent, cancellationToken);
        await _serviceCaseRepository.SaveChangesAsync(cancellationToken);

        return response;
    }

    private static void ValidateServiceCaseId(Guid serviceCaseId)
    {
        if (serviceCaseId == Guid.Empty)
        {
            throw new ArgumentException("The service case identifier cannot be empty.",
                nameof(serviceCaseId));
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