namespace Shop.BLL.Response_VM;

/// <summary>
/// Standard response envelope wrapping every API result. Carries a success
/// flag, an optional message, the payload, and the HTTP status code so clients
/// get a uniform shape for both success and failure.
/// </summary>
/// <typeparam name="T">The payload type.</typeparam>
public class ApiResponse<T>
{
    /// <summary>Whether the operation succeeded.</summary>
    public bool Success { get; set; }

    /// <summary>Optional human-readable message (set on failure or info).</summary>
    public string? Message { get; set; }

    /// <summary>The payload, present on success.</summary>
    public T? Data { get; set; }

    /// <summary>The HTTP status code associated with this response.</summary>
    public int StatusCode { get; set; }

    /// <summary>
    /// Builds a successful response (HTTP 200) wrapping <paramref name="data"/>.
    /// </summary>
    /// <param name="data">The payload to return.</param>
    /// <param name="msg">Optional informational message.</param>
    public static ApiResponse<T> Ok(T data, string? msg = null)
    {
        return new ApiResponse<T>
        {
            Success = true,
            Message = msg,
            Data = data,
            StatusCode = 200
        };
    }

    /// <summary>
    /// Builds a failed response carrying a message and the given status code.
    /// </summary>
    /// <param name="msg">The failure message.</param>
    /// <param name="statusCode">The HTTP status code to report.</param>
    public static ApiResponse<T> Fail(string msg, int statusCode)
    {
        return new ApiResponse<T>
        {
            Success = false,
            Message = msg,
            Data = default,
            StatusCode = statusCode
        };
    }
}
