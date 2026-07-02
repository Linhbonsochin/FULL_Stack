using API.Data;
using API.Messages.Events;
using MassTransit;
using Microsoft.EntityFrameworkCore;

namespace API.Consumers;

/// <summary>
/// Consumer to listen for 'employee.updated' event from HR Service (Group 1)
/// Automatically updates employee information in Attendance database
/// </summary>
public class EmployeeUpdatedEventConsumer : IConsumer<EmployeeUpdatedEvent>
{
    private readonly ApplicationDbContext _context;
    private readonly ILogger<EmployeeUpdatedEventConsumer> _logger;

    public EmployeeUpdatedEventConsumer(
        ApplicationDbContext context,
        ILogger<EmployeeUpdatedEventConsumer> logger)
    {
        _context = context;
        _logger = logger;
    }

    public async Task Consume(ConsumeContext<EmployeeUpdatedEvent> context)
    {
        var @event = context.Message;

        try
        {
            _logger.LogInformation(
                $"Received EmployeeUpdatedEvent: EmployeeId={@event.EmployeeId}");

            // Find employee in Attendance database
            var employee = await _context.Employees
                .FirstOrDefaultAsync(e => e.Id == @event.EmployeeId);

            if (employee == null)
            {
                _logger.LogWarning(
                    $"Employee {@event.EmployeeId} not found in AttendanceDB. Skipping update...");
                return;
            }

            // Update only the fields that have values
            if (!string.IsNullOrEmpty(@event.FullName))
            {
                employee.FullName = @event.FullName;
            }

            if (!string.IsNullOrEmpty(@event.Status))
            {
                employee.Status = @event.Status;
            }

            _context.Employees.Update(employee);
            await _context.SaveChangesAsync();

            _logger.LogInformation(
                $"Successfully updated employee {@event.EmployeeId} " +
                $"({employee.EmployeeCode}: {employee.FullName}) in AttendanceDB");
        }
        catch (DbUpdateException ex)
        {
            _logger.LogError(
                ex,
                $"Database error while processing EmployeeUpdatedEvent for employee {@event.EmployeeId}");
            throw;
        }
        catch (Exception ex)
        {
            _logger.LogError(
                ex,
                $"Unexpected error while processing EmployeeUpdatedEvent for employee {@event.EmployeeId}");
            throw;
        }
    }
}
