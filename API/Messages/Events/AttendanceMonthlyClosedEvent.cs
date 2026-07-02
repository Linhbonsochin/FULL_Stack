namespace API.Messages.Events;

/// <summary>
/// Event published to Payroll Service (Group 3) when monthly attendance is closed
/// Contains salary information for payroll calculation and employee contract details
/// </summary>
public record AttendanceMonthlyClosedEvent
{
    public int EmployeeId { get; set; }
    public int Year { get; set; }
    public int Month { get; set; }
    public int TotalWorkingDays { get; set; }
    public int PresentDays { get; set; }
    public int AbsentDays { get; set; }
    public int LateDays { get; set; }
    public int OnLeaveDays { get; set; }
    public decimal TotalOvertimeHours { get; set; }
    public DateTime LockedAt { get; set; }

    /// <summary>
    /// Employee's base salary (from HR data) - used by Payroll Service for salary calculation
    /// </summary>
    public decimal BaseSalary { get; set; }

    /// <summary>
    /// Employee's contract type (Full-time, Part-time, Probation) - used for payroll rules
    /// </summary>
    public string ContractType { get; set; } = string.Empty;
}
