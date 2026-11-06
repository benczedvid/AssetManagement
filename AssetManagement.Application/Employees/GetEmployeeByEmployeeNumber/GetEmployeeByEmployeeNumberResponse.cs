namespace AssetManagement.Application.Employees.GetEmployeeByEmployeeNumber;

/// <summary>
/// Represents detailed information about an employee returned by the API.
/// </summary>
/// <param name="EmployeeNumber">The unique employee number of the employee.</param>
/// <param name="FullName">The full name of the employee.</param>
/// <param name="EmploymentStartDate">The date and time when the employee's employment started.</param>
/// <param name="EmploymentEndDate">The date and time when the employee's employment ended, when applicable.</param>
/// <param name="StoreNumber">The store number of the store to which the employee is assigned, when applicable.</param>
/// <param name="OrganizationType">The organizational type associated with the employee.</param>
/// <param name="PositionCode">The code identifying the employee's position.</param>
/// <param name="Position">The name or description of the employee's position.</param>
/// <param name="PhoneNumber">The phone number of the employee, when available.</param>
/// <param name="Email">The email address of the employee, when available.</param>
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