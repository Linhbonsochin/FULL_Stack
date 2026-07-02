namespace API.Models.DTOs;

/// <summary>
/// Complete monthly data for a single employee (attendance + leave)
/// Used in MonthlyReportDto for comprehensive Payroll reporting
/// </summary>
public class EmployeeMonthlyDataDto
{
    // Employee info
    public Guid EmployeeId { get; set; }
    public string EmployeeCode { get; set; } = string.Empty;
    public string FullName { get; set; } = string.Empty;
    public string Department { get; set; } = string.Empty;

    // Attendance metrics
    public int WorkingDays { get; set; }
    public int PresentDays { get; set; }
    public int AbsentDays { get; set; }
    public int LateArrivals { get; set; }
    public int TotalLateMinutes { get; set; }

    // Leave metrics
    public int ApprovedLeaveDays { get; set; }
    public int PendingLeaveDays { get; set; }

    // Time-based metrics
    public decimal OvertimeHours { get; set; }
    public decimal RegularWorkingHours { get; set; }

    // Salary-related (optional, can be populated by Payroll)
    public decimal BaseSalary { get; set; }
    public decimal OvertimePayment { get; set; }
    public decimal LeaveDeduction { get; set; }

    // Status and dates
    public string Status { get; set; } = "Draft";
    public DateTime CalculatedAt { get; set; } = DateTime.UtcNow;
    public string Notes { get; set; } = string.Empty;
}
