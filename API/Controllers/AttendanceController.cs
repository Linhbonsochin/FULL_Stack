using API.Models.DTOs;
using API.Services;
using Microsoft.AspNetCore.Mvc;

namespace API.Controllers;

[ApiController]
[Route("api/[controller]")]
public class AttendanceController : ControllerBase
{
    private readonly IAttendanceService _attendanceService;
    private readonly ILogger<AttendanceController> _logger;

    public AttendanceController(IAttendanceService attendanceService, ILogger<AttendanceController> logger)
    {
        _attendanceService = attendanceService;
        _logger = logger;
    }

    /// <summary>
    /// Employee check-in: records the check-in time and calculates late minutes
    /// </summary>
    /// <param name="employeeId">The ID of the employee checking in</param>
    /// <param name="shiftId">Optional shift ID (default: First active shift)</param>
    /// <returns>Check-in response with status and late minutes</returns>
    [HttpPost("check-in")]
    [ProducesResponseType(typeof(CheckInResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status500InternalServerError)]
    public async Task<ActionResult<CheckInResponse>> CheckIn([FromBody] Guid employeeId, [FromQuery] Guid? shiftId = null)
    {
        if (employeeId == Guid.Empty)
        {
            return BadRequest(new { message = "Invalid employee ID" });
        }

        var result = await _attendanceService.CheckInAsync(employeeId, shiftId);

        if (!result.Success)
        {
            _logger.LogWarning($"Check-in failed for employee {employeeId}: {result.Message}");

            // Return 403 Forbidden when employee is inactive
            if (!string.IsNullOrEmpty(result.Message) && result.Message.IndexOf("inactive", StringComparison.OrdinalIgnoreCase) >= 0 ||
                !string.IsNullOrEmpty(result.Message) && result.Message.IndexOf("not active", StringComparison.OrdinalIgnoreCase) >= 0)
            {
                return StatusCode(StatusCodes.Status403Forbidden, result);
            }

            return BadRequest(result);
        }

        return Ok(result);
    }

    /// <summary>
    /// Employee check-out: records the check-out time, calculates working hours and overtime
    /// </summary>
    /// <param name="employeeId">The ID of the employee checking out</param>
    /// <returns>Check-out response with working hours and overtime</returns>
    [HttpPost("check-out")]
    [ProducesResponseType(typeof(CheckOutResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status500InternalServerError)]
    public async Task<ActionResult<CheckOutResponse>> CheckOut([FromBody] Guid employeeId)
    {
        if (employeeId == Guid.Empty)
        {
            return BadRequest(new { message = "Invalid employee ID" });
        }

        var result = await _attendanceService.CheckOutAsync(employeeId);

        if (!result.Success)
        {
            _logger.LogWarning($"Check-out failed for employee {employeeId}: {result.Message}");

            if (!string.IsNullOrEmpty(result.Message) && (result.Message.IndexOf("inactive", StringComparison.OrdinalIgnoreCase) >= 0 ||
                result.Message.IndexOf("not active", StringComparison.OrdinalIgnoreCase) >= 0))
            {
                return StatusCode(StatusCodes.Status403Forbidden, result);
            }

            return BadRequest(result);
        }

        return Ok(result);
    }

    /// <summary>
    /// Get attendance history for an employee
    /// </summary>
    /// <param name="employeeId">The ID of the employee</param>
    /// <param name="year">Optional year (defaults to current year if month is also provided)</param>
    /// <param name="month">Optional month (defaults to current month if not specified with year)</param>
    /// <returns>List of attendance records for the specified period</returns>
    [HttpGet("history/{employeeId}")]
    [ProducesResponseType(typeof(List<AttendanceHistoryDto>), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status500InternalServerError)]
    public async Task<ActionResult<List<AttendanceHistoryDto>>> GetHistory(
        [FromRoute] Guid employeeId,
        [FromQuery] int? year = null,
        [FromQuery] int? month = null)
    {
        if (employeeId == Guid.Empty)
        {
            return BadRequest(new { message = "Invalid employee ID" });
        }

        var history = await _attendanceService.GetHistoryAsync(employeeId, year, month);

        return Ok(history);
    }

    /// <summary>
    /// Close monthly attendance records: calculates totals and locks the month
    /// </summary>
    /// <param name="request">Close month request with year and month</param>
    /// <returns>Success or failure status</returns>
    [HttpPost("close-month")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status500InternalServerError)]
    public async Task<ActionResult> CloseMonth([FromBody] CloseMonthDto request)
    {
        if (request.Year < 2000 || request.Year > DateTime.Now.Year)
        {
            return BadRequest(new { message = "Invalid year" });
        }

        if (request.Month < 1 || request.Month > 12)
        {
            return BadRequest(new { message = "Invalid month" });
        }

        var result = await _attendanceService.CloseMonthAsync(request.Year, request.Month);

        if (!result)
        {
            _logger.LogError($"Failed to close month {request.Month}/{request.Year}");
            return StatusCode(StatusCodes.Status500InternalServerError, 
                new { message = "Failed to close the month" });
        }

        return Ok(new { message = $"Month {request.Month}/{request.Year} closed successfully" });
    }

    /// <summary>
    /// Get monthly attendance summary for Payroll integration
    /// Returns: working days, leave days, overtime hours, late minutes
    /// </summary>
    /// <param name="employeeId">Employee ID</param>
    /// <param name="month">Month (1-12)</param>
    /// <param name="year">Year</param>
    /// <returns>Monthly summary with attendance statistics</returns>
    [HttpGet("monthly-summary/{employeeId}")]
    [ProducesResponseType(typeof(MonthlyAttendanceSummaryDto), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    [ProducesResponseType(StatusCodes.Status500InternalServerError)]
    public async Task<ActionResult<MonthlyAttendanceSummaryDto>> GetMonthlySummary(
        [FromRoute] Guid employeeId,
        [FromQuery] int month,
        [FromQuery] int year)
    {
        if (employeeId == Guid.Empty)
            return BadRequest(new { message = "Invalid employee ID" });

        if (month < 1 || month > 12)
            return BadRequest(new { message = "Month must be between 1 and 12" });

        if (year < 2000 || year > DateTime.Now.Year + 1)
            return BadRequest(new { message = "Invalid year" });

        try
        {
            var summary = await _attendanceService.GetMonthlySummaryAsync(employeeId, month, year);

            if (summary == null)
                return NotFound(new { message = "Employee not found or no data available" });

            _logger.LogInformation($"Retrieved monthly summary for employee {employeeId} - {month}/{year}");
            return Ok(summary);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error retrieving monthly summary");
            return StatusCode(StatusCodes.Status500InternalServerError,
                new { message = "Error retrieving monthly summary" });
        }
    }

    /// <summary>
    /// Get leave summary for employee in specified month
    /// Returns: approved leave days, leave requests details, pending requests count
    /// </summary>
    /// <param name="employeeId">Employee ID</param>
    /// <param name="month">Month (1-12)</param>
    /// <param name="year">Year</param>
    /// <returns>Leave summary with details of all leave requests</returns>
    [HttpGet("leave-summary/{employeeId}")]
    [ProducesResponseType(typeof(EmployeeLeaveSummaryDto), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    [ProducesResponseType(StatusCodes.Status500InternalServerError)]
    public async Task<ActionResult<EmployeeLeaveSummaryDto>> GetLeaveSummary(
        [FromRoute] Guid employeeId,
        [FromQuery] int month,
        [FromQuery] int year)
    {
        if (employeeId == Guid.Empty)
            return BadRequest(new { message = "Invalid employee ID" });

        if (month < 1 || month > 12)
            return BadRequest(new { message = "Month must be between 1 and 12" });

        if (year < 2000 || year > DateTime.Now.Year + 1)
            return BadRequest(new { message = "Invalid year" });

        try
        {
            var summary = await _attendanceService.GetEmployeeLeaveSummaryAsync(employeeId, month, year);

            if (summary == null)
                return NotFound(new { message = "Employee not found or no data available" });

            _logger.LogInformation($"Retrieved leave summary for employee {employeeId} - {month}/{year}");
            return Ok(summary);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error retrieving leave summary");
            return StatusCode(StatusCodes.Status500InternalServerError,
                new { message = "Error retrieving leave summary" });
        }
    }
}
