namespace ReSys.Core.Common.Models.Wrappers.Responses;

public partial class ApiResponse<T>
{
    /// <summary>
    /// Creates a 422 Unprocessable Entity response for validation errors with structured field errors, conforming to RFC 7807.
    /// </summary>
    /// <param name="validationErrors">A dictionary where keys are field names and values are arrays of error messages for that field.</param>
    /// <param name="requestId">An optional unique identifier for the request.</param>
    /// <returns>An `ApiResponse<T>` representing a validation error.</returns>
    public static ApiResponse<T> ValidationError(Dictionary<string, string[]> validationErrors, string? requestId = null)
    {
        return CreateError(
            statusCode: 422,
            message: "Validation failed",
            detail: "One or more validation errors occurred",
            errors: validationErrors,
            requestId: requestId);
    }

    /// <summary>
    /// Creates a 422 Unprocessable Entity response for validation errors from a list of error messages.
    /// These errors will be grouped under a "Validation" category.
    /// </summary>
    /// <param name="validationErrors">A collection of string error messages.</param>
    /// <param name="requestId">An optional unique identifier for the request.</param>
    /// <returns>An `ApiResponse<T>` representing a validation error.</returns>
    public static ApiResponse<T> ValidationError(IEnumerable<string> validationErrors, string? requestId = null)
    {
        Dictionary<string, string[]> errors = new() { [key: "Validation"] = validationErrors.ToArray() };
        return ValidationError(validationErrors: errors,
            requestId: requestId);
    }

    /// <summary>
    /// Creates a 422 Unprocessable Entity response for validation errors with a single field error.
    /// </summary>
    /// <param name="fieldName">The name of the field that has the error.</param>
    /// <param name="errorMessage">The error message for the specified field.</param>
    /// <param name="requestId">An optional unique identifier for the request.</param>
    /// <returns>An `ApiResponse<T>` representing a validation error.</returns>
    public static ApiResponse<T> ValidationError(string fieldName, string errorMessage, string? requestId = null)
    {
        Dictionary<string, string[]> errors = new() { [key: fieldName] = [errorMessage] };
        return ValidationError(validationErrors: errors,
            requestId: requestId);
    }
}