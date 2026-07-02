using API.Models.DTOs;

namespace API.Services
{
    /// <summary>
    /// Interface for HR Service Client to call Group 1's HR service
    /// Pattern: Typed HttpClient with strongly-typed methods
    /// </summary>
    public interface IHrServiceClient
    {
        /// <summary>
        /// Get all employees from HR service
        /// Endpoint: GET /api/v1/hr/Employees
        /// </summary>
        Task<List<EmployeeDto>> GetEmployeesAsync();

        /// <summary>
        /// Get employee by ID from HR service
        /// Endpoint: GET /api/v1/hr/Employees/{id}
        /// </summary>
        Task<EmployeeDto> GetEmployeeByIdAsync(int id);

        /// <summary>
        /// Create new employee in HR service
        /// Endpoint: POST /api/v1/hr/Employees
        /// </summary>
        Task<EmployeeDto> CreateEmployeeAsync(EmployeeDto employee);

        /// <summary>
        /// Update employee in HR service
        /// Endpoint: PUT /api/v1/hr/Employees/{id}
        /// </summary>
        Task<EmployeeDto> UpdateEmployeeAsync(int id, EmployeeDto employee);

        /// <summary>
        /// Delete employee from HR service
        /// Endpoint: DELETE /api/v1/hr/Employees/{id}
        /// </summary>
        Task<bool> DeleteEmployeeAsync(int id);
    }
}
