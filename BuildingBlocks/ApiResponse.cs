namespace BuildingBlocks
{
    /// <summary>
    /// Standard API response wrapper for all endpoints
    /// Ensures consistent response format across the entire API
    /// </summary>
    public class ApiResponse<T>
    {
        public bool Success { get; set; }
        public T? Data { get; set; }
        public List<string>? Errors { get; set; }
        public string? Message { get; set; }

        // Success response
        public static ApiResponse<T> SuccessResponse(T data, string message = "Success")
        {
            return new ApiResponse<T>
            {
                Success = true,
                Data = data,
                Message = message,
                Errors = null
            };
        }

        // Error response
        public static ApiResponse<T> ErrorResponse(List<string> errors, string message = "Error")
        {
            return new ApiResponse<T>
            {
                Success = false,
                Data = default,
                Message = message,
                Errors = errors
            };
        }

        // Single error response
        public static ApiResponse<T> ErrorResponse(string error, string message = "Error")
        {
            return new ApiResponse<T>
            {
                Success = false,
                Data = default,
                Message = message,
                Errors = new List<string> { error }
            };
        }
    }

    /// <summary>
    /// Non-generic version for operations that don't return data
    /// </summary>
    public class ApiResponse
    {
        public bool Success { get; set; }
        public List<string>? Errors { get; set; }
        public string? Message { get; set; }

        public static ApiResponse SuccessResponse(string message = "Success")
        {
            return new ApiResponse
            {
                Success = true,
                Message = message,
                Errors = null
            };
        }

        public static ApiResponse ErrorResponse(List<string> errors, string message = "Error")
        {
            return new ApiResponse
            {
                Success = false,
                Message = message,
                Errors = errors
            };
        }

        public static ApiResponse ErrorResponse(string error, string message = "Error")
        {
            return new ApiResponse
            {
                Success = false,
                Message = message,
                Errors = new List<string> { error }
            };
        }
    }
}
