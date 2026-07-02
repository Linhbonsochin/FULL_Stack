namespace API.Models.DTOs
{
    /// <summary>
    /// DTO for Employee data stored locally in Attendance Service
    /// </summary>
    public class EmployeeDto
    {
        public Guid Id { get; set; }
        public string EmployeeCode { get; set; } = string.Empty;
        public string FullName { get; set; } = string.Empty;
        public string Email { get; set; } = string.Empty;
        public string Position { get; set; } = string.Empty;
        public string Phone { get; set; } = string.Empty;
        public string Address { get; set; } = string.Empty;
        public DateTime HireDate { get; set; }

        // 🆕 Fields mới từ HR Service
        public string TaxCode { get; set; } = string.Empty;
        public int DependentsCount { get; set; }
        public string IdentityNumber { get; set; } = string.Empty;
        public string BankName { get; set; } = string.Empty;
        public string BankAccountNumber { get; set; } = string.Empty;
        public string BankBranch { get; set; } = string.Empty;

        public string Status { get; set; } = string.Empty;
        public DateTime CreatedAt { get; set; }
        public DateTime UpdatedAt { get; set; }
    }
}

