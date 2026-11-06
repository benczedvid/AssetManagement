using AssetManagement.Domain.Entities.Employees;

namespace AssetManagement.Tests.Builders
{
    public sealed class EmployeeBuilder
    {
        private string _employeeNumber = "103549";
        private string _fullName = "John Doe";
        private DateTime _employmentStartDate =  new(2020, 1, 1);
        private DateTime? _employmentEndDate;
        private string? _storeNumber = "321";
        private string _organizationType = "Store";
        private string _positionCode = "STORE_USER";
        private string _position = "Store employee";
        private string? _phoneNumber = "+36 30 123 4567";
        private string? _email = "john.doe@example.com";

        public EmployeeBuilder WithEmployeeNumber(string employeeNumber) { _employeeNumber = employeeNumber; return this; }
        public EmployeeBuilder WithFullName(string fullName) { _fullName = fullName; return this; }
        public EmployeeBuilder WithEmploymentStartDate(DateTime employmentStartDate) { _employmentStartDate = employmentStartDate; return this; }
        public EmployeeBuilder WithEmploymentEndDate(DateTime? employmentEndDate) { _employmentEndDate = employmentEndDate; return this; }
        public EmployeeBuilder WithStoreNumber(string? storeNumber) { _storeNumber = storeNumber; return this; }
        public EmployeeBuilder WithOrganizationType(string organizationType) { _organizationType = organizationType; return this; }
        public EmployeeBuilder WithPositionCode(string positionCode) { _positionCode = positionCode; return this; }
        public EmployeeBuilder WithPosition(string position) { _position = position; return this; }
        public EmployeeBuilder WithPhoneNumber(string? phoneNumber) { _phoneNumber = phoneNumber; return this; }
        public EmployeeBuilder WithEmail(string? email) { _email = email; return this; }

        public Employee Build()
        {
            return Employee.CreateSnapshot(
                employeeNumber: _employeeNumber,
                fullName: _fullName,
                employmentStartDate: _employmentStartDate,
                employmentEndDate: _employmentEndDate,
                storeNumber: _storeNumber,
                organizationType: _organizationType,
                positionCode: _positionCode,
                position: _position,
                phoneNumber: _phoneNumber,
                email: _email);
        }
    }
}