namespace API.Models.DTOs
{
    /// <summary>
    /// Generic wrapper for HR Service API responses
    /// HR Service returns all responses in format: { statusCode, message, data }
    /// This DTO allows proper deserialization of wrapped responses
    /// </summary>
    public class HrApiResponse<T>
    {
        /// <summary>
        /// HTTP status code from HR Service
        /// </summary>
        public int StatusCode { get; set; }

        /// <summary>
        /// Response message from HR Service
        /// </summary>
        public string Message { get; set; } = string.Empty;

        /// <summary>
        /// Actual response data (wrapped in 'data' property by HR Service)
        /// Can be: List<EmployeeDto>, EmployeeDto, bool, etc.
        /// </summary>
        public T Data { get; set; }
    }
}
