using API.Data;
using API.Messages.Events;
using API.Models.Entities;
using MassTransit;
using Microsoft.EntityFrameworkCore;

namespace API.Consumers;

/// <summary>
/// Consumer to listen for 'employee.created' event from HR Service (Group 1)
/// Automatically adds new employee to Attendance database
/// </summary>
public class EmployeeCreatedEventConsumer : IConsumer<EmployeeCreatedEvent>
{
    private readonly ApplicationDbContext _context;
    private readonly ILogger<EmployeeCreatedEventConsumer> _logger;

    public EmployeeCreatedEventConsumer(
        ApplicationDbContext context,
        ILogger<EmployeeCreatedEventConsumer> logger)
    {
        _context = context;
        _logger = logger;
    }

    public async Task Consume(ConsumeContext<EmployeeCreatedEvent> context)
    {
        var @event = context.Message;

        try
        {
            _logger.LogInformation(
                $"Received EmployeeCreatedEvent: EmployeeId={@event.EmployeeId}, " +
                $"EmployeeCode={@event.EmployeeCode}, FullName={@event.FullName}");

            // Check if employee already exists
            var existingEmployee = await _context.Employees
                .AsNoTracking()
                .FirstOrDefaultAsync(e => e.Id == @event.EmployeeId);

            if (existingEmployee != null)
            {
                _logger.LogWarning(
                    $"Employee {@event.EmployeeId} already exists in AttendanceDB. Skipping...");
                return;
            }

            // Create new employee in Attendance database
            var employee = new Employee
            {
                Id = @event.EmployeeId,
                EmployeeCode = @event.EmployeeCode,
                FullName = @event.FullName,
                Status = "Active",
                CreatedAt = DateTime.UtcNow
            };

            _context.Employees.Add(employee);
            await _context.SaveChangesAsync();

            _logger.LogInformation(
                $"Successfully added employee {employee.EmployeeCode} ({employee.FullName}) " +
                $"to AttendanceDB");
        }
        catch (DbUpdateException ex)
        {
            _logger.LogError(
                ex,
                $"Database error while processing EmployeeCreatedEvent for employee {@event.EmployeeId}");
            throw;
        }
        catch (Exception ex)
        {
            _logger.LogError(
                ex,
                $"Unexpected error while processing EmployeeCreatedEvent for employee {@event.EmployeeId}");
            throw;
        }
    }
}
