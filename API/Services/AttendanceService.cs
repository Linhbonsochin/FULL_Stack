using API.Data;
using API.Models.Entities;
using API.Models.DTOs;
using API.Messages.Events;
using MassTransit;
using Microsoft.EntityFrameworkCore;

namespace API.Services;

public interface IAttendanceService
{
    Task<CheckInResponse> CheckInAsync(Guid employeeId, Guid? shiftId = null);
    Task<CheckOutResponse> CheckOutAsync(Guid employeeId);
    Task<List<AttendanceHistoryDto>> GetHistoryAsync(Guid employeeId, int? year = null, int? month = null);
    Task<bool> CloseMonthAsync(int year, int month);
    Task<MonthlyAttendanceSummaryDto?> GetMonthlySummaryAsync(Guid employeeId, int month, int year);
    Task<EmployeeLeaveSummaryDto?> GetEmployeeLeaveSummaryAsync(Guid employeeId, int month, int year);
    Task<bool> GenerateAttendanceSummaryAsync(Guid employeeId, int year, int month);
    Task<AttendanceSummaryDto?> GetAttendanceSummaryAsync(Guid employeeId, int year, int month);
}

public class AttendanceService : IAttendanceService
{
    private readonly ApplicationDbContext _context;
    private readonly ILogger<AttendanceService> _logger;
    private readonly IPublishEndpoint? _publishEndpoint;
    private const int LATE_THRESHOLD_MINUTES = 5;

    public AttendanceService(
        ApplicationDbContext context,
        ILogger<AttendanceService> logger,
        IPublishEndpoint? publishEndpoint = null)
    {
        _context = context;
        _logger = logger;
        _publishEndpoint = publishEndpoint;
    }

    public async Task<CheckInResponse> CheckInAsync(Guid employeeId, Guid? shiftId = null)
    {
        try
        {
            // Validate employee ID
            if (employeeId == Guid.Empty)
                return new CheckInResponse { Success = false, Message = "Invalid employee ID" };

            var employee = await _context.Employees.FirstOrDefaultAsync(e => e.Id == employeeId && e.Status == "Active");
            if (employee == null)
                return new CheckInResponse { Success = false, Message = "Employee not found or inactive" };

            // Multi-session: chi block neu dang co phien mo (checkIn nhung chua checkOut)
            var openSession = await _context.AttendanceLogs
                .FirstOrDefaultAsync(a => a.EmployeeId == employeeId
                                       && a.Date == DateTime.UtcNow.AddHours(7).Date
                                       && a.CheckIn.HasValue
                                       && !a.CheckOut.HasValue);

            if (openSession != null)
                return new CheckInResponse { Success = false, Message = "Please check out before checking in again" };

            // Get shift - use provided shift or default to first active shift
            Shift? shift = null;
            if (shiftId.HasValue && shiftId.Value != Guid.Empty)
            {
                shift = await _context.Shifts.FirstOrDefaultAsync(s => s.Id == shiftId.Value && s.Status == "Active");
            }
            else
            {
                shift = await _context.Shifts.FirstOrDefaultAsync(s => s.Status == "Active");
            }

            if (shift == null)
                return new CheckInResponse { Success = false, Message = "Shift not found or inactive" };

            var checkInTime = DateTime.UtcNow.TimeOfDay;
            var localCheckInTime = DateTime.UtcNow.AddHours(7).TimeOfDay;
            var lateMinutes = 0;
            var status = AttendanceStatus.Valid;

            // 🆕 Calculate if late (tính đi muộn) - using local time (GMT+7)
            if (localCheckInTime > shift.StartTime)
            {
                var diff = (localCheckInTime - shift.StartTime).TotalMinutes;
                if (diff > shift.GracePeriodMinutes)
                {
                    lateMinutes = (int)Math.Ceiling(diff - shift.GracePeriodMinutes);
                    status = AttendanceStatus.Late;
                }
            }

            var attendanceLog = new AttendanceLog
            {
                EmployeeId = employeeId,
                CheckIn = checkInTime,
                Date = DateTime.UtcNow.AddHours(7).Date,
                Status = status,
                ShiftId = shift.Id,
                LateCheckInMinutes = lateMinutes,
                CreatedAt = DateTime.UtcNow
            };

            _context.AttendanceLogs.Add(attendanceLog);
            await _context.SaveChangesAsync();

            _logger.LogInformation(
                $"✅ Employee {employee.EmployeeCode} ({employee.FullName}) checked in at " +
                $"{checkInTime:hh\\:mm\\:ss} (Shift: {shift.Name}, Late: {lateMinutes}min, Status: {status})");

            return new CheckInResponse
            {
                Success = true,
                Message = "Check in successful",
                AttendanceLogId = attendanceLog.Id,
                CheckInTime = checkInTime,
                LateMinutes = lateMinutes,
                Status = status.ToString()
            };
        }
        catch (DbUpdateException ex)
        {
            _logger.LogError(ex, "Database error during check-in");
            return new CheckInResponse { Success = false, Message = "Database error during check-in" };
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error during check-in");
            return new CheckInResponse { Success = false, Message = "Error during check-in" };
        }
    }

    public async Task<CheckOutResponse> CheckOutAsync(Guid employeeId)
    {
        try
        {
            // Validate employee ID
            if (employeeId == Guid.Empty)
                return new CheckOutResponse { Success = false, Message = "Invalid employee ID" };

            var employee = await _context.Employees.FindAsync(employeeId);
            if (employee == null)
                return new CheckOutResponse { Success = false, Message = "Employee not found" };

            // Block checkout if employee is not active
            if (!string.Equals(employee.Status, "Active", StringComparison.OrdinalIgnoreCase))
            {
                _logger.LogWarning($"⛔ Attempt to check-out for non-active employee {employee.EmployeeCode} (Status={employee.Status})");
                return new CheckOutResponse { Success = false, Message = "Employee not active" };
            }

            // Multi-session: lay phien dang mo (checkIn nhung chua checkOut)
            var todayLog = await _context.AttendanceLogs
                .Include(a => a.Shift)
                .Where(a => a.EmployeeId == employeeId
                         && a.Date == DateTime.UtcNow.AddHours(7).Date
                         && a.CheckIn.HasValue
                         && !a.CheckOut.HasValue)
                .OrderBy(a => a.CheckIn)
                .LastOrDefaultAsync();

            if (todayLog == null)
                return new CheckOutResponse { Success = false, Message = "No open check-in session found for today" };

            var checkOutTime = DateTime.UtcNow.TimeOfDay;
            var localCheckOutTime = DateTime.UtcNow.AddHours(7).TimeOfDay;
            todayLog.CheckOut = checkOutTime;

            // Calculate working hours and overtime
            var workingMinutes = (checkOutTime - todayLog.CheckIn.Value).TotalMinutes;
            var workingHours = (decimal)(workingMinutes / 60.0);

            // Calculate overtime based on shift end time - using local time (GMT+7)
            decimal overtimeHours = 0;
            if (todayLog.Shift != null && localCheckOutTime > todayLog.Shift.EndTime)
            {
                var overtimeMinutes = (localCheckOutTime - todayLog.Shift.EndTime).TotalMinutes;
                overtimeHours = (decimal)(overtimeMinutes / 60.0);
                todayLog.OvertimeHours = overtimeHours;
            }

            // 🆕 Calculate Early CheckOut (tính về sớm) - using local time (GMT+7)
            int earlyCheckOutMinutes = 0;
            if (todayLog.Shift != null && localCheckOutTime < todayLog.Shift.EndTime)
            {
                earlyCheckOutMinutes = (int)Math.Ceiling((todayLog.Shift.EndTime - localCheckOutTime).TotalMinutes);
                if (todayLog.Status != AttendanceStatus.EarlyLeave)
                {
                    todayLog.Status = AttendanceStatus.EarlyLeave;
                }
            }
            todayLog.EarlyCheckOutMinutes = earlyCheckOutMinutes;

            // 🆕 Calculate Standard Workday (tính ngày công quy chuẩn)
            // Nếu làm đủ ca (không đi muộn > grace + không về sớm) = 1.0 công
            // Nếu đi muộn hoặc về sớm: tính theo tỷ lệ
            decimal standardWorkday = 1.0m;
            if (todayLog.Shift != null)
            {
                var shiftDurationMinutes = (todayLog.Shift.EndTime - todayLog.Shift.StartTime).TotalMinutes;

                // Náº¿u khÃ´ng Ä‘i muá»™ vÃ  khÃ´ng vá» sá»›m = lÃ m Ä‘á»§ ca
                if (todayLog.LateCheckInMinutes <= 0 && earlyCheckOutMinutes <= 0)
                {
                    standardWorkday = 1.0m;
                }
                // Náº¿u Ä‘i muá»™ hoáº·c vá» sá»›m: tÃ­nh theo pháº§n trÄƒm time presence
                else
                {
                    var actualWorkMinutes = shiftDurationMinutes - todayLog.LateCheckInMinutes - earlyCheckOutMinutes;
                    standardWorkday = (decimal)(actualWorkMinutes / shiftDurationMinutes);
                    standardWorkday = Math.Max(0, standardWorkday); // KhÃ´ng Ã¢m
                }
            }
            todayLog.StandardWorkday = standardWorkday;

            _context.AttendanceLogs.Update(todayLog);
            await _context.SaveChangesAsync();

            _logger.LogInformation(
                $"âœ… Employee {employee.EmployeeCode} ({employee.FullName}) checked out at " +
                $"{checkOutTime:hh\\:mm\\:ss}. Working: {workingHours:F2}h, OT: {overtimeHours:F2}h, " +
                $"Early: {earlyCheckOutMinutes}min, Workday: {standardWorkday:F2}");

            return new CheckOutResponse
            {
                Success = true,
                Message = "Check out successful",
                AttendanceLogId = todayLog.Id,
                CheckOutTime = checkOutTime,
                WorkingHours = workingHours,
                OvertimeHours = overtimeHours,
                EarlyCheckOutMinutes = earlyCheckOutMinutes,
                StandardWorkday = standardWorkday
            };
        }
        catch (DbUpdateException ex)
        {
            _logger.LogError(ex, "Database error during check-out");
            return new CheckOutResponse { Success = false, Message = "Database error during check-out" };
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error during check-out");
            return new CheckOutResponse { Success = false, Message = "Error during check-out" };
        }
    }

    public async Task<List<AttendanceHistoryDto>> GetHistoryAsync(Guid employeeId, int? year = null, int? month = null)
    {
        try
        {
            var employee = await _context.Employees.FindAsync(employeeId);
            if (employee == null)
                return new List<AttendanceHistoryDto>();

            var query = _context.AttendanceLogs
                .Include(a => a.Shift)
                .Where(a => a.EmployeeId == employeeId);

            if (year.HasValue && month.HasValue)
            {
                query = query.Where(a => a.Date.Year == year.Value && a.Date.Month == month.Value);
            }
            else if (year.HasValue)
            {
                query = query.Where(a => a.Date.Year == year.Value);
            }
            else
            {
                var now = DateTime.UtcNow.AddHours(7);
                query = query.Where(a => a.Date.Year == now.Year && a.Date.Month == now.Month);
            }

            var logs = await query
                .OrderByDescending(a => a.Date)
                .ThenBy(a => a.CheckIn)
                .ToListAsync();

            return logs.Select(l => new AttendanceHistoryDto
            {
                Id = l.Id,
                Date = l.Date,
                CheckIn = l.CheckIn,
                CheckOut = l.CheckOut,
                Status = l.Status.ToString(),
                OvertimeHours = l.OvertimeHours,
                ShiftName = l.Shift?.Name ?? string.Empty,
                LateCheckInMinutes = l.LateCheckInMinutes,
                EarlyCheckOutMinutes = l.EarlyCheckOutMinutes,
                StandardWorkday = l.StandardWorkday
            }).ToList();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error retrieving attendance history");
            return new List<AttendanceHistoryDto>();
        }
    }

    public async Task<bool> CloseMonthAsync(int year, int month)
    {
        try
        {
            _logger.LogInformation($"Starting to close month {month}/{year}");

            // Validate month and year
            if (month < 1 || month > 12)
                throw new ArgumentException("Invalid month");

            if (year < 2000 || year > DateTime.Now.Year)
                throw new ArgumentException("Invalid year");

            var startDate = new DateTime(year, month, 1);
            var endDate = startDate.AddMonths(1).AddDays(-1);

            _logger.LogInformation($"Processing attendance logs from {startDate:yyyy-MM-dd} to {endDate:yyyy-MM-dd}");

            // Get all employees
            var employees = await _context.Employees
                .AsNoTracking()
                .ToListAsync();

            foreach (var employee in employees)
            {
                // If employee is currently inactive, assume UpdatedAt is the time status was changed
                // and only count attendance up to that date. This prevents counting workdays after termination.
                var cutoffDate = endDate;
                if (!string.Equals(employee.Status, "Active", StringComparison.OrdinalIgnoreCase))
                {
                    cutoffDate = employee.UpdatedAt.Date;
                    _logger.LogInformation($"Employee {employee.EmployeeCode} is not active (Status={employee.Status}). Using cutoff date {cutoffDate:yyyy-MM-dd} for month calculations.");
                }

                // If the cutoff is before the start of the month -> no logs to process
                if (cutoffDate < startDate)
                {
                    _logger.LogInformation($"Skipping employee {employee.EmployeeCode} for {month}/{year} - inactive before start of month");
                    continue;
                }

                // Get all attendance logs for this employee in the month and before cutoff
                var monthlyLogs = await _context.AttendanceLogs
                    .Include(a => a.Shift)
                    .Where(a => a.EmployeeId == employee.Id &&
                                a.Date >= startDate &&
                                a.Date <= cutoffDate)
                    .ToListAsync();

                // Calculate statistics
                var presentDays = monthlyLogs.Count(a => a.Status == AttendanceStatus.Valid || a.Status == AttendanceStatus.Late);
                var lateDays = monthlyLogs.Count(a => a.Status == AttendanceStatus.Late);
                var onLeaveDays = monthlyLogs.Count(a => a.Status == AttendanceStatus.OnLeave);
                var absentDays = monthlyLogs.Count(a => a.Status == AttendanceStatus.Absent);
                var totalWorkingDays = GetWorkingDaysInMonth(year, month);
                var totalOvertimeHours = monthlyLogs
                    .Where(a => a.OvertimeHours > 0)
                    .Sum(a => a.OvertimeHours);

                _logger.LogInformation(
                    $"Employee {employee.EmployeeCode} - Present: {presentDays}, " +
                    $"Late: {lateDays}, OnLeave: {onLeaveDays}, Absent: {absentDays}, " +
                    $"OT Hours: {totalOvertimeHours}");

                // Create and publish event
                var monthlyClosedEvent = new AttendanceMonthlyClosedEvent
                {
                    EmployeeId = employee.Id.GetHashCode(), // Convert Guid to int for compatibility
                    Year = year,
                    Month = month,
                    TotalWorkingDays = totalWorkingDays,
                    PresentDays = presentDays,
                    AbsentDays = absentDays,
                    LateDays = lateDays,
                    OnLeaveDays = onLeaveDays,
                    TotalOvertimeHours = totalOvertimeHours,
                    LockedAt = DateTime.UtcNow
                };

                // Publish event to Payroll Service (Group 3) via RabbitMQ (if MassTransit configured)
                if (_publishEndpoint != null)
                {
                    await _publishEndpoint.Publish(monthlyClosedEvent);
                    _logger.LogInformation(
                        $"Published AttendanceMonthlyClosedEvent for employee {employee.EmployeeCode} " +
                        $"to RabbitMQ");
                }
                else
                {
                    _logger.LogWarning(
                        $"PublishEndpoint not configured - skipping RabbitMQ event publication for {employee.EmployeeCode}");
                }
            }

            _logger.LogInformation($"Successfully closed month {month}/{year}");
            return true;
        }
        catch (ArgumentException ex)
        {
            _logger.LogError(ex, "Invalid argument while closing month");
            return false;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, $"Error closing month {month}/{year}");
            return false;
        }
    }

    private static int GetWorkingDaysInMonth(int year, int month)
    {
        var startDate = new DateTime(year, month, 1);
        var endDate = startDate.AddMonths(1).AddDays(-1);

        var workingDays = 0;
        var currentDate = startDate;

        while (currentDate <= endDate)
        {
            // Count only weekdays (Monday-Friday)
            if (currentDate.DayOfWeek != DayOfWeek.Saturday && currentDate.DayOfWeek != DayOfWeek.Sunday)
            {
                workingDays++;
            }

            currentDate = currentDate.AddDays(1);
        }

        return workingDays;
    }

    /// <summary>
    /// Get monthly attendance summary for Payroll integration
    /// Calculates working days, leave days, overtime, and late arrivals
    /// </summary>
    public async Task<MonthlyAttendanceSummaryDto?> GetMonthlySummaryAsync(Guid employeeId, int month, int year)
    {
        try
        {
            var employee = await _context.Employees.FindAsync(employeeId);
            if (employee == null)
                return null;

            var startDate = new DateTime(year, month, 1);
            var endDate = startDate.AddMonths(1).AddDays(-1); //Last day of month

            // Calculate working days (excluding weekends)
            var workingDays = GetWorkingDaysInMonth(year, month);

            // Get attendance records for the month
            var attendanceRecords = await _context.AttendanceLogs
                .Where(a => a.EmployeeId == employeeId &&
                           a.Date >= startDate && a.Date <= endDate)
                .ToListAsync();

            // Calculate statistics
            int presentDays = attendanceRecords.Count(a =>
                a.Status == AttendanceStatus.Valid || a.Status == AttendanceStatus.Late || a.Status == AttendanceStatus.OnLeave);
            int leaveDays = attendanceRecords.Count(a => a.Status == AttendanceStatus.OnLeave);
            int absentDays = attendanceRecords.Count(a => a.Status == AttendanceStatus.Absent);
            int lateArrivals = attendanceRecords.Count(a => a.Status == AttendanceStatus.Late);
            int totalLateMinutes = attendanceRecords.Where(a => a.Status == AttendanceStatus.Late)
                .Sum(a => a.OvertimeHours > 0 ? (int)a.OvertimeHours : 0);
            decimal overtimeHours = attendanceRecords.Sum(a => a.OvertimeHours);

            return new MonthlyAttendanceSummaryDto
            {
                EmployeeId = employeeId,
                Month = month,
                Year = year,
                WorkingDays = workingDays,
                PresentDays = presentDays,
                LeaveDays = leaveDays,
                AbsentDays = absentDays,
                LateArrivals = lateArrivals,
                TotalLateMinutes = totalLateMinutes,
                OvertimeHours = overtimeHours,
                Status = "Draft",
                CalculatedAt = DateTime.UtcNow
            };
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, $"Error calculating monthly summary for employee {employeeId}");
            return null;
        }
    }

    /// <summary>
    /// Get leave summary for employee in specific month
    /// Shows all leave requests (approved, pending, rejected)
    /// </summary>
    public async Task<EmployeeLeaveSummaryDto?> GetEmployeeLeaveSummaryAsync(Guid employeeId, int month, int year)
    {
        try
        {
            var employee = await _context.Employees.FindAsync(employeeId);
            if (employee == null)
                return null;

            var startDate = new DateTime(year, month, 1);
            var endDate = startDate.AddMonths(1).AddDays(-1);

            // Get all leave requests for the month
            var leaveRequests = await _context.LeaveRequests
                .Where(l => l.EmployeeId == employeeId &&
                           ((l.StartDate <= endDate) && (l.EndDate >= startDate)))
                .ToListAsync();

            var approvedRequests = leaveRequests.Where(l => l.Status == "Approved").ToList();
            var approvedDays = 0;

            foreach (var leave in approvedRequests)
            {
                // Count only weekdays within the month
                var leaveStart = leave.StartDate < startDate ? startDate : leave.StartDate;
                var leaveEnd = leave.EndDate > endDate ? endDate : leave.EndDate;

                // Calculate working days manually for this leave period
                int leaveDays = 0;
                var current = leaveStart.Date;
                while (current <= leaveEnd.Date)
                {
                    if (current.DayOfWeek != DayOfWeek.Saturday && current.DayOfWeek != DayOfWeek.Sunday)
                        leaveDays++;
                    current = current.AddDays(1);
                }
                approvedDays += leaveDays;
            }

            var leaveDetails = leaveRequests
                .Where(l => l.Status == "Approved")
                .Select(l => new LeaveDetailDto
                {
                    LeaveRequestId = l.Id,
                    StartDate = l.StartDate,
                    EndDate = l.EndDate,
                    Status = l.Status,
                    Reason = l.Reason,
                    LeaveDays = CalculateWorkingDaysInRange(l.StartDate, l.EndDate)
                })
                .ToList();

            return new EmployeeLeaveSummaryDto
            {
                EmployeeId = employeeId,
                EmployeeCode = employee.EmployeeCode,
                FullName = employee.FullName,
                Month = month,
                Year = year,
                TotalLeaveDays = approvedDays,
                ApprovedLeaveDays = approvedRequests.Count,
                PendingLeaveRequests = leaveRequests.Count(l => l.Status == "Pending"),
                RejectedLeaveRequests = leaveRequests.Count(l => l.Status == "Rejected"),
                LeaveDetails = leaveDetails
            };
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, $"Error calculating leave summary for employee {employeeId}");
            return null;
        }
    }

    /// <summary>
    /// Calculate working days (weekdays only) between two dates
    /// </summary>
    private int CalculateWorkingDaysInRange(DateTime startDate, DateTime endDate)
    {
        int days = 0;
        var current = startDate.Date;
        while (current <= endDate.Date)
        {
            if (current.DayOfWeek != DayOfWeek.Saturday && current.DayOfWeek != DayOfWeek.Sunday)
                days++;
            current = current.AddDays(1);
        }
        return days;
    }

    /// <summary>
    /// ðŸ†• Generate/Update Attendance Summary for a specific month
    /// Calculates: TotalStandardWorkdays, TotalLeaveDays, TotalSickDays, etc.
    /// </summary>
    public async Task<bool> GenerateAttendanceSummaryAsync(Guid employeeId, int year, int month)
    {
        try
        {
            var employee = await _context.Employees.FindAsync(employeeId);
            if (employee == null)
            {
                _logger.LogWarning($"âŒ Employee {employeeId} not found for generating summary");
                return false;
            }

            var startDate = new DateTime(year, month, 1);
            var endDate = startDate.AddMonths(1).AddDays(-1);

            // If employee is inactive, only include attendance up to UpdatedAt (assumed status change time)
            var cutoffDate = endDate;
            if (!string.Equals(employee.Status, "Active", StringComparison.OrdinalIgnoreCase))
            {
                cutoffDate = employee.UpdatedAt.Date;
                _logger.LogInformation($"Employee {employee.EmployeeCode} inactive (Status={employee.Status}). Using cutoff {cutoffDate:yyyy-MM-dd} for summary generation.");
            }

            if (cutoffDate < startDate)
            {
                _logger.LogInformation($"No attendance to process for {employee.EmployeeCode} in {month}/{year} (inactive before month)");
            }

            // 1. Get attendance logs for the month (and before cutoff)
            var attendanceLogs = await _context.AttendanceLogs
                .Where(a => a.EmployeeId == employeeId &&
                           a.Date >= startDate && a.Date <= cutoffDate)
                .ToListAsync();

            // 2. Get approved leave requests for the month
            var approvedLeaves = await _context.LeaveRequests
                .Where(l => l.EmployeeId == employeeId &&
                           l.Status == "Approved" &&
                           ((l.StartDate <= endDate) && (l.EndDate >= startDate)))
                .ToListAsync();

            // 3. Calculate totals
            decimal totalStandardWorkdays = attendanceLogs.Sum(a => a.StandardWorkday);
            decimal totalOvertimeHours = attendanceLogs.Sum(a => a.OvertimeHours);
            int totalLateMinutes = attendanceLogs.Sum(a => a.LateCheckInMinutes);
            int totalEarlyCheckOutMinutes = attendanceLogs.Sum(a => a.EarlyCheckOutMinutes);

            // 4. Calculate leave days by type
            decimal totalAnnualLeaveDays = 0;
            decimal totalSickLeaveDays = 0;
            decimal totalUnpaidLeaveDays = 0;

            foreach (var leave in approvedLeaves)
            {
                var leaveStart = leave.StartDate < startDate ? startDate : leave.StartDate;
                var leaveEnd = leave.EndDate > endDate ? endDate : leave.EndDate;
                int leaveDays = CalculateWorkingDaysInRange(leaveStart, leaveEnd);

                // Map by LeaveType
                if (leave.LeaveType == "Annual")
                    totalAnnualLeaveDays += leaveDays;
                else if (leave.LeaveType == "Sick")
                    totalSickLeaveDays += leaveDays;
                else if (leave.LeaveType == "Unpaid")
                    totalUnpaidLeaveDays += leaveDays;
            }

            // 5. Calculate absent days (khÃ´ng Ä‘áº¿n & khÃ´ng cÃ³ lÃ½ do)
            var workingDays = GetWorkingDaysInMonth(year, month);
            var presentDays = attendanceLogs.Count(a => a.Status != AttendanceStatus.Absent);
            decimal totalAbsentDays = workingDays - presentDays -
                (totalAnnualLeaveDays + totalSickLeaveDays + totalUnpaidLeaveDays);

            // 6. Get or create summary
            var summary = await _context.AttendanceSummaries
                .FirstOrDefaultAsync(s => s.EmployeeId == employeeId &&
                                         s.Year == year &&
                                         s.Month == month);

            if (summary == null)
            {
                summary = new AttendanceSummary
                {
                    EmployeeId = employeeId,
                    Year = year,
                    Month = month,
                    CreatedAt = DateTime.UtcNow
                };
                _context.AttendanceSummaries.Add(summary);
            }

            // 7. Update values
            summary.TotalStandardWorkdays = totalStandardWorkdays;
            summary.TotalAnnualLeaveDays = totalAnnualLeaveDays;
            summary.TotalSickLeaveDays = totalSickLeaveDays;
            summary.TotalUnpaidLeaveDays = totalUnpaidLeaveDays;
            summary.TotalAbsentDays = totalAbsentDays;
            summary.TotalOvertimeHours = totalOvertimeHours;
            summary.TotalLateMinutes = totalLateMinutes;
            summary.TotalEarlyCheckOutMinutes = totalEarlyCheckOutMinutes;
            summary.Status = "Draft"; // Chá»‰ admin má»›i cÃ³ thá»ƒ finalize thÃ nh "Finalized"
            summary.UpdatedAt = DateTime.UtcNow;

            await _context.SaveChangesAsync();

            _logger.LogInformation(
                $"âœ… Generated summary for {employee.EmployeeCode} ({year}-{month:D2}): " +
                $"Workdays={totalStandardWorkdays:F1}, Annual={totalAnnualLeaveDays:F1}, " +
                $"Sick={totalSickLeaveDays:F1}, OT={totalOvertimeHours:F2}h");

            return true;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, $"Error generating attendance summary for employee {employeeId}");
            return false;
        }
    }

    /// <summary>
    /// Get Attendance Summary for specific month
    /// </summary>
    public async Task<AttendanceSummaryDto?> GetAttendanceSummaryAsync(Guid employeeId, int year, int month)
    {
        try
        {
            var summary = await _context.AttendanceSummaries
                .Include(s => s.Employee)
                .FirstOrDefaultAsync(s => s.EmployeeId == employeeId &&
                                         s.Year == year &&
                                         s.Month == month);

            if (summary == null)
                return null;

            return new AttendanceSummaryDto
            {
                Id = summary.Id,
                EmployeeId = summary.EmployeeId,
                EmployeeCode = summary.Employee?.EmployeeCode ?? string.Empty,
                EmployeeName = summary.Employee?.FullName ?? string.Empty,
                Year = summary.Year,
                Month = summary.Month,
                TotalStandardWorkdays = summary.TotalStandardWorkdays,
                TotalAnnualLeaveDays = summary.TotalAnnualLeaveDays,
                TotalSickLeaveDays = summary.TotalSickLeaveDays,
                TotalUnpaidLeaveDays = summary.TotalUnpaidLeaveDays,
                TotalAbsentDays = summary.TotalAbsentDays,
                TotalOvertimeHours = summary.TotalOvertimeHours,
                TotalLateMinutes = summary.TotalLateMinutes,
                TotalEarlyCheckOutMinutes = summary.TotalEarlyCheckOutMinutes,
                Notes = summary.Notes,
                Status = summary.Status,
                CreatedAt = summary.CreatedAt,
                UpdatedAt = summary.UpdatedAt
            };
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, $"Error getting attendance summary for employee {employeeId}");
            return null;
        }
    }
}
