using AssetManagement.Application.Common.Authorization;
using AssetManagement.Domain.Entities.ServiceCases;
using AssetManagement.Domain.Entities.Users;

namespace AssetManagement.Application.ServiceCases.Authorization;

/// <summary>
/// Resolves the service case actions available to the current user.
/// </summary>
/// <remarks>
/// The returned actions are intended for user interface guidance.
/// Every operation must still perform authoritative authorization
/// and state validation in its application handler.
/// </remarks>
public static class ServiceCaseAvailableActionsResolver
{
    /// <summary>
    /// Resolves the actions available for the specified service case.
    /// </summary>
    /// <param name="currentUser">
    /// The current application user.
    /// </param>
    /// <param name="serviceCase">
    /// The service case for which the available actions are resolved.
    /// </param>
    /// <returns>
    /// A read-only list containing the available action identifiers.
    /// </returns>
    /// <exception cref="ArgumentNullException">
    /// The current user or service case is null.
    /// </exception>
    public static IReadOnlyList<string> Resolve(ApplicationUser currentUser,ServiceCase serviceCase)
    {
        ArgumentNullException.ThrowIfNull(currentUser);
        ArgumentNullException.ThrowIfNull(serviceCase);

        ServiceCaseAuthorization.EnsureCanRead(currentUser, serviceCase.OriginStoreId);

        var roleName = currentUser.Role.ToString();
        var canManageAll = RolePermissions.HasPermission(roleName, ApplicationPermissions.ServiceCasesManageAll);
        var canClose = RolePermissions.HasPermission(roleName, ApplicationPermissions.ServiceCasesClose);
        var canDisposeAsset = currentUser.IsActive && currentUser.Role == ApplicationRole.ApplicationAdministrator;
        var hasValidOriginStore = serviceCase.OriginStoreId.HasValue && serviceCase.OriginStoreId.Value != Guid.Empty;
        var hasValidVendor = serviceCase.VendorId.HasValue && serviceCase.VendorId.Value != Guid.Empty;
        var actions = new List<string>();

        switch (serviceCase.Status)
        {
            case ServiceCaseStatus.AwaitingCentralReceipt:
                // Receipt is handled through the Packages workflow.
                break;


            case ServiceCaseStatus.UnderCentralInspection:
                if (canManageAll)
                {
                    if (!serviceCase.VendorId.HasValue)
                    {
                        actions.Add(ServiceCaseActions.RequireExternalService);
                    }

                    if (!serviceCase.OriginStoreId.HasValue ||
                        hasValidOriginStore)
                    {
                        actions.Add(ServiceCaseActions.CompleteRepairAtHeadquarters);
                    }
                }

                if (canDisposeAsset)
                {
                    actions.Add(ServiceCaseActions.DisposeAsset);
                }

                break;

            case ServiceCaseStatus.AwaitingExternalServiceDispatch:
                if (canManageAll && hasValidVendor)
                {
                    actions.Add(ServiceCaseActions.DispatchToExternalService);
                    actions.Add(ServiceCaseActions.HandOverToExternalService);
                }

                if (canDisposeAsset && hasValidVendor)
                {
                    actions.Add(ServiceCaseActions.DisposeAsset);
                }

                break;

            case ServiceCaseStatus.SentToExternalService:
                if (canManageAll && hasValidVendor)
                {
                    actions.Add(ServiceCaseActions.ConfirmReceivedByExternalService);
                }

                break;

            case ServiceCaseStatus.InExternalService:
                if (canManageAll && hasValidVendor)
                {
                    actions.Add(ServiceCaseActions.ReturnFromExternalService);
                }

                if (canDisposeAsset && hasValidVendor)
                {
                    actions.Add(ServiceCaseActions.DisposeAsset);
                }

                break;

                case ServiceCaseStatus.AwaitingReturn:
                    if (canManageAll &&
                        !serviceCase.OriginStoreId.HasValue &&
                        hasValidVendor)
                    {
                        actions.Add(
                            ServiceCaseActions.ResolveAfterExternalServiceReturn);
                    }

                    if (canDisposeAsset)
                    {
                        actions.Add(ServiceCaseActions.DisposeAsset);
                    }

                    break;


            case ServiceCaseStatus.ReturningToStore:
                // Receipt at the origin store is handled through Packages.
                break;

            case ServiceCaseStatus.Resolved:
                if (canClose)
                {
                    actions.Add(ServiceCaseActions.Close);
                }

                break;

            default:
                break;
        }

        return actions.AsReadOnly();
    }
}