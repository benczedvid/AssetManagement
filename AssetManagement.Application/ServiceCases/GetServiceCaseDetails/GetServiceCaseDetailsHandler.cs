using AssetManagement.Application.Common.Authorization;
using AssetManagement.Application.Common.Interfaces.ServiceCases;
using AssetManagement.Application.Common.Interfaces.Users;
using AssetManagement.Application.ServiceCases.Authorization;
using AssetManagement.Application.ServiceCases.Exceptions;
using AssetManagement.Domain.Entities.ServiceCases;

namespace AssetManagement.Application.ServiceCases.GetServiceCaseDetails;

public sealed class GetServiceCaseDetailsHandler
{
    private readonly IServiceCaseRepository _serviceCaseRepository;
    private readonly IUserAccessScopeResolver _userAccessScopeResolver;
    private readonly ICurrentApplicationUserResolver _currentApplicationUserResolver;

    public GetServiceCaseDetailsHandler(
        IServiceCaseRepository serviceCaseRepository,
        IUserAccessScopeResolver userAccessScopeResolver,
        ICurrentApplicationUserResolver currentApplicationUserResolver)
    {
        ArgumentNullException.ThrowIfNull(serviceCaseRepository);
        ArgumentNullException.ThrowIfNull(userAccessScopeResolver);
        ArgumentNullException.ThrowIfNull(currentApplicationUserResolver);

        _serviceCaseRepository = serviceCaseRepository;
        _userAccessScopeResolver = userAccessScopeResolver;
        _currentApplicationUserResolver = currentApplicationUserResolver;
    }

    public async Task<GetServiceCaseDetailsResponse> HandleAsync(Guid serviceCaseId, CancellationToken cancellationToken = default)
    {
        if (serviceCaseId == Guid.Empty)
        {
            throw new ArgumentException("The service case identifier cannot be empty.", nameof(serviceCaseId));
        }

        var currentUser = await _currentApplicationUserResolver.ResolveAsync(cancellationToken);
        var accessScope = await _userAccessScopeResolver.ResolveAsync(cancellationToken);
        var serviceCase = await _serviceCaseRepository.GetByIdAsync(serviceCaseId, cancellationToken)
            ?? throw new ServiceCaseNotFoundException(serviceCaseId);

        ServiceCaseAuthorization.EnsureCanRead(currentUser, serviceCase.OriginStoreId);

        ValidateAccess(serviceCase, accessScope);

        var availableActions = ServiceCaseAvailableActionsResolver.Resolve(currentUser, serviceCase);
        
        var serviceCaseEvents = await _serviceCaseRepository.GetEventsByServiceCaseIdAsync(serviceCase.Id, cancellationToken);
        var events = serviceCaseEvents
            .Select(serviceCaseEvent => new ServiceCaseEventResponse(
                Id: serviceCaseEvent.Id,
                Type: serviceCaseEvent.Type,
                PreviousStatus: serviceCaseEvent.PreviousStatus,
                NewStatus: serviceCaseEvent.NewStatus,
                Comment: serviceCaseEvent.Comment,
                PerformedByUserId: serviceCaseEvent.PerformedByUserId,
                PerformedByUserName: serviceCaseEvent.PerformedByUserName,
                OccurredAtUtc: serviceCaseEvent.OccurredAtUtc))
            .ToList();

        return new GetServiceCaseDetailsResponse(
            Id: serviceCase.Id,
            CaseNumber: serviceCase.CaseNumber,
            AssetId: serviceCase.AssetId,
            Status: serviceCase.Status,
            IssueType: serviceCase.IssueType,
            OtherIssueDescription: serviceCase.OtherIssueDescription,
            Description: serviceCase.Description,
            OriginStoreId: serviceCase.OriginStoreId,
            VendorId: serviceCase.VendorId,
            IsActive: serviceCase.IsActive,
            CreatedByUserId: serviceCase.CreatedByUserId,
            CreatedByUserName: serviceCase.CreatedByUserName,
            CreatedAtUtc: serviceCase.CreatedAtUtc,
            ClosedByUserId: serviceCase.ClosedByUserId,
            ClosedAtUtc: serviceCase.ClosedAtUtc,
            Events: events,
            AvailableActions: availableActions,
            ServiceDeskRequestId: serviceCase.ServiceDeskRequestId
            );
    }

    private static void ValidateAccess(ServiceCase serviceCase, UserAccessScope accessScope)
    {
        if (accessScope.CanAccessAllStores)
        {
            return;
        }

        if (!accessScope.StoreId.HasValue || accessScope.StoreId.Value == Guid.Empty)
        {
            throw new ServiceCaseAccessDeniedException("The current user does not have a valid store access scope.");
        }

        if (serviceCase.OriginStoreId != accessScope.StoreId.Value)
        {
            throw new ServiceCaseAccessDeniedException("The current user cannot access this service case.");
        }
    }
}