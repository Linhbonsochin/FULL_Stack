namespace API.Models.DTOs;

public class CheckInResponse
{
    public bool Success { get; set; }
    public string Message { get; set; } = string.Empty;
    public Guid AttendanceLogId { get; set; }
    public TimeSpan CheckInTime { get; set; }
    public int LateMinutes { get; set; }
    public string Status { get; set; } = string.Empty;
}

public class CheckOutResponse
{
    public bool Success { get; set; }
    public string Message { get; set; } = string.Empty;
    public Guid AttendanceLogId { get; set; }
    public TimeSpan CheckOutTime { get; set; }
    public decimal WorkingHours { get; set; }
    public decimal OvertimeHours { get; set; }
    public int EarlyCheckOutMinutes { get; set; } = 0;
    public decimal StandardWorkday { get; set; } = 1.0m;
}

public class AttendanceHistoryDto
{
    public Guid Id { get; set; }
    public DateTime Date { get; set; }
    public TimeSpan? CheckIn { get; set; }
    public TimeSpan? CheckOut { get; set; }
    public string Status { get; set; } = string.Empty;
    public decimal OvertimeHours { get; set; }
    public string ShiftName { get; set; } = string.Empty;
    public int LateCheckInMinutes { get; set; }
    public int EarlyCheckOutMinutes { get; set; }
    public decimal StandardWorkday { get; set; }
}

/// <summary>
/// Monthly attendance summary for Payroll integration
/// Contains aggregated statistics for a specific employee and month
/// </summary>
public class MonthlyAttendanceSummaryDto
{
    public Guid EmployeeId { get; set; }
    public int Month { get; set; }
    public int Year { get; set; }

    // Working statistics
    public int WorkingDays { get; set; }
    public int PresentDays { get; set; }
    public int LeaveDays { get; set; }
    public int AbsentDays { get; set; }
    public int LateArrivals { get; set; }

    // Time-based metrics
    public int TotalLateMinutes { get; set; }
    public decimal OvertimeHours { get; set; }

    // Status
    public string Status { get; set; } = "Draft"; // Draft, Pending, Finalized
    public DateTime CalculatedAt { get; set; } = DateTime.UtcNow;

    // Frontend compatibility properties
    public int LateCount => LateArrivals;
    public int AbsentCount => AbsentDays;
    public decimal TotalHours { get; set; }
}

/// <summary>
/// Simplified employee leave summary for Payroll
/// Shows approved leave days in the specified period
/// </summary>
public class EmployeeLeaveSummaryDto
{
    public Guid EmployeeId { get; set; }
    public string EmployeeCode { get; set; } = string.Empty;
    public string FullName { get; set; } = string.Empty;
    public int Month { get; set; }
    public int Year { get; set; }

    // Leave statistics
    public int TotalLeaveDays { get; set; }
    public int ApprovedLeaveDays { get; set; }
    public int PendingLeaveRequests { get; set; }
    public int RejectedLeaveRequests { get; set; }

    // Details
    public List<LeaveDetailDto> LeaveDetails { get; set; } = new();
}

/// <summary>
/// Detailed leave request for include in summary
/// </summary>
public class LeaveDetailDto
{
    public Guid LeaveRequestId { get; set; }
    public DateTime StartDate { get; set; }
    public DateTime EndDate { get; set; }
    public string Status { get; set; } = string.Empty;
    public string Reason { get; set; } = string.Empty;
    public int LeaveDays { get; set; }
}

/// <summary>
/// Comprehensive monthly report combining attendance and leave data
/// Used when closing a month for Payroll processing
/// </summary>
public class MonthlyReportDto
{
    public int Month { get; set; }
    public int Year { get; set; }
    public DateTime GeneratedAt { get; set; } = DateTime.UtcNow;
    public string GeneratedBy { get; set; } = string.Empty;

    // Summaries by employee
    public List<EmployeeMonthlyDataDto> EmployeeData { get; set; } = new();

    // Report status
    public string Status { get; set; } = "Draft"; // Draft, Reviewed, Approved, Finalized
    public string Notes { get; set; } = string.Empty;
}
