using API.Models.DTOs;
using API.Services;
using Microsoft.AspNetCore.Mvc;

namespace API.Controllers;

[ApiController]
[Route("api/[controller]")]
public class LeaveController : ControllerBase
{
    private readonly ILeaveService _leaveService;
    private readonly ILogger<LeaveController> _logger;

    public LeaveController(ILeaveService leaveService, ILogger<LeaveController> logger)
    {
        _leaveService = leaveService;
        _logger = logger;
    }

    /// <summary>
    /// Get list of active employees from HR data for leave request selection
    /// Demonstrates real integration with HR Service data (synced via RabbitMQ events)
    /// Endpoint: GET /api/leave/employees
    /// </summary>
    /// <returns>List of active employees with their information</returns>
    [HttpGet("employees")]
    [ProducesResponseType(typeof(List<EmployeeListItemDto>), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status500InternalServerError)]
    public async Task<ActionResult<List<EmployeeListItemDto>>> GetEmployeesForLeaveRequest()
    {
        try
        {
            var employees = await _leaveService.GetEmployeesForLeaveAsync();
            _logger.LogInformation($"Retrieved {employees.Count} employees for leave request creation");
            return Ok(employees);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error retrieving employees for leave request");
            return StatusCode(StatusCodes.Status500InternalServerError,
                new { message = "Error retrieving employees from HR data" });
        }
    }

    /// <summary>
    /// Create a new leave request
    /// </summary>
    /// <param name="request">Leave request details</param>
    /// <returns>Created leave request response</returns>
    [HttpPost("request")]
    [ProducesResponseType(typeof(CreateLeaveRequestResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status500InternalServerError)]
    public async Task<ActionResult<CreateLeaveRequestResponse>> CreateLeaveRequest([FromBody] CreateLeaveRequestDto request)
    {
        if (request == null)
        {
            return BadRequest(new { message = "Request cannot be null" });
        }

        if (request.EmployeeId == Guid.Empty)
        {
            return BadRequest(new { message = "Invalid employee ID" });
        }

        if (request.StartDate >= request.EndDate)
        {
            return BadRequest(new { message = "StartDate must be before EndDate" });
        }

        var result = await _leaveService.CreateLeaveRequestAsync(request);

        if (!result.Success)
        {
            _logger.LogWarning($"Leave request creation failed: {result.Message}");
            return BadRequest(result);
        }

        return Ok(result);
    }

    /// <summary>
    /// Get all pending leave requests (for managers/HR)
    /// </summary>
    /// <returns>List of pending leave requests</returns>
    [HttpGet("pending-list")]
    [ProducesResponseType(typeof(List<LeaveRequestDto>), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status500InternalServerError)]
    public async Task<ActionResult<List<LeaveRequestDto>>> GetPendingLeaveRequests()
    {
        try
        {
            var result = await _leaveService.GetPendingLeaveRequestsAsync();
            return Ok(result);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error retrieving pending leave requests");
            return StatusCode(StatusCodes.Status500InternalServerError, 
                new { message = "Error retrieving pending leave requests" });
        }
    }

    /// <summary>
    /// Get all leave requests for a specific employee
    /// </summary>
    /// <param name="employeeId">Employee ID</param>
    /// <returns>List of leave requests for the employee</returns>
    [HttpGet("employee/{employeeId}")]
    [ProducesResponseType(typeof(List<LeaveRequestDto>), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    [ProducesResponseType(StatusCodes.Status500InternalServerError)]
    public async Task<ActionResult<List<LeaveRequestDto>>> GetEmployeeLeaveRequests(Guid employeeId)
    {
        if (employeeId == Guid.Empty)
        {
            return BadRequest(new { message = "Invalid employee ID" });
        }

        try
        {
            var result = await _leaveService.GetLeaveRequestsByEmployeeAsync(employeeId);

            if (result == null || result.Count == 0)
            {
                return NotFound(new { message = "No leave requests found for this employee" });
            }

            return Ok(result);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error retrieving leave requests");
            return StatusCode(StatusCodes.Status500InternalServerError, 
                new { message = "Error retrieving leave requests" });
        }
    }

    /// <summary>
    /// Approve a leave request (for managers/HR)
    /// </summary>
    /// <param name="id">Leave request ID</param>
    /// <param name="request">Approval details including approver ID</param>
    /// <returns>Success or failure status</returns>
    [HttpPut("approve/{id}")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    [ProducesResponseType(StatusCodes.Status500InternalServerError)]
    public async Task<ActionResult> ApproveLeaveRequest(Guid id, [FromBody] ApproveLeaveRequestDto request)
    {
        if (id == Guid.Empty)
        {
            return BadRequest(new { message = "Invalid leave request ID" });
        }

        if (request == null || request.ApproverId == Guid.Empty)
        {
            return BadRequest(new { message = "Invalid approver ID" });
        }

        try
        {
            var result = await _leaveService.ApproveLeaveRequestAsync(id, request.ApproverId);

            if (!result)
            {
                return NotFound(new { message = "Leave request not found or cannot be approved" });
            }

            return Ok(new { message = "Leave request approved successfully" });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error approving leave request");
            return StatusCode(StatusCodes.Status500InternalServerError, 
                new { message = "Error approving leave request" });
        }
    }

    /// <summary>
    /// Reject a leave request (for managers/HR)
    /// </summary>
    /// <param name="id">Leave request ID</param>
    /// <param name="request">Rejection details including rejector ID</param>
    /// <returns>Success or failure status</returns>
    [HttpPut("reject/{id}")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    [ProducesResponseType(StatusCodes.Status500InternalServerError)]
    public async Task<ActionResult> RejectLeaveRequest(Guid id, [FromBody] ApproveLeaveRequestDto request)
    {
        if (id == Guid.Empty)
        {
            return BadRequest(new { message = "Invalid leave request ID" });
        }

        if (request == null || request.ApproverId == Guid.Empty)
        {
            return BadRequest(new { message = "Invalid rejector ID" });
        }

        try
        {
            var result = await _leaveService.RejectLeaveRequestAsync(id, request.ApproverId);

            if (!result)
            {
                return NotFound(new { message = "Leave request not found or cannot be rejected" });
            }

            return Ok(new { message = "Leave request rejected successfully" });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error rejecting leave request");
            return StatusCode(StatusCodes.Status500InternalServerError, 
                new { message = "Error rejecting leave request" });
        }
    }
}
