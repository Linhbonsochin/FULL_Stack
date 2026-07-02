namespace API.Models.DTOs;

public class ShiftDto
{
    public Guid Id { get; set; }
    public string Name { get; set; } = string.Empty;
    public TimeSpan StartTime { get; set; }
    public TimeSpan EndTime { get; set; }
    public int GracePeriodMinutes { get; set; }
    public string Description { get; set; } = string.Empty;
    public string Status { get; set; } = "Active";
    public DateTime CreatedAt { get; set; }
    public DateTime UpdatedAt { get; set; }
}

public class CreateShiftDto
{
    public string Name { get; set; } = string.Empty;
    public TimeSpan StartTime { get; set; }
    public TimeSpan EndTime { get; set; }
    public int GracePeriodMinutes { get; set; } = 15;
    public string Description { get; set; } = string.Empty;
}

public class UpdateShiftDto
{
    public string? Name { get; set; }
    public TimeSpan? StartTime { get; set; }
    public TimeSpan? EndTime { get; set; }
    public int? GracePeriodMinutes { get; set; }
    public string? Description { get; set; }
    public string? Status { get; set; }
}
