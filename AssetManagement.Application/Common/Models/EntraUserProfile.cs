namespace AssetManagement.Application.Common.Models;

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