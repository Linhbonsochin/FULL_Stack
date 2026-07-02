namespace API.Messages.Events;

public record EmployeeUpdatedEvent
{
    public Guid EmployeeId { get; set; }
    public string? FullName { get; set; }
    public string? Email { get; set; }
    public string? Status { get; set; }
    public string? ContractType { get; set; }
    public decimal? BaseSalary { get; set; }
    public DateTime UpdatedAt { get; set; }
}
