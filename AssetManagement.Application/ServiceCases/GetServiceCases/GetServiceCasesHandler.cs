using AssetManagement.Application.Common.Authorization;
using AssetManagement.Application.Common.Interfaces.ServiceCases;
using AssetManagement.Application.Common.Interfaces.Users;
using AssetManagement.Application.ServiceCases.Exceptions;

namespace AssetManagement.Application.ServiceCases.GetServiceCases;

public sealed class GetServiceCasesHandler
{
    private readonly IServiceCaseRepository _serviceCaseRepository;
    private readonly IUserAccessScopeResolver _userAccessScopeResolver;
    private readonly ICurrentApplicationUserResolver _currentApplicationUserResolver;

    public GetServiceCasesHandler(
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

    public async Task<GetServiceCasesResponse> HandleAsync(CancellationToken cancellationToken = default)
    {
        var currentUser = await _currentApplicationUserResolver.ResolveAsync(cancellationToken);
        var accessScope = await _userAccessScopeResolver.ResolveAsync(cancellationToken);

        Guid? originStoreId = null;

        if (!accessScope.CanAccessAllStores)
        {
            if (!accessScope.StoreId.HasValue || accessScope.StoreId.Value == Guid.Empty)
            {
                throw new ServiceCaseAccessDeniedException("The current user does not have a valid store access scope.");
            }

            originStoreId = accessScope.StoreId.Value;
        }

        ServiceCaseAuthorization.EnsureCanRead(currentUser, originStoreId);

        var serviceCases = await _serviceCaseRepository.GetListAsync(originStoreId, cancellationToken);

        var items = serviceCases
            .Select(serviceCase => new ServiceCaseListItemResponse(
                Id: serviceCase.Id,
                CaseNumber: serviceCase.CaseNumber,
                AssetId: serviceCase.AssetId,
                Status: serviceCase.Status,
                IssueType: serviceCase.IssueType,
                OriginStoreId: serviceCase.OriginStoreId,
                VendorId: serviceCase.VendorId,
                IsActive: serviceCase.IsActive,
                CreatedAtUtc: serviceCase.CreatedAtUtc,
                CreatedByUserName: serviceCase.CreatedByUserName,
                ClosedAtUtc: serviceCase.ClosedAtUtc))
            .ToList();

        return new GetServiceCasesResponse(
            Items: items);
    }
}