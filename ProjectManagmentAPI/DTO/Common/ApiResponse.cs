namespace ProjectManagementAPI.DTO.Common
{
    /// <summary>
    /// Необобщенный ответ API (для методов, не возвращающих данные)
    /// </summary>
    public class ApiResponse
    {
        public bool Success { get; set; }
        public string Message { get; set; } = string.Empty;
        public List<string>? Errors { get; set; }
        public int? StatusCode { get; set; }

        public static ApiResponse Ok(string message = "Успешно", int statusCode = 200)
        {
            return new ApiResponse
            {
                Success = true,
                Message = message,
                StatusCode = statusCode
            };
        }

        public static ApiResponse Fail(string message, List<string>? errors = null, int statusCode = 400)
        {
            return new ApiResponse
            {
                Success = false,
                Message = message,
                Errors = errors,
                StatusCode = statusCode
            };
        }
    }

    /// <summary>
    /// Обобщенный ответ API (для методов, возвращающих данные)
    /// </summary>
    public class ApiResponse<T> : ApiResponse
    {
        public T? Data { get; set; }

        public static new ApiResponse<T> Ok(T data, string message = "Успешно", int statusCode = 200)
        {
            return new ApiResponse<T>
            {
                Success = true,
                Message = message,
                Data = data,
                StatusCode = statusCode
            };
        }

        public static new ApiResponse<T> Fail(string message, List<string>? errors = null, int statusCode = 400)
        {
            return new ApiResponse<T>
            {
                Success = false,
                Message = message,
                Errors = errors,
                StatusCode = statusCode
            };
        }
    }
}