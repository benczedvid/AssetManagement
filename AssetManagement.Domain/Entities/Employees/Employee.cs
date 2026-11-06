namespace AssetManagement.Domain.Entities.Employees
{
    public sealed class Employee
    {
        public string EmployeeNumber { get; private set; } = string.Empty;
        public string FullName { get; private set; } = string.Empty;
        public DateTime EmploymentStartDate { get; private set; }
        public DateTime? EmploymentEndDate { get; private set; }
        public string? StoreNumber { get; private set; }
        public string OrganizationType { get; private set; } = string.Empty;
        public string PositionCode { get; private set; } =  string.Empty;
        public string Position { get; private set; } = string.Empty;
        public string? PhoneNumber { get; private set; }
        public string? Email { get; private set; }

        private Employee()
        {
        }

        private Employee(
            string employeeNumber,
            string fullName,
            DateTime employmentStartDate,
            DateTime? employmentEndDate,
            string? storeNumber,
            string organizationType,
            string positionCode,
            string position,
            string? phoneNumber,
            string? email)
        {
            EmployeeNumber = employeeNumber;
            FullName = fullName;
            EmploymentStartDate = employmentStartDate;
            EmploymentEndDate = employmentEndDate;
            StoreNumber = storeNumber;
            OrganizationType = organizationType;
            PositionCode = positionCode;
            Position = position;
            PhoneNumber = phoneNumber;
            Email = email;
        }

        internal static Employee CreateSnapshot(
            string employeeNumber,
            string fullName,
            DateTime employmentStartDate,
            DateTime? employmentEndDate,
            string? storeNumber,
            string organizationType,
            string positionCode,
            string position,
            string? phoneNumber,
            string? email)
        {
            ArgumentException.ThrowIfNullOrWhiteSpace(employeeNumber);
            ArgumentException.ThrowIfNullOrWhiteSpace(fullName);
            ArgumentException.ThrowIfNullOrWhiteSpace(organizationType);
            ArgumentException.ThrowIfNullOrWhiteSpace(positionCode);
            ArgumentException.ThrowIfNullOrWhiteSpace(position);

            if (employmentEndDate.HasValue && employmentEndDate.Value < employmentStartDate)
            {
                throw new ArgumentException("The employment end date cannot be earlier than the employment start date.", nameof(employmentEndDate));
            }

            return new Employee(
                employeeNumber: employeeNumber.Trim(),
                fullName: fullName.Trim(),
                employmentStartDate: employmentStartDate,
                employmentEndDate: employmentEndDate,
                storeNumber: NormalizeOptional(storeNumber),
                organizationType: organizationType.Trim(),
                positionCode: positionCode.Trim(),
                position: position.Trim(),
                phoneNumber: NormalizeOptional(phoneNumber),
                email: NormalizeOptional(email));
        }

        private static string? NormalizeOptional(string? value)
        {
            return string.IsNullOrWhiteSpace(value) ? null : value.Trim();
        }
    }
}