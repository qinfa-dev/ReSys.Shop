namespace ReSys.Core.Common.Models.Wrappers.Responses;

/// <summary>
/// Non-generic version of ApiResponse for responses without a specific data type.
/// This class inherits from `ApiResponse<object>` and provides convenience methods
/// for scenarios where the data payload is not relevant or is of type `object`.
/// </summary>
public sealed class ApiResponse : ApiResponse<object>
{
    /// <summary>
    /// Creates a successful 200 OK response without a specific data payload.
    /// </summary>
    /// <param name="message">An optional message to accompany the response. Defaults to "Operation completed successfully".</param>
    /// <param name="requestId">An optional unique identifier for the request.</param>
    /// <returns>A successful non-generic `ApiResponse`.</returns>
    public static ApiResponse Success(string? message = null, string? requestId = null)
        => new() { IsSuccess = true, Message = message ?? "Operation completed successfully", RequestId = requestId, Status = 200 };

    /// <summary>
    /// Creates a generic error response with structured messages, conforming to RFC 7807 problem details.
    /// </summary>
    /// <param name="errors">A dictionary containing structured error messages.</param>
    /// <param name="message">An optional overarching error message. If not provided, a default message is used.</param>
    /// <param name="requestId">An optional unique identifier for the request for tracing purposes.</param>
    /// <param name="statusCode">The HTTP status code, defaulting to 400 (Bad Request).</param>
    /// <returns>A non-generic `ApiResponse` representing the error.</returns>
    public static new ApiResponse Error(Dictionary<string, string[]> errors, string? message = null, string? requestId = null, int statusCode = 400)
        => (ApiResponse)ApiResponse<object>.Error(errors: errors,
            message: message,
            requestId: requestId,
            statusCode: statusCode);

    /// <summary>
    /// Creates a generic error response with a single error message under the "General" category.
    /// </summary>
    /// <param name="error">The error message.</param>
    /// <param name="message">An optional overarching error message.</param>
    /// <param name="requestId">An optional unique identifier for the request.</param>
    /// <param name="statusCode">The HTTP status code, defaulting to 400 (Bad Request).</param>
    /// <returns>A non-generic `ApiResponse` representing the error.</returns>
    public static new ApiResponse Error(string error, string? message = null, string? requestId = null, int statusCode = 400)
        => (ApiResponse)ApiResponse<object>.Error(error: error,
            message: message,
            requestId: requestId,
            statusCode: statusCode);

    /// <summary>
    /// Creates a 404 Not Found error response.
    /// </summary>
    /// <param name="message">An optional message. Defaults to "Resource not found".</param>
    /// <param name="requestId">An optional unique identifier for the request.</param>
    /// <returns>A non-generic `ApiResponse` representing a Not Found error.</returns>
    public static new ApiResponse NotFound(string? message = null, string? requestId = null)
        => (ApiResponse)ApiResponse<object>.NotFound(message: message,
            requestId: requestId);

    /// <summary>
    /// Creates a 401 Unauthorized error response.
    /// </summary>
    /// <param name="message">An optional message. Defaults to "Unauthorized access".</param>
    /// <param name="requestId">An optional unique identifier for the request.</param>
    /// <returns>A non-generic `ApiResponse` representing an Unauthorized error.</returns>
    public static new ApiResponse Unauthorized(string? message = null, string? requestId = null)
        => (ApiResponse)ApiResponse<object>.Unauthorized(message: message,
            requestId: requestId);

    /// <summary>
    /// Creates a 422 Unprocessable Entity response, typically for validation failures.
    /// </summary>
    /// <param name="validationErrors">A dictionary of validation errors, where the key is the field and value is the error messages.</param>
    /// <param name="requestId">An optional unique identifier for the request.</param>
    /// <returns>A non-generic `ApiResponse` representing a validation failure.</returns>
    public static new ApiResponse ValidationError(Dictionary<string, string[]> validationErrors, string? requestId = null)
        => (ApiResponse)ValidationFailed(errors: validationErrors,
            requestId: requestId);
}