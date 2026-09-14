namespace AssetManagement.Application.Employees.GetEmployeeByEmployeeNumber;

public sealed record GetEmployeeByEmployeeNumberResponse(
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