namespace API.Models.DTOs;

/// <summary>
/// Request DTO for closing a month in attendance records
/// </summary>
public class CloseMonthDto
{
    /// <summary>
    /// Year to close (e.g., 2026)
    /// </summary>
    public int Year { get; set; }

    /// <summary>
    /// Month to close (1-12)
    /// </summary>
    public int Month { get; set; }
}
