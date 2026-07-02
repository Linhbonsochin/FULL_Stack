namespace API.Models.Entities;

public class Employee
{
    public Guid Id { get; set; } = Guid.NewGuid();
    public string EmployeeCode { get; set; } = string.Empty;
    public string FullName { get; set; } = string.Empty;
    public string Email { get; set; } = string.Empty;
    public string Position { get; set; } = string.Empty;
    public string Phone { get; set; } = string.Empty;
    public string Address { get; set; } = string.Empty;
    public DateTime HireDate { get; set; } = DateTime.UtcNow;

    // 🆕 Fields mới từ HR Service
    public string TaxCode { get; set; } = string.Empty; // Mã số thuế cá nhân
    public int DependentsCount { get; set; } = 0; // Số người phụ thuộc
    public string IdentityNumber { get; set; } = string.Empty; // CMND/CCCD
    public string BankName { get; set; } = string.Empty; // Tên ngân hàng
    public string BankAccountNumber { get; set; } = string.Empty; // Số tài khoản
    public string BankBranch { get; set; } = string.Empty; // Chi nhánh ngân hàng

    public string Status { get; set; } = "Active"; // Active, Inactive, OnLeave
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    public DateTime UpdatedAt { get; set; } = DateTime.UtcNow;
}
