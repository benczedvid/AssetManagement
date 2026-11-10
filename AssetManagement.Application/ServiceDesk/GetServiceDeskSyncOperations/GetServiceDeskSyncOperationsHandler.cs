using AssetManagement.Application.Common.Authorization;
using AssetManagement.Application.Common.Interfaces.ServiceDesk;
using AssetManagement.Application.Common.Interfaces.Users;

namespace AssetManagement.Application.ServiceDesk.GetServiceDeskSyncOperations;

/// <summary>
/// Retrieves ServiceDesk synchronization operations
/// for the administration interface.
/// </summary>
/// <remarks>
/// Only users authorized to manage all service cases
/// can retrieve synchronization operations.
///
/// The handler performs read-only operations.
/// </remarks>
public sealed class GetServiceDeskSyncOperationsHandler
{
    private static readonly HashSet<string> AllowedOperationTypes =
        new(StringComparer.OrdinalIgnoreCase)
        {
            "RequestCreation",
            "TaskCreation",
            "RequestClosure"
        };

    private static readonly HashSet<string> AllowedStatuses =
        new(StringComparer.OrdinalIgnoreCase)
        {
            "Pending",
            "Processing",
            "Completed",
            "Failed",
            "RequiresReconciliation"
        };

    private readonly IServiceDeskSyncOperationsRepository _repository;
    private readonly ICurrentApplicationUserResolver _currentApplicationUserResolver;

    public GetServiceDeskSyncOperationsHandler(
        IServiceDeskSyncOperationsRepository repository,
        ICurrentApplicationUserResolver currentApplicationUserResolver)
    {
        ArgumentNullException.ThrowIfNull(repository);
        ArgumentNullException.ThrowIfNull(currentApplicationUserResolver);

        _repository = repository;
        _currentApplicationUserResolver = currentApplicationUserResolver;
    }

    /// <summary>
    /// Retrieves ServiceDesk synchronization operations
    /// using the supplied filters.
    /// </summary>
    public async Task<GetServiceDeskSyncOperationsResponse> HandleAsync(
        GetServiceDeskSyncOperationsRequest request,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(request);

        cancellationToken.ThrowIfCancellationRequested();

        var currentUser = await _currentApplicationUserResolver.ResolveAsync(cancellationToken);

        ServiceCaseAuthorization.EnsureCanManageAll(currentUser);

        var status = NormalizeFilter(request.Status);
        var operationType = NormalizeFilter(request.OperationType);

        ValidateStatus(status);
        ValidateOperationType(operationType);
        ValidateServiceCaseId(request.ServiceCaseId);

        var operations = await _repository.GetOperationsAsync(cancellationToken);

        var filteredOperations = operations
            .Where(operation =>
                status is null ||
                string.Equals(
                    operation.Status,
                    status,
                    StringComparison.OrdinalIgnoreCase))
            .Where(operation =>
                operationType is null ||
                string.Equals(
                    operation.OperationType,
                    operationType,
                    StringComparison.OrdinalIgnoreCase))
            .Where(operation =>
                !request.ServiceCaseId.HasValue ||
                operation.ServiceCaseId == request.ServiceCaseId.Value)
            .OrderByDescending(operation => operation.CreatedAtUtc)
            .ThenBy(operation => operation.OutboxId)
            .ToList();

        return new GetServiceDeskSyncOperationsResponse(Items: filteredOperations);
    }

    private static string? NormalizeFilter(string? value)
    {
        return string.IsNullOrWhiteSpace(value) ? null : value.Trim();
    }

    private static void ValidateStatus(string? status)
    {
        if (status is null)
        {
            return;
        }

        if (!AllowedStatuses.Contains(status))
        {
            throw new ArgumentException($"Unsupported ServiceDesk synchronization status: '{status}'.",  nameof(status));
        }
    }

    private static void ValidateOperationType(string? operationType)
    {
        if (operationType is null)
        {
            return;
        }

        if (!AllowedOperationTypes.Contains(operationType))
        {
            throw new ArgumentException($"Unsupported ServiceDesk operation type: '{operationType}'.", nameof(operationType));
        }
    }

    private static void ValidateServiceCaseId(Guid? serviceCaseId)
    {
        if (serviceCaseId == Guid.Empty)
        {
            throw new ArgumentException("The service case identifier cannot be empty.", nameof(serviceCaseId));
        }
    }
}