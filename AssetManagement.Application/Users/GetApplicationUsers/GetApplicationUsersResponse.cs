namespace AssetManagement.Application.Users.GetApplicationUsers;

/// <summary>
/// Represents an application user available for selection.
/// </summary>
/// <param name="Id">
/// The unique identifier of the application user.
/// </param>
/// <param name="DisplayName">
/// The display name of the application user.
/// </param>
public sealed record GetApplicationUsersResponse(
    Guid Id,
    string DisplayName);