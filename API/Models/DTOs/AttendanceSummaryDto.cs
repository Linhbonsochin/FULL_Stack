namespace API.Models.DTOs;

public class AttendanceSummaryDto
{
    public Guid Id { get; set; }
    public Guid EmployeeId { get; set; }
    public string EmployeeCode { get; set; } = string.Empty;
    public string EmployeeName { get; set; } = string.Empty;
    public int Year { get; set; }
    public int Month { get; set; }

    public decimal TotalStandardWorkdays { get; set; }
    public decimal TotalAnnualLeaveDays { get; set; }
    public decimal TotalSickLeaveDays { get; set; }
    public decimal TotalUnpaidLeaveDays { get; set; }
    public decimal TotalAbsentDays { get; set; }
    public decimal TotalOvertimeHours { get; set; }
    public int TotalLateMinutes { get; set; }
    public int TotalEarlyCheckOutMinutes { get; set; }

    public string Notes { get; set; } = string.Empty;
    public string Status { get; set; } = "Draft";
    public DateTime CreatedAt { get; set; }
    public DateTime UpdatedAt { get; set; }
}

public class AttendanceSummaryResponseDto
{
    public Guid Id { get; set; }
    public Guid EmployeeId { get; set; }
    public int Year { get; set; }
    public int Month { get; set; }
    public decimal TotalStandardWorkdays { get; set; }
    public decimal TotalAnnualLeaveDays { get; set; }
    public decimal TotalSickLeaveDays { get; set; }
    public decimal TotalUnpaidLeaveDays { get; set; }
    public decimal TotalAbsentDays { get; set; }
    public decimal TotalOvertimeHours { get; set; }
    public string Status { get; set; }
}
