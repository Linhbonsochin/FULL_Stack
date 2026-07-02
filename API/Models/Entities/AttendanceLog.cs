namespace API.Models.Entities;

public enum AttendanceStatus
{
    Valid = 0,
    Late = 1,
    EarlyLeave = 2,
    Absent = 3,
    OnLeave = 4
}

public class AttendanceLog
{
    public Guid Id { get; set; } = Guid.NewGuid();
    public Guid EmployeeId { get; set; }
    public DateTime Date { get; set; }
    public TimeSpan? CheckIn { get; set; }
    public TimeSpan? CheckOut { get; set; }
    public Guid ShiftId { get; set; }

    /// <summary>
    /// Số phút đi muộ (tính từ StartTime của ca)
    /// </summary>
    public int LateCheckInMinutes { get; set; } = 0;

    /// <summary>
    /// Số phút về sớm (tính từ EndTime của ca)
    /// </summary>
    public int EarlyCheckOutMinutes { get; set; } = 0;

    public decimal OvertimeHours { get; set; }

    /// <summary>
    /// Ngày công quy chuẩn (0.0 - 1.0)
    /// 1.0 = làm đủ ca, 0.5 = nửa ca, etc.
    /// </summary>
    public decimal StandardWorkday { get; set; } = 0;

    public AttendanceStatus Status { get; set; } = AttendanceStatus.Valid;
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

    // Navigation properties
    public Employee? Employee { get; set; }
    public Shift? Shift { get; set; }
}
