namespace AssetManagement.Application.Employees.GetEmployees;

public sealed record GetEmployeesResponse(
    string EmployeeNumber,
    string FullName,
    DateTime EmploymentStartDate,
    DateTime? EmploymentEndDate,
    string? StoreNumber,
    string OrganizationType,
    string PositionCode,
    string Position,
    string? PhoneNumber,
    string? Email);