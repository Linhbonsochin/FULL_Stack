namespace API.Models.Entities;

/// <summary>
/// Tổng hợp công tháng (Attendance Summary)
/// Lưu thống kê công tháng của nhân viên: ngày công thực tế, ngày nghỉ phép, ngày nghỉ ốm, etc.
/// </summary>
public class AttendanceSummary
{
    public Guid Id { get; set; } = Guid.NewGuid();

    public Guid EmployeeId { get; set; }

    /// <summary>
    /// Năm (vd: 2026)
    /// </summary>
    public int Year { get; set; }

    /// <summary>
    /// Tháng (1-12)
    /// </summary>
    public int Month { get; set; }

    /// <summary>
    /// Tổng ngày công quy chuẩn thực tế (làm việc + tính công)
    /// Ví dụ: 20.5 ngày
    /// </summary>
    public decimal TotalStandardWorkdays { get; set; } = 0;

    /// <summary>
    /// Tổng ngày nghỉ phép hưởng lương (Annual Leave)
    /// </summary>
    public decimal TotalAnnualLeaveDays { get; set; } = 0;

    /// <summary>
    /// Tổng ngày nghỉ ốm (Sick Leave)
    /// </summary>
    public decimal TotalSickLeaveDays { get; set; } = 0;

    /// <summary>
    /// Tổng ngày nghỉ không lương (Unpaid Leave)
    /// </summary>
    public decimal TotalUnpaidLeaveDays { get; set; } = 0;

    /// <summary>
    /// Tổng ngày vắng mặt không phép (Absent)
    /// </summary>
    public decimal TotalAbsentDays { get; set; } = 0;

    /// <summary>
    /// Tổng giờ làm thêm (Overtime)
    /// </summary>
    public decimal TotalOvertimeHours { get; set; } = 0;

    /// <summary>
    /// Tổng phút đi muộ trong tháng
    /// </summary>
    public int TotalLateMinutes { get; set; } = 0;

    /// <summary>
    /// Tổng phút về sớm trong tháng
    /// </summary>
    public int TotalEarlyCheckOutMinutes { get; set; } = 0;

    /// <summary>
    /// Ghi chú
    /// </summary>
    public string Notes { get; set; } = string.Empty;

    /// <summary>
    /// Trạng thái: Draft (nháp), Finalized (đã chốt công)
    /// </summary>
    public string Status { get; set; } = "Draft";

    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    public DateTime UpdatedAt { get; set; } = DateTime.UtcNow;

    // Navigation properties
    public Employee? Employee { get; set; }
}
