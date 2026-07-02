namespace API.Models.Entities;

/// <summary>
/// Ca làm việc (Shift)
/// Định nghĩa ca làm việc với thời gian bắt đầu, kết thúc và thời gian đi muộ cho phép
/// </summary>
public class Shift
{
    public Guid Id { get; set; } = Guid.NewGuid();

    /// <summary>
    /// Tên ca làm việc (vd: Ca Hành chính, Ca Sáng, Ca Chiều)
    /// </summary>
    public string Name { get; set; } = string.Empty;

    /// <summary>
    /// Giờ bắt đầu ca (vd: 08:00)
    /// </summary>
    public TimeSpan StartTime { get; set; }

    /// <summary>
    /// Giờ kết thúc ca (vd: 17:30)
    /// </summary>
    public TimeSpan EndTime { get; set; }

    /// <summary>
    /// Thời gian đi muộ cho phép (phút)
    /// Nếu check-in trễ hơn StartTime nhưng <= GracePeriodMinutes thì không tính đi muộ
    /// Giá trị mặc định: 15 phút
    /// </summary>
    public int GracePeriodMinutes { get; set; } = 15;

    /// <summary>
    /// Mô tả ca làm việc
    /// </summary>
    public string Description { get; set; } = string.Empty;

    /// <summary>
    /// Trạng thái: Active, Inactive
    /// </summary>
    public string Status { get; set; } = "Active";

    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    public DateTime UpdatedAt { get; set; } = DateTime.UtcNow;

    // Navigation properties
    public ICollection<AttendanceLog> AttendanceLogs { get; set; } = new List<AttendanceLog>();
}
