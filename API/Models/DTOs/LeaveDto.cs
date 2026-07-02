using API.Models.Entities;

namespace API.Models.DTOs;

public class CreateLeaveRequestDto
{
    public Guid EmployeeId { get; set; }
    public DateTime StartDate { get; set; }
    public DateTime EndDate { get; set; }
    public string Reason { get; set; } = string.Empty;
    public string LeaveType { get; set; } = "Other"; // Annual, Sick, Unpaid, Other
}

public class CreateLeaveRequestResponse
{
    public bool Success { get; set; }
    public string Message { get; set; } = string.Empty;
    public Guid LeaveRequestId { get; set; }
}

public class LeaveRequestDto
{
    public Guid Id { get; set; }
    public Guid EmployeeId { get; set; }
    public string EmployeeName { get; set; } = string.Empty;
    public string EmployeeCode { get; set; } = string.Empty;
    public DateTime StartDate { get; set; }
    public DateTime EndDate { get; set; }
    public string Status { get; set; } = string.Empty;
    public string Reason { get; set; } = string.Empty;
    public string LeaveType { get; set; } = string.Empty;
    public DateTime CreatedAt { get; set; }
    public DateTime UpdatedAt { get; set; }
    public Guid? ApprovedBy { get; set; }
}

public class ApproveLeaveRequestDto
{
    public Guid LeaveRequestId { get; set; }
    public Guid ApproverId { get; set; }
    public string? Notes { get; set; }
}

/// <summary>
/// DTO for employee list item - used when selecting employee for leave request
/// Synced from HR Service via RabbitMQ events
/// </summary>
public class EmployeeListItemDto
{
    public Guid Id { get; set; }
    public string EmployeeCode { get; set; } = string.Empty;
    public string FullName { get; set; } = string.Empty;
    public string Status { get; set; } = string.Empty;
}
