using API.Models.DTOs;
using System.Net.Http.Json;
using System.Text.Json;
using System.Text.Json.Serialization;
using System.Security.Claims;
using System.IdentityModel.Tokens.Jwt;
using Microsoft.IdentityModel.Tokens;
using Polly;

namespace API.Services
{
    /// <summary>
    /// HR Service Client - Calls Group 1's HR microservice
    /// Pattern: Typed HttpClient with strongly-typed methods
    /// Authentication: Generates JWT Bearer Token locally using JWT configuration
    /// </summary>
    public class HrServiceClient : IHrServiceClient
    {
        private readonly HttpClient _httpClient;
        private readonly ILogger<HrServiceClient> _logger;
        private readonly IConfiguration _configuration;
        private string? _cachedToken;
        private DateTime _tokenExpiryTime = DateTime.MinValue;

        public HrServiceClient(HttpClient httpClient, ILogger<HrServiceClient> logger, IConfiguration configuration)
        {
            _httpClient = httpClient;
            _logger = logger;
            _configuration = configuration;
        }

        /// <summary>
        /// Generate JWT Token locally using configured JWT settings
        /// No call to HR Service's token endpoint - token is created with standard claims
        /// </summary>
        private async Task<string> GetTokenAsync()
        {
            try
            {
                // Check if cached token is still valid (with 5-minute buffer)
                if (!string.IsNullOrEmpty(_cachedToken) && DateTime.UtcNow < _tokenExpiryTime.AddMinutes(-5))
                {
                    _logger.LogDebug("Using cached JWT token");
                    return _cachedToken;
                }

                _logger.LogInformation("Generating JWT token locally");

                // Get JWT configuration
                var secretKey = _configuration["JwtSettings:SecretKey"];
                var issuer = _configuration["JwtSettings:Issuer"];
                var audience = _configuration["JwtSettings:Audience"];

                if (string.IsNullOrEmpty(secretKey) || string.IsNullOrEmpty(issuer) || string.IsNullOrEmpty(audience))
                {
                    _logger.LogError("JWT configuration missing: SecretKey, Issuer, or Audience not configured");
                    throw new InvalidOperationException("JWT configuration is incomplete. Check appsettings.json for JwtSettings:SecretKey, Issuer, and Audience");
                }

                // Create claims for the token
                var claims = new[]
                {
                    new Claim(ClaimTypes.NameIdentifier, Guid.NewGuid().ToString()),
                    new Claim(ClaimTypes.Role, "Service"),
                    new Claim("DepartmentId", Guid.NewGuid().ToString()),
                    new Claim(JwtRegisteredClaimNames.Jti, Guid.NewGuid().ToString())
                };

                // Create security key from secret
                var key = new SymmetricSecurityKey(System.Text.Encoding.UTF8.GetBytes(secretKey));
                var credentials = new SigningCredentials(key, SecurityAlgorithms.HmacSha256);

                // Create token
                var tokenHandler = new JwtSecurityTokenHandler();
                var expiryTime = DateTime.UtcNow.AddHours(1);
                var token = tokenHandler.CreateToken(new SecurityTokenDescriptor
                {
                    Subject = new ClaimsIdentity(claims),
                    Expires = expiryTime,
                    Issuer = issuer,
                    Audience = audience,
                    SigningCredentials = credentials
                });

                var tokenString = tokenHandler.WriteToken(token);
                _cachedToken = tokenString;
                _tokenExpiryTime = expiryTime;

                _logger.LogInformation($"JWT token generated successfully (expires {expiryTime:yyyy-MM-dd HH:mm:ss})");
                return tokenString;
            }
            catch (InvalidOperationException ex)
            {
                _logger.LogError($"JWT generation error: {ex.Message}");
                throw;
            }
            catch (Exception ex)
            {
                _logger.LogError($"Error generating JWT token: {ex.GetType().Name} - {ex.Message}");
                throw;
            }
        }

        /// <summary>
        /// Set Bearer Token to HTTP client headers
        /// </summary>
        private async Task SetAuthorizationHeaderAsync()
        {
            try
            {
                var token = await GetTokenAsync();
                _httpClient.DefaultRequestHeaders.Authorization = 
                    new System.Net.Http.Headers.AuthenticationHeaderValue("Bearer", token);
                _logger.LogDebug("Authorization header set with Bearer token");
            }
            catch (Exception ex)
            {
                _logger.LogError($"Failed to set authorization header: {ex.Message}");
                throw;
            }
        }

        /// <summary>
        /// Get all employees from HR service
        /// HR Service returns: { statusCode, message, data: [ {...}, ... ] }
        /// Handles: string id ? int conversion, case-insensitive properties
        /// </summary>
        public async Task<List<EmployeeDto>> GetEmployeesAsync()
        {
            try
            {
                await SetAuthorizationHeaderAsync();

                var employeesEndpoint = _configuration["ExternalServices:HrEmployeesEndpoint"] ?? "api/v1/hr/Employees";
                _logger.LogInformation($"🔄 [HR Service] Fetching employees: GET {employeesEndpoint}");

                // Create retry policy for transient failures
                var retryPolicy = Polly.Policy
                    .Handle<HttpRequestException>()
                    .Or<TaskCanceledException>()
                    .WaitAndRetryAsync(
                        retryCount: 3,
                        sleepDurationProvider: attempt => TimeSpan.FromMilliseconds(Math.Pow(2, attempt) * 1000));

                // Execute with retry policy
                var response = await retryPolicy.ExecuteAsync(
                    () => _httpClient.GetAsync(employeesEndpoint));

                response.EnsureSuccessStatusCode();

                var rawJson = await response.Content.ReadAsStringAsync();
                _logger.LogInformation($"✅ [HR Service] Raw response received ({rawJson.Length} bytes)");

                // Deserialize with custom converter
                var options = new JsonSerializerOptions 
                { 
                    PropertyNameCaseInsensitive = true,
                    Converters = { new API.Converters.EmployeeDtoJsonConverter() }
                };

                var wrappedResponse = JsonSerializer.Deserialize<HrApiResponse<List<EmployeeDto>>>(rawJson, options);

                if (wrappedResponse?.Data != null)
                {
                    _logger.LogInformation($"✅ [HR Service] Retrieved {wrappedResponse.Data.Count} employees (Status: {wrappedResponse.StatusCode})");
                    return wrappedResponse.Data;
                }

                _logger.LogWarning("⚠️  [HR Service] Returned null data for employees list");
                return new List<EmployeeDto>();
            }
            catch (HttpRequestException ex)
            {
                _logger.LogError($"❌ [HR Service] HTTP error: {ex.StatusCode} - {ex.Message}");
                _logger.LogError($"❌ [HR Service] Connection failed to {_httpClient.BaseAddress}{_configuration["ExternalServices:HrEmployeesEndpoint"] ?? "api/v1/hr/Employees"}");
                throw;
            }
            catch (TaskCanceledException ex)
            {
                _logger.LogError($"❌ [HR Service] Request timeout: {ex.Message}");
                _logger.LogError($"❌ HR Service connection timeout. Please ensure the service is running at https://172.16.6.17:7084");
                throw new InvalidOperationException("HR Service connection timeout. Service not responding at https://172.16.6.17:7084", ex);
            }
            catch (Exception ex)
            {
                _logger.LogError($"❌ [HR Service] Error: {ex.GetType().Name} - {ex.Message}");
                throw;
            }
        }

        /// <summary>
        /// Get employee by ID from HR service
        /// HR Service returns: { statusCode, message, data: {...} }
        /// Handles: string id ? int conversion, case-insensitive properties
        /// </summary>
        public async Task<EmployeeDto> GetEmployeeByIdAsync(int id)
        {
            try
            {
                await SetAuthorizationHeaderAsync();

                var employeesEndpoint = _configuration["ExternalServices:HrEmployeesEndpoint"] ?? "api/v1/hr/Employees";
                _logger.LogInformation($"Fetching employee {id} from HR Service: GET {employeesEndpoint}/{id}");

                // Get raw response first for debugging
                var response = await _httpClient.GetAsync($"{employeesEndpoint}/{id}");
                response.EnsureSuccessStatusCode();

                var rawJson = await response.Content.ReadAsStringAsync();
                _logger.LogInformation($"Raw HR response for employee {id}: {rawJson}");

                // Deserialize with custom converter
                var options = new JsonSerializerOptions 
                { 
                    PropertyNameCaseInsensitive = true,
                    Converters = { new API.Converters.EmployeeDtoJsonConverter() }
                };

                var wrappedResponse = JsonSerializer.Deserialize<HrApiResponse<EmployeeDto>>(rawJson, options);

                if (wrappedResponse?.Data != null)
                {
                    _logger.LogInformation($"Retrieved employee {id} from HR Service (Status: {wrappedResponse.StatusCode})");
                    return wrappedResponse.Data;
                }

                _logger.LogWarning($"Employee {id} not found in HR Service");
                throw new HttpRequestException($"Employee with ID {id} not found");
            }
            catch (HttpRequestException ex)
            {
                _logger.LogError($"HTTP error getting employee {id}: {ex.StatusCode} - {ex.Message}");
                throw;
            }
            catch (Exception ex)
            {
                _logger.LogError($"Error getting employee {id}: {ex.GetType().Name} - {ex.Message}");
                throw;
            }
        }

        /// <summary>
        /// Create new employee in HR service
        /// HR Service returns: { statusCode, message, data: {...} }
        /// Handles: string id ? int conversion, case-insensitive properties
        /// </summary>
        public async Task<EmployeeDto> CreateEmployeeAsync(EmployeeDto employee)
        {
            try
            {
                await SetAuthorizationHeaderAsync();

                var employeesEndpoint = _configuration["ExternalServices:HrEmployeesEndpoint"] ?? "api/v1/hr/Employees";
                _logger.LogInformation($"Creating employee in HR Service: POST {employeesEndpoint}");

                var response = await _httpClient.PostAsJsonAsync(employeesEndpoint, employee);
                response.EnsureSuccessStatusCode();

                var rawJson = await response.Content.ReadAsStringAsync();
                _logger.LogInformation($"Raw HR response for create: {rawJson}");

                // HR Service returns wrapped response: { statusCode, message, data: {...} }
                var options = new JsonSerializerOptions 
                { 
                    PropertyNameCaseInsensitive = true,
                    Converters = { new API.Converters.EmployeeDtoJsonConverter() }
                };

                var wrappedResponse = JsonSerializer.Deserialize<HrApiResponse<EmployeeDto>>(rawJson, options);

                if (wrappedResponse?.Data != null)
                {
                    _logger.LogInformation($"Employee created in HR Service: {wrappedResponse.Data.FullName} (Status: {wrappedResponse.StatusCode})");
                    return wrappedResponse.Data;
                }

                throw new InvalidOperationException("Failed to deserialize created employee from HR Service response");
            }
            catch (HttpRequestException ex)
            {
                _logger.LogError($"HTTP error creating employee: {ex.StatusCode} - {ex.Message}");
                throw;
            }
            catch (Exception ex)
            {
                _logger.LogError($"Error creating employee: {ex.GetType().Name} - {ex.Message}");
                throw;
            }
        }

        /// <summary>
        /// Update employee in HR service
        /// HR Service returns: { statusCode, message, data: {...} }
        /// </summary>
        public async Task<EmployeeDto> UpdateEmployeeAsync(int id, EmployeeDto employee)
        {
            try
            {
                await SetAuthorizationHeaderAsync();

                var employeesEndpoint = _configuration["ExternalServices:HrEmployeesEndpoint"] ?? "api/v1/hr/Employees";
                _logger.LogInformation($"Updating employee in HR Service: PUT {employeesEndpoint}/{id}");

                var response = await _httpClient.PutAsJsonAsync($"{employeesEndpoint}/{id}", employee);
                response.EnsureSuccessStatusCode();

                // HR Service returns wrapped response: { statusCode, message, data: {...} }
                var wrappedResponse = await response.Content.ReadFromJsonAsync<HrApiResponse<EmployeeDto>>();

                if (wrappedResponse?.Data != null)
                {
                    _logger.LogInformation($"Employee {id} updated in HR Service (Status: {wrappedResponse.StatusCode})");
                    return wrappedResponse.Data;
                }

                throw new InvalidOperationException("Failed to deserialize updated employee from HR Service response");
            }
            catch (HttpRequestException ex)
            {
                _logger.LogError($"HTTP error updating employee {id}: {ex.StatusCode} - {ex.Message}");
                throw;
            }
            catch (Exception ex)
            {
                _logger.LogError($"Error updating employee {id}: {ex.GetType().Name} - {ex.Message}");
                throw;
            }
        }

        /// <summary>
        /// Delete employee from HR service
        /// HR Service returns: { statusCode, message, data: true/false }
        /// </summary>
        public async Task<bool> DeleteEmployeeAsync(int id)
        {
            try
            {
                await SetAuthorizationHeaderAsync();

                var employeesEndpoint = _configuration["ExternalServices:HrEmployeesEndpoint"] ?? "api/v1/hr/Employees";
                _logger.LogInformation($"Deleting employee from HR Service: DELETE {employeesEndpoint}/{id}");

                var response = await _httpClient.DeleteAsync($"{employeesEndpoint}/{id}");

                if (response.IsSuccessStatusCode)
                {
                    // For DELETE, we could parse the wrapped response if needed
                    // But typically just checking IsSuccessStatusCode is enough
                    _logger.LogInformation($"Employee {id} deleted from HR Service (Status: {response.StatusCode})");
                    return true;
                }

                _logger.LogWarning($"Failed to delete employee {id} - Status: {response.StatusCode}");
                return false;
            }
            catch (HttpRequestException ex)
            {
                _logger.LogError($"HTTP error deleting employee {id}: {ex.StatusCode} - {ex.Message}");
                throw;
            }
            catch (Exception ex)
            {
                _logger.LogError($"Error deleting employee {id}: {ex.GetType().Name} - {ex.Message}");
                throw;
            }
        }
    }
}
