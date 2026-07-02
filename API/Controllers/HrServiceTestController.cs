using API.Models.DTOs;
using API.Services;
using Microsoft.AspNetCore.Mvc;

namespace API.Controllers
{
    /// <summary>
    /// HR Service Test Controller
    /// Test endpoint to verify Attendance ↔ HR Service connectivity
    /// </summary>
    [ApiController]
    [Route("api/test")]
    public class HrServiceTestController : ControllerBase
    {
        private readonly IHrServiceClient _hrServiceClient;
        private readonly ILogger<HrServiceTestController> _logger;

        public HrServiceTestController(
            IHrServiceClient hrServiceClient,
            ILogger<HrServiceTestController> logger)
        {
            _hrServiceClient = hrServiceClient;
            _logger = logger;
        }

        /// <summary>
        /// Test endpoint: Verify Attendance can fetch employees from HR Service
        /// GET /api/test/hr-employees
        /// </summary>
        [HttpGet("hr-employees")]
        public async Task<IActionResult> GetEmployeesFromHrService()
        {
            try
            {
                _logger.LogInformation("Test: Fetching employees from HR Service");
                var employees = await _hrServiceClient.GetEmployeesAsync();

                _logger.LogInformation($"Test: Successfully retrieved {employees.Count} employees");
                return Ok(new 
                { 
                    success = true, 
                    count = employees.Count,
                    data = employees 
                });
            }
            catch (Exception ex)
            {
                _logger.LogError($"Test: Error - {ex.GetType().Name}: {ex.Message}");
                return StatusCode(500, new 
                { 
                    success = false, 
                    error = ex.Message,
                    type = ex.GetType().Name
                });
            }
        }
    }
}
