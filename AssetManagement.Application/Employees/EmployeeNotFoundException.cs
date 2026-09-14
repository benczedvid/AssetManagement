namespace AssetManagement.Application.Employees;
    public sealed class EmployeeNotFoundException : Exception
    {
        public EmployeeNotFoundException(string employeeNumber) : base($"The employee with {employeeNumber} was not found.")
        {
            EmployeeNumber = employeeNumber;
        }

        public string EmployeeNumber { get; }
    }


