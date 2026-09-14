using AssetManagement.Application.Common.Interfaces.Employees;
using AssetManagement.Application.Common.Interfaces.Stores;
using AssetManagement.Application.Common.Interfaces.Users;
using AssetManagement.Application.Common.Models;
using AssetManagement.Domain.Entities.Users;

namespace AssetManagement.Application.Users.GetOrCreateCurrentUser;

public sealed class GetOrCreateCurrentUserHandler
{
    private readonly ICurrentUser _currentUser;
    private readonly IApplicationUserRepository _userRepository;
    private readonly IEntraUserProfileService _entraUserProfileService;
    private readonly IEmployeeRepository _employeeRepository;
    private readonly IStoreRepository _storeRepository;

    public GetOrCreateCurrentUserHandler(
        ICurrentUser currentUser,
        IApplicationUserRepository userRepository,
        IEntraUserProfileService entraUserProfileService,
        IEmployeeRepository employeeRepository,
        IStoreRepository storeRepository)
    {
        ArgumentNullException.ThrowIfNull(currentUser);
        ArgumentNullException.ThrowIfNull(userRepository);
        ArgumentNullException.ThrowIfNull(entraUserProfileService);
        ArgumentNullException.ThrowIfNull(employeeRepository);
        ArgumentNullException.ThrowIfNull(storeRepository);

        _currentUser = currentUser;
        _userRepository = userRepository;
        _entraUserProfileService = entraUserProfileService;
        _employeeRepository = employeeRepository;
        _storeRepository = storeRepository;
    }

    public async Task<CurrentUserResponse> HandleAsync(
        CancellationToken cancellationToken = default)
    {
        EnsureAuthenticated();

        var applicationRole = ResolveApplicationRole();
        var entraProfile = await GetEntraUserProfileAsync(cancellationToken);

        ValidateEntraProfile(entraProfile);

        var isActive = entraProfile.IsAccountEnabled && applicationRole != ApplicationRole.Unknown;

        var storeId = await ResolveStoreIdAsync(
            entraProfile,
            applicationRole,
            cancellationToken);

        var applicationUser =
            await _userRepository.GetByEntraIdentityAsync(
                entraObjectId: _currentUser.EntraObjectId,
                entraTenantId: _currentUser.EntraTenantId,
                cancellationToken: cancellationToken);

        if (applicationUser is null)
        {
            applicationUser = CreateApplicationUser(
                entraProfile,
                applicationRole,
                storeId,
                isActive);

            await _userRepository.AddAsync(
                applicationUser,
                cancellationToken);
        }
        else
        {
            SynchronizeApplicationUser(
                applicationUser,
                entraProfile,
                applicationRole,
                storeId,
                isActive);
        }

        if (!applicationUser.IsActive)
        {
            await _userRepository.SaveChangesAsync(
                cancellationToken);

            throw new UnauthorizedAccessException(
                "The user account is inactive.");
        }

        applicationUser.RegisterLogin();

        await _userRepository.SaveChangesAsync(
            cancellationToken);

        return Map(applicationUser, entraProfile.EmployeeNumber);
    }

    private async Task<EntraUserProfile> GetEntraUserProfileAsync(
        CancellationToken cancellationToken)
    {
        var entraProfile =
            await _entraUserProfileService.GetByObjectIdAsync(
                entraObjectId: _currentUser.EntraObjectId,
                cancellationToken: cancellationToken);

        if (entraProfile is null)
        {
            throw new UnauthorizedAccessException(
                "The authenticated user could not be found in Microsoft Entra ID.");
        }

        return entraProfile;
    }

    private async Task<Guid?> ResolveStoreIdAsync(
        EntraUserProfile entraProfile,
        ApplicationRole applicationRole,
        CancellationToken cancellationToken)
    {
        if (!IsStoreScopedRole(applicationRole))
        {
            return null;
        }

        if (string.IsNullOrWhiteSpace(
                entraProfile.EmployeeNumber))
        {
            throw new UnauthorizedAccessException(
                "The store-scoped user has no employee number in Microsoft Entra ID.");
        }

        var employeeNumber =
            entraProfile.EmployeeNumber.Trim();

        var employee =
            await _employeeRepository.GetByEmployeeNumberAsync(
                employeeNumber,
                cancellationToken);

        if (employee is null)
        {
            throw new UnauthorizedAccessException(
                $"No employee record was found for employee number " +
                $"'{employeeNumber}'.");
        }

        if (string.IsNullOrWhiteSpace(employee.StoreNumber))
        {
            throw new UnauthorizedAccessException(
                $"The employee with employee number " +
                $"'{employeeNumber}' is not assigned to a store.");
        }

        var storeNumber = employee.StoreNumber.Trim();

        var store =
            await _storeRepository.GetByStoreNumberAsync(
                storeNumber,
                cancellationToken);

        if (store is null)
        {
            throw new UnauthorizedAccessException(
                $"No application store was found for store number " +
                $"'{storeNumber}'.");
        }

        return store.Id;
    }

    private ApplicationUser CreateApplicationUser(
        EntraUserProfile entraProfile,
        ApplicationRole applicationRole,
        Guid? storeId,
        bool isActive)
    {
        return ApplicationUser.CreateFromEntra(
            entraObjectId: entraProfile.EntraObjectId,
            entraTenantId: _currentUser.EntraTenantId,
            firstName: entraProfile.FirstName,
            lastName: entraProfile.LastName,
            displayName: entraProfile.DisplayName,
            role: applicationRole,
            isActive: isActive,
            mail: entraProfile.Mail,
            department: entraProfile.Department,
            jobTitle: entraProfile.JobTitle,
            mobilePhone: entraProfile.MobilePhone,
            storeId: storeId);
    }

    private static void SynchronizeApplicationUser(
        ApplicationUser applicationUser,
        EntraUserProfile entraProfile,
        ApplicationRole applicationRole,
        Guid? storeId,
        bool isActive)
    {
        ArgumentNullException.ThrowIfNull(applicationUser);
        ArgumentNullException.ThrowIfNull(entraProfile);

        applicationUser.SynchronizeProfile(
            firstName: entraProfile.FirstName,
            lastName: entraProfile.LastName,
            displayName: entraProfile.DisplayName,
            mail: entraProfile.Mail,
            department: entraProfile.Department,
            jobTitle: entraProfile.JobTitle,
            mobilePhone: entraProfile.MobilePhone);

        applicationUser.SynchronizeAccess(
            role: applicationRole,
            isActive: isActive);

        if (IsStoreScopedRole(applicationRole))
        {
            if (!storeId.HasValue)
            {
                throw new InvalidOperationException(
                    "A store-scoped user must be assigned to a store.");
            }

            applicationUser.AssignStore(storeId.Value);
        }
        else
        {
            applicationUser.RemoveStoreAssignment();
        }
    }

    private void EnsureAuthenticated()
    {
        if (!_currentUser.IsAuthenticated)
        {
            throw new UnauthorizedAccessException(
                "The current user is not authenticated.");
        }

        if (_currentUser.EntraObjectId == Guid.Empty)
        {
            throw new UnauthorizedAccessException(
                "The authenticated user has no valid Entra Object ID.");
        }

        if (_currentUser.EntraTenantId == Guid.Empty)
        {
            throw new UnauthorizedAccessException(
                "The authenticated user has no valid Entra Tenant ID.");
        }
    }

    private void ValidateEntraProfile(
        EntraUserProfile entraProfile)
    {
        if (entraProfile.EntraObjectId !=
            _currentUser.EntraObjectId)
        {
            throw new UnauthorizedAccessException(
                "The Microsoft Entra ID profile does not belong to the authenticated user.");
        }

        if (string.IsNullOrWhiteSpace(
                entraProfile.DisplayName))
        {
            throw new UnauthorizedAccessException(
                "The Microsoft Entra ID user has no display name.");
        }
    }

    private ApplicationRole ResolveApplicationRole()
    {
        if (_currentUser.Roles is null ||
            _currentUser.Roles.Count == 0)
        {
            throw new UnauthorizedAccessException(
                "The authenticated user has no assigned application role.");
        }

        if (_currentUser.Roles.Count > 1)
        {
            throw new UnauthorizedAccessException(
                "The authenticated user has multiple application roles. " +
                "Only one role is currently supported.");
        }

        var applicationRole = _currentUser.Roles.Single();

        if (!Enum.IsDefined(applicationRole) ||
            applicationRole == ApplicationRole.Unknown)
        {
            throw new UnauthorizedAccessException(
                "The authenticated user has no valid application role.");
        }

        return applicationRole;
    }

    private static bool IsStoreScopedRole(
        ApplicationRole applicationRole)
    {
        return applicationRole ==
               ApplicationRole.StoreManagement ||
               applicationRole ==
               ApplicationRole.StoreAdministrator;
    }

    private static CurrentUserResponse Map(
    ApplicationUser applicationUser,
    string? employeeNumber)
    {
        ArgumentNullException.ThrowIfNull(applicationUser);

        return new CurrentUserResponse(
            Id: applicationUser.Id,
            EntraObjectId: applicationUser.EntraObjectId,
            EntraTenantId: applicationUser.EntraTenantId,
            EmployeeNumber: string.IsNullOrWhiteSpace(employeeNumber)
                ? null
                : employeeNumber.Trim(),
            FirstName: applicationUser.FirstName,
            LastName: applicationUser.LastName,
            DisplayName: applicationUser.DisplayName,
            Role: applicationUser.Role,
            StoreId: applicationUser.StoreId,
            CanAccessAllStores: IsGlobalScopeRole(
                applicationUser.Role),
            IsActive: applicationUser.IsActive);
    }
    private static bool IsGlobalScopeRole(
    ApplicationRole applicationRole)
    {
        return applicationRole ==
                   ApplicationRole.ApplicationAdministrator ||
               applicationRole ==
                   ApplicationRole.CentralUser;
    }
}