namespace AssetManagement.Application.Common.Models;

/// <summary>
/// Represents profile information retrieved from Microsoft Entra ID.
/// </summary>
/// <param name="EntraObjectId">The unique Microsoft Entra ID object identifier of the user.</param>
/// <param name="EmployeeNumber">The employee number of the user, when available.</param>
/// <param name="FirstName">The first name of the user, when available.</param>
/// <param name="LastName">The last name of the user, when available.</param>
/// <param name="DisplayName">The display name of the user.</param>
/// <param name="Mail">The email address of the user, when available.</param>
/// <param name="Department">The department of the user, when available.</param>
/// <param name="JobTitle">The job title of the user, when available.</param>
/// <param name="MobilePhone">The mobile phone number of the user, when available.</param>
/// <param name="IsAccountEnabled">Indicates whether the Microsoft Entra ID user account is enabled.</param>
public sealed record EntraUserProfile(
    Guid EntraObjectId,
    string? EmployeeNumber,
    string? FirstName,
    string? LastName,
    string DisplayName,
    string? Mail,
    string? Department,
    string? JobTitle,
    string? MobilePhone,
    bool IsAccountEnabled);