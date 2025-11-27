namespace ReSys.Core.Common.Models.Wrappers.Responses;

public partial class ApiResponse<T>
{
    /// <summary>
    /// A private helper method to create a standardized error response.
    /// </summary>
    /// <param name="statusCode">The HTTP status code.</param>
    /// <param name="message">The primary, human-readable error message.</param>
    /// <param name="detail">A more detailed, human-readable explanation of the error.</param>
    /// <param name="errors">A dictionary of structured errors.</param>
    /// <param name="title">A custom title for the RFC 7807 problem details. If null, it's inferred from the status code.</param>
    /// <param name="requestId">The unique request identifier for tracing.</param>
    /// <returns>An ApiResponse object configured for an error.</returns>
    private static ApiResponse<T> CreateError(
        int statusCode,
        string? message = null,
        string? detail = null,
        Dictionary<string, string[]>? errors = null,
        string? title = null,
        string? requestId = null)
    {
        string problemTitle = title ?? GetProblemTitle(statusCode: statusCode);
        string type = GetProblemTypeUri(statusCode: statusCode);

        return new ApiResponse<T>
        {
            IsSuccess = false,
            Message = message ?? problemTitle,
            Errors = errors,
            Type = type,
            Title = problemTitle,
            Status = statusCode,
            Detail = detail ?? message ?? problemTitle,
            RequestId = requestId
        };
    }

    /// <summary>
    /// Creates a generic error response with structured messages, conforming to RFC 7807 problem details.
    /// </summary>
    /// <param name="errors">A dictionary containing structured error messages.</param>
    /// <param name="message">An optional overarching error message. If not provided, a default message is used.</param>
    /// <param name="requestId">An optional unique identifier for the request for tracing purposes.</param>
    /// <param name="statusCode">The HTTP status code, defaulting to 400 (Bad Request).</param>
    /// <returns>An `ApiResponse<T>` representing the error.</returns>
    public static ApiResponse<T> Error(Dictionary<string, string[]> errors, string? message = null, string? requestId = null, int statusCode = 400)
    {
        return CreateError(
            statusCode: statusCode,
            message: message ?? "An error occurred",
            errors: errors,
            requestId: requestId);
    }

    /// <summary>
    /// Creates a generic error response with a single error message under the "General" category.
    /// </summary>
    /// <param name="error">The error message.</param>
    /// <param name="message">An optional overarching error message.</param>
    /// <param name="requestId">An optional unique identifier for the request.</param>
    /// <param name="statusCode">The HTTP status code, defaulting to 400 (Bad Request).</param>
    /// <returns>An `ApiResponse<T>` representing the error.</returns>
    public static ApiResponse<T> Error(string error, string? message = null, string? requestId = null, int statusCode = 400)
    {
        return Error(errors: new Dictionary<string, string[]>
            {
                [key: "General"] =
                [
                    error
                ]
            },
            message: message,
            requestId: requestId,
            statusCode: statusCode);
    }

    /// <summary>
    /// Creates a generic error response with a list of messages for a specific category.
    /// </summary>
    /// <param name="category">The category for the errors (e.g., a field name).</param>
    /// <param name="errors">An array of error messages for the category.</param>
    /// <param name="message">An optional overarching error message.</param>
    /// <param name="requestId">An optional unique identifier for the request.</param>
    /// <param name="statusCode">The HTTP status code, defaulting to 400 (Bad Request).</param>
    /// <returns>An `ApiResponse<T>` representing the error.</returns>
    public static ApiResponse<T> Error(string category, string[] errors, string? message = null, string? requestId = null, int statusCode = 400)
    {
        return Error(errors: new Dictionary<string, string[]>
            {
                [key: category] = errors
            },
            message: message,
            requestId: requestId,
            statusCode: statusCode);
    }

    /// <summary>
    /// Creates a 422 Unprocessable Entity response, typically for validation failures.
    /// </summary>
    /// <param name="errors">A dictionary of validation errors, where the key is the field and value is the error messages.</param>
    /// <param name="message">An optional message. Defaults to "Validation failed".</param>
    /// <param name="requestId">An optional unique identifier for the request.</param>
    /// <returns>An `ApiResponse<T>` representing a validation failure.</returns>
    public static ApiResponse<T> ValidationFailed(Dictionary<string, string[]> errors, string? message = null, string? requestId = null)
    {
        return CreateError(
            statusCode: 422,
            message: message ?? "Validation failed",
            errors: errors,
            requestId: requestId);
    }

    /// <summary>
    /// Creates a 404 Not Found error response.
    /// </summary>
    /// <param name="message">An optional message. Defaults to "Resource not found".</param>
    /// <param name="requestId">An optional unique identifier for the request.</param>
    /// <returns>An `ApiResponse<T>` representing a Not Found error.</returns>
    public static ApiResponse<T> NotFound(string? message = null, string? requestId = null)
    {
        return CreateError(
            statusCode: 404,
            message: message ?? "Resource not found",
            detail: message ?? "The requested resource was not found",
            requestId: requestId);
    }

    /// <summary>
    /// Creates a 404 Not Found error response with a custom error code as the title.
    /// </summary>
    /// <param name="errorCode">A custom error code (e.g., "User.NotFound") to be used as the problem details title.</param>
    /// <param name="message">A human-readable message explaining the error.</param>
    /// <param name="requestId">An optional unique identifier for the request.</param>
    /// <returns>An `ApiResponse<T>` representing a Not Found error with a custom code.</returns>
    public static ApiResponse<T> NotFound(string errorCode, string message, string? requestId = null)
    {
        return CreateError(
            statusCode: 404,
            message: message,
            title: errorCode,
            requestId: requestId);
    }

    /// <summary>
    /// Creates a 401 Unauthorized error response.
    /// </summary>
    /// <param name="message">An optional message. Defaults to "Unauthorized access".</param>
    /// <param name="requestId">An optional unique identifier for the request.</param>
    /// <returns>An `ApiResponse<T>` representing an Unauthorized error.</returns>
    public static ApiResponse<T> Unauthorized(string? message = null, string? requestId = null)
    {
        return CreateError(
            statusCode: 401,
            message: message ?? "Unauthorized access",
            detail: message ?? "Authentication is required to access this resource",
            requestId: requestId);
    }

    /// <summary>
    /// Creates a 403 Forbidden error response.
    /// </summary>
    /// <param name="message">An optional message. Defaults to "Access forbidden".</param>
    /// <param name="requestId">An optional unique identifier for the request.</param>
    /// <returns>An `ApiResponse<T>` representing a Forbidden error.</returns>
    public static ApiResponse<T> Forbidden(string? message = null, string? requestId = null)
    {
        return CreateError(
            statusCode: 403,
            message: message ?? "Access forbidden",
            detail: message ?? "You don't have permission to access this resource",
            requestId: requestId);
    }

    /// <summary>
    /// Creates a 409 Conflict error response.
    /// </summary>
    /// <param name="message">An optional message. Defaults to "Conflict occurred".</param>
    /// <param name="requestId">An optional unique identifier for the request.</param>
    /// <returns>An `ApiResponse<T>` representing a Conflict error.</returns>
    public static ApiResponse<T> Conflict(string? message = null, string? requestId = null)
    {
        return CreateError(
            statusCode: 409,
            message: message ?? "Conflict occurred",
            detail: message ?? "The request conflicts with the current state of the resource",
            requestId: requestId);
    }
}