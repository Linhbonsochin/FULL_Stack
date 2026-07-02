namespace API.Models.Entities;

public enum LeaveRequestStatus
{
    Pending = 0,
    Approved = 1,
    Rejected = 2
}

public enum LeaveType
{
    Annual = 0,
    Sick = 1,
    Unpaid = 2,
    Other = 3
}

public class LeaveRequest
{
    public Guid Id { get; set; } = Guid.NewGuid();
    public Guid EmployeeId { get; set; }
    public DateTime StartDate { get; set; }
    public DateTime EndDate { get; set; }
    public string Reason { get; set; } = string.Empty;
    public string Status { get; set; } = "Pending"; // Pending, Approved, Rejected
    public string LeaveType { get; set; } = "Other"; // Annual, Sick, Unpaid, Other
    public Guid? ApprovedBy { get; set; }
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    public DateTime UpdatedAt { get; set; } = DateTime.UtcNow;
}
