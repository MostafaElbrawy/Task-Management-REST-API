namespace Task_Management.DTOs;

public class ApiResponse<T>
{
    public bool Success { get; set; }
    public string Message { get; set; } = string.Empty;
    public T? Data { get; set; }
    public List<string> Errors { get; set; } = new();
    public int StatusCode { get; set; }

    // return methods
    public static ApiResponse<T> Ok(T? data, string message = "Success")
    => new() { Success = true, Data = data, Message = message, StatusCode = 200 };

    public static ApiResponse<T> Created(T data, string message = "Created successfully")
    => new() { Success = true, Data = data, Message = message, StatusCode = 201 };

    public static ApiResponse<T> Fail(string message, List<string>? errors = null, int statusCode = 400)
    => new() { Success = false, Message = message, Errors = errors ?? new(), StatusCode = statusCode };

    public static ApiResponse<T> Unauthorized(string message = "Unauthorized")
    => new() { Success = false, Message = message, StatusCode = 401 };

    public static ApiResponse<T> NotFound(string message = "Not found")
    => new() { Success = false, Message = message, StatusCode = 404 };

    public static ApiResponse<T> ValidationError(List<string>? errors = null, string message = "Validation failed")
    => new() { Success = false, Message = message, Errors = errors ?? new(), StatusCode = 422 };

    //public static ApiResponse<T> TooManyRequests(string message = "Too many requests")
    //=> new() { Success = false, Message = message, StatusCode = 429 };

    public static ApiResponse<T> Forbid(string message = "Forbidden")
    => new() { Success = false, Message = message, Errors = new(), StatusCode = 403 };
}