using API.Data;
using API.Models.Entities;
using API.Models.DTOs;
using Microsoft.EntityFrameworkCore;

namespace API.Services;

public interface ILeaveService
{
    Task<CreateLeaveRequestResponse> CreateLeaveRequestAsync(CreateLeaveRequestDto request);
    Task<List<LeaveRequestDto>> GetPendingLeaveRequestsAsync();
    Task<List<LeaveRequestDto>> GetLeaveRequestsByEmployeeAsync(Guid employeeId);
    Task<bool> ApproveLeaveRequestAsync(Guid leaveRequestId, Guid approverId);
    Task<bool> RejectLeaveRequestAsync(Guid leaveRequestId, Guid approverId);

    /// <summary>
    /// Get list of active employees from HR data (synced via RabbitMQ events)
    /// This demonstrates real integration with HR Service data
    /// </summary>
    Task<List<EmployeeListItemDto>> GetEmployeesForLeaveAsync();
}

public class LeaveService : ILeaveService
{
    private readonly ApplicationDbContext _context;
    private readonly ILogger<LeaveService> _logger;

    public LeaveService(ApplicationDbContext context, ILogger<LeaveService> logger)
    {
        _context = context;
        _logger = logger;
    }

    public async Task<CreateLeaveRequestResponse> CreateLeaveRequestAsync(CreateLeaveRequestDto request)
    {
        try
        {
            // Validate employee ID
            if (request.EmployeeId == Guid.Empty)
            {
                _logger.LogWarning("Invalid employee ID provided");
                return new CreateLeaveRequestResponse { Success = false, Message = "Invalid employee ID" };
            }

            var employee = await _context.Employees.FirstOrDefaultAsync(e => e.Id == request.EmployeeId && e.Status == "Active");
            if (employee == null)
            {
                _logger.LogWarning($"Employee not found or inactive: {request.EmployeeId}");
                return new CreateLeaveRequestResponse { Success = false, Message = "Employee not found or inactive" };
            }

            // Validate dates
            if (request.StartDate >= request.EndDate)
            {
                _logger.LogWarning($"Invalid date range: StartDate ({request.StartDate}) >= EndDate ({request.EndDate})");
                return new CreateLeaveRequestResponse { Success = false, Message = "StartDate must be before EndDate" };
            }

            if (request.StartDate < DateTime.UtcNow.Date)
            {
                _logger.LogWarning($"StartDate ({request.StartDate:yyyy-MM-dd}) is in the past");
                return new CreateLeaveRequestResponse { Success = false, Message = "Cannot create leave request for past dates" };
            }

            // Check for overlapping approved leave requests
            var overlappingLeaves = await _context.LeaveRequests
                .Where(l => l.EmployeeId == request.EmployeeId &&
                            (l.Status == "Approved" || l.Status == "Pending") &&
                            ((request.StartDate >= l.StartDate && request.StartDate < l.EndDate) ||
                             (request.EndDate > l.StartDate && request.EndDate <= l.EndDate) ||
                             (request.StartDate <= l.StartDate && request.EndDate >= l.EndDate)))
                .AnyAsync();

            if (overlappingLeaves)
            {
                _logger.LogWarning($"Overlapping leave request exists for employee {employee.EmployeeCode} " +
                    $"from {request.StartDate:yyyy-MM-dd} to {request.EndDate:yyyy-MM-dd}");
                return new CreateLeaveRequestResponse 
                { 
                    Success = false, 
                    Message = "Leave request overlaps with existing leave request" 
                };
            }

            var leaveRequest = new LeaveRequest
            {
                EmployeeId = request.EmployeeId,
                StartDate = request.StartDate,
                EndDate = request.EndDate,
                Reason = request.Reason ?? string.Empty,
                LeaveType = request.LeaveType ?? "Other",
                Status = "Pending",
                CreatedAt = DateTime.UtcNow
            };

            _context.LeaveRequests.Add(leaveRequest);
            await _context.SaveChangesAsync();

            _logger.LogInformation(
                $"Leave request {leaveRequest.Id} created for employee {employee.EmployeeCode} " +
                $"from {request.StartDate:yyyy-MM-dd} to {request.EndDate:yyyy-MM-dd}");

            return new CreateLeaveRequestResponse
            {
                Success = true,
                Message = "Leave request created successfully",
                LeaveRequestId = leaveRequest.Id
            };
        }
        catch (DbUpdateException ex)
        {
            _logger.LogError(ex, "Database error while creating leave request: {ErrorMessage}", ex.Message);
            return new CreateLeaveRequestResponse 
            { 
                Success = false, 
                Message = $"Database error: {ex.InnerException?.Message ?? ex.Message}" 
            };
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error creating leave request: {ErrorMessage}", ex.Message);
            return new CreateLeaveRequestResponse 
            { 
                Success = false, 
                Message = $"Error creating leave request: {ex.Message}" 
            };
        }
    }

    public async Task<List<LeaveRequestDto>> GetPendingLeaveRequestsAsync()
    {
        try
        {
            var leaveRequests = await _context.LeaveRequests
                .Where(l => l.Status == "Pending")
                .OrderByDescending(l => l.CreatedAt)
                .ToListAsync();

            // Get employee codes separately
            var employeeIds = leaveRequests.Select(l => l.EmployeeId).Distinct().ToList();
            var employees = await _context.Employees
                .Where(e => employeeIds.Contains(e.Id))
                .ToListAsync();

            var employeeDict = employees.ToDictionary(e => e.Id);

            return leaveRequests.Select(l => new LeaveRequestDto
            {
                Id = l.Id,
                EmployeeId = l.EmployeeId,
                EmployeeName = employeeDict.ContainsKey(l.EmployeeId) ? employeeDict[l.EmployeeId].FullName : string.Empty,
                EmployeeCode = employeeDict.ContainsKey(l.EmployeeId) ? employeeDict[l.EmployeeId].EmployeeCode : string.Empty,
                StartDate = l.StartDate,
                EndDate = l.EndDate,
                Status = l.Status,
                Reason = l.Reason,
                LeaveType = l.LeaveType,
                CreatedAt = l.CreatedAt,
                UpdatedAt = l.UpdatedAt,
                ApprovedBy = l.ApprovedBy
            }).ToList();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error retrieving pending leave requests");
            return new List<LeaveRequestDto>();
        }
    }

    public async Task<List<LeaveRequestDto>> GetLeaveRequestsByEmployeeAsync(Guid employeeId)
    {
        try
        {
            if (employeeId == Guid.Empty)
                return new List<LeaveRequestDto>();

            var leaveRequests = await _context.LeaveRequests
                .Where(l => l.EmployeeId == employeeId)
                .OrderByDescending(l => l.CreatedAt)
                .ToListAsync();

            // Get employee information
            var employee = await _context.Employees.FirstOrDefaultAsync(e => e.Id == employeeId);

            return leaveRequests.Select(l => new LeaveRequestDto
            {
                Id = l.Id,
                EmployeeId = l.EmployeeId,
                EmployeeName = employee?.FullName ?? string.Empty,
                EmployeeCode = employee?.EmployeeCode ?? string.Empty,
                StartDate = l.StartDate,
                EndDate = l.EndDate,
                Status = l.Status,
                Reason = l.Reason,
                LeaveType = l.LeaveType,
                CreatedAt = l.CreatedAt,
                UpdatedAt = l.UpdatedAt,
                ApprovedBy = l.ApprovedBy
            }).ToList();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error retrieving leave requests for employee");
            return new List<LeaveRequestDto>();
        }
    }

    public async Task<bool> ApproveLeaveRequestAsync(Guid leaveRequestId, Guid approverId)
    {
        try
        {
            // Validate IDs
            if (leaveRequestId == Guid.Empty || approverId == Guid.Empty)
                return false;

            var leaveRequest = await _context.LeaveRequests
                .FirstOrDefaultAsync(l => l.Id == leaveRequestId);

            if (leaveRequest == null)
                return false;

            if (leaveRequest.Status != "Pending")
                return false;

            leaveRequest.Status = "Approved";
            leaveRequest.ApprovedBy = approverId;
            leaveRequest.UpdatedAt = DateTime.UtcNow;

            // Create/Update attendance logs for leave days (only weekdays)
            var leaveDays = (int)(leaveRequest.EndDate.Date - leaveRequest.StartDate.Date).TotalDays + 1;
            var logsCreatedCount = 0;

            for (int i = 0; i < leaveDays; i++)
            {
                var leaveDate = leaveRequest.StartDate.Date.AddDays(i);

                // Skip weekends
                if (leaveDate.DayOfWeek == DayOfWeek.Saturday || leaveDate.DayOfWeek == DayOfWeek.Sunday)
                    continue;

                var attendanceLog = await _context.AttendanceLogs
                    .FirstOrDefaultAsync(a => a.EmployeeId == leaveRequest.EmployeeId && a.Date == leaveDate);

                if (attendanceLog == null)
                {
                    attendanceLog = new AttendanceLog
                    {
                        EmployeeId = leaveRequest.EmployeeId,
                        Date = leaveDate,
                        Status = AttendanceStatus.OnLeave,
                        ShiftId = Guid.Parse("00000000-0000-0000-0000-000000000001"), // Default shift (Ca Hành chính)
                        StandardWorkday = 1.0m, // Full day leave = 1.0 workday
                        CreatedAt = DateTime.UtcNow
                    };
                    _context.AttendanceLogs.Add(attendanceLog);
                    logsCreatedCount++;
                }
                else if (attendanceLog.Status == AttendanceStatus.Valid || attendanceLog.Status == AttendanceStatus.Absent)
                {
                    // Only update if not already checked in/out
                    if (!attendanceLog.CheckIn.HasValue)
                    {
                        attendanceLog.Status = AttendanceStatus.OnLeave;
                        _context.AttendanceLogs.Update(attendanceLog);
                        logsCreatedCount++;
                    }
                }
            }

            _context.LeaveRequests.Update(leaveRequest);
            await _context.SaveChangesAsync();

            _logger.LogInformation(
                $"Leave request {leaveRequestId} approved by {approverId}. " +
                $"Created {logsCreatedCount} attendance logs");

            return true;
        }
        catch (DbUpdateException ex)
        {
            _logger.LogError(ex, $"Database error while approving leave request {leaveRequestId}");
            return false;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, $"Error approving leave request {leaveRequestId}");
            return false;
        }
    }

    public async Task<bool> RejectLeaveRequestAsync(Guid leaveRequestId, Guid approverId)
    {
        try
        {
            // Validate IDs
            if (leaveRequestId == Guid.Empty || approverId == Guid.Empty)
                return false;

            var leaveRequest = await _context.LeaveRequests
                .FirstOrDefaultAsync(l => l.Id == leaveRequestId);

            if (leaveRequest == null)
                return false;

            if (leaveRequest.Status != "Pending")
                return false;            leaveRequest.Status = "Rejected";
            leaveRequest.ApprovedBy = approverId;

            _context.LeaveRequests.Update(leaveRequest);
            await _context.SaveChangesAsync();

            _logger.LogInformation(
                $"Leave request {leaveRequestId} rejected by {approverId}");

            return true;
        }
        catch (DbUpdateException ex)
        {
            _logger.LogError(ex, $"Database error while rejecting leave request {leaveRequestId}");
            return false;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, $"Error rejecting leave request {leaveRequestId}");
            return false;
        }
    }

    /// <summary>
    /// Get list of active employees from local database
    /// </summary>
    public async Task<List<EmployeeListItemDto>> GetEmployeesForLeaveAsync()
    {
        try
        {
            var employees = await _context.Employees
                .Where(e => e.Status == "Active")
                .Select(e => new EmployeeListItemDto
                {
                    Id = e.Id,
                    EmployeeCode = e.EmployeeCode,
                    FullName = e.FullName,
                    Status = e.Status
                })
                .OrderBy(e => e.FullName)
                .ToListAsync();

            _logger.LogInformation($"Retrieved {employees.Count} active employees for leave request selection");
            return employees;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error retrieving employees for leave request");
            throw;
        }
    }
}
