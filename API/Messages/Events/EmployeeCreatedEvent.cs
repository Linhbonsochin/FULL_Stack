namespace API.Messages.Events;

public record EmployeeCreatedEvent
{
    public Guid EmployeeId { get; set; }
    public string EmployeeCode { get; set; } = string.Empty;
    public string FullName { get; set; } = string.Empty;
    public string Email { get; set; } = string.Empty;
    public Guid DepartmentId { get; set; }
    public string ContractType { get; set; } = "Full-time";
    public decimal BaseSalary { get; set; }
    public DateTime CreatedAt { get; set; }
}
