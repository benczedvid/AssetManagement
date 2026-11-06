using AssetManagement.Application.Common.Interfaces.Users;
using AssetManagement.Domain.Entities.Users;

namespace AssetManagement.Application.Users.GetApplicationUsers;

/// <summary>
/// Handles requests for retrieving application users available for selection.
/// </summary>
public sealed class GetApplicationUsersHandler
{
    private readonly IApplicationUserRepository _applicationUserRepository;

    /// <summary>
    /// Initializes a new instance of the
    /// <see cref="GetApplicationUsersHandler"/> class.
    /// </summary>
    /// <param name="applicationUserRepository">
    /// The application user repository.
    /// </param>
    /// <exception cref="ArgumentNullException">
    /// Thrown when <paramref name="applicationUserRepository"/> is
    /// <see langword="null"/>.
    /// </exception>
    public GetApplicationUsersHandler(IApplicationUserRepository applicationUserRepository)
    {
        ArgumentNullException.ThrowIfNull(applicationUserRepository);

        _applicationUserRepository = applicationUserRepository;
    }

    /// <summary>
    /// Retrieves all application users available for selection.
    /// </summary>
    /// <param name="cancellationToken">
    /// A token used to cancel the asynchronous operation.
    /// </param>
    /// <returns>
    /// A read-only list containing the identifiers and display names
    /// of the application users.
    /// </returns>
    public async Task<IReadOnlyList<GetApplicationUsersResponse>> HandleAsync(CancellationToken cancellationToken = default)
    {
        var users = await _applicationUserRepository.GetAllAsync(cancellationToken);

        return [.. users.Select(Map)];
    }

    private static GetApplicationUsersResponse Map(ApplicationUser applicationUser)
    {
        return new GetApplicationUsersResponse(
            Id: applicationUser.Id,
            DisplayName: applicationUser.DisplayName);
    }
}