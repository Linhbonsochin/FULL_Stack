using API.Services;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace API.Controllers
{
    /// <summary>
    /// Employee Synchronization Controller
    /// Endpoints to sync employees from HR Service to local database
    /// </summary>
    [ApiController]
    [Route("api/employee-sync")]
    public class EmployeeSyncController : ControllerBase
    {
        private readonly IEmployeeSyncService _syncService;
        private readonly ILogger<EmployeeSyncController> _logger;

        public EmployeeSyncController(
            IEmployeeSyncService syncService,
            ILogger<EmployeeSyncController> logger)
        {
            _syncService = syncService;
            _logger = logger;
        }

        /// <summary>
        /// Sync employees from HR Service to database
        /// Endpoint: POST /api/employee-sync/sync
        /// 
        /// This endpoint:
        /// 1. Fetches employees from HR Service (https://172.16.6.17:7084)
        /// 2. Compares with local database
        /// 3. Inserts new employees
        /// 4. Updates existing employees
        /// 5. Returns sync results
        /// </summary>
        [HttpPost("sync")]
        [ProducesResponseType(typeof(object), StatusCodes.Status200OK)]
        [ProducesResponseType(typeof(object), StatusCodes.Status500InternalServerError)]
        public async Task<IActionResult> SyncEmployees()
        {
            try
            {
                _logger.LogInformation("📡 [API] Endpoint called: POST /api/employee-sync/sync");

                var syncResult = await _syncService.SyncEmployeesAsync();

                _logger.LogInformation($"✅ [API] Sync completed: {syncResult} employees synced");

                return Ok(new
                {
                    success = true,
                    message = $"✅ Employee synchronization completed successfully",
                    syncedCount = syncResult,
                    timestamp = DateTime.UtcNow
                });
            }
            catch (Exception ex)
            {
                _logger.LogError($"❌ [API] Error during sync: {ex.Message}");
                return StatusCode(500, new
                {
                    success = false,
                    message = "❌ Error during employee synchronization",
                    error = ex.Message,
                    timestamp = DateTime.UtcNow
                });
            }
        }

        /// <summary>
        /// Get employee synchronization status
        /// Endpoint: GET /api/employee-sync/status
        /// 
        /// Returns:
        /// - LocalEmployeeCount: Employees in local database
        /// - HrEmployeeCount: Employees in HR Service
        /// - IsHealthy: HR Service availability
        /// - Message: Status message
        /// </summary>
        [HttpGet("status")]
        [ProducesResponseType(typeof(object), StatusCodes.Status200OK)]
        [ProducesResponseType(typeof(object), StatusCodes.Status500InternalServerError)]
        public async Task<IActionResult> GetSyncStatus()
        {
            try
            {
                _logger.LogInformation("📊 [API] Endpoint called: GET /api/employee-sync/status");

                var status = await _syncService.GetSyncStatusAsync();

                return Ok(new
                {
                    success = true,
                    data = new
                    {
                        localEmployeeCount = status.LocalEmployeeCount,
                        hrEmployeeCount = status.HrEmployeeCount,
                        syncedCount = status.SyncedCount,
                        lastSyncTime = status.LastSyncTime,
                        isHealthy = status.IsHealthy,
                        message = status.Message
                    },
                    timestamp = DateTime.UtcNow
                });
            }
            catch (Exception ex)
            {
                _logger.LogError($"❌ [API] Error getting sync status: {ex.Message}");
                return StatusCode(500, new
                {
                    success = false,
                    message = "Error getting sync status",
                    error = ex.Message,
                    timestamp = DateTime.UtcNow
                });
            }
        }

        /// <summary>
        /// Get all employees from local database
        /// Endpoint: GET /api/employee-sync/local-employees
        /// 
        /// Returns list of all employees currently in local database
        /// </summary>
        [HttpGet("local-employees")]
        [ProducesResponseType(typeof(object), StatusCodes.Status200OK)]
        public async Task<IActionResult> GetLocalEmployees()
        {
            try
            {
                var appDbContext = HttpContext.RequestServices
                    .GetRequiredService<API.Data.ApplicationDbContext>();

                var employees = await appDbContext.Employees
                    .Select(e => new
                    {
                        id = e.Id,
                        employeeCode = e.EmployeeCode,
                        fullName = e.FullName,
                        status = e.Status,
                        createdAt = e.CreatedAt,
                        updatedAt = e.UpdatedAt
                    })
                    .ToListAsync();

                return Ok(new
                {
                    success = true,
                    count = employees.Count,
                    data = employees,
                    timestamp = DateTime.UtcNow
                });
            }
            catch (Exception ex)
            {
                _logger.LogError($"❌ Error getting local employees: {ex.Message}");
                return StatusCode(500, new
                {
                    success = false,
                    message = "Error getting local employees",
                    error = ex.Message
                });
            }
        }
    }
}
