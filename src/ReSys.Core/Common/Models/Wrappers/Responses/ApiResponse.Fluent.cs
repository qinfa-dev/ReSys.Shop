namespace ReSys.Core.Common.Models.Wrappers.Responses;

public partial class ApiResponse<T>
{
    /// <summary>
    /// Adds a HATEOAS link to the response.
    /// </summary>
    /// <param name="linkName">The name of the link (e.g., "self", "next").</param>
    /// <param name="url">The URL for the link.</param>
    /// <returns>The current `ApiResponse<T>` instance for method chaining.</returns>
    public ApiResponse<T> WithLink(string linkName, string url)
    {
        Links ??= new Dictionary<string, string>();
        Links[key: linkName] = url;
        return this;
    }

    /// <summary>
    /// Adds multiple HATEOAS links to the response.
    /// </summary>
    /// <param name="links">A dictionary where keys are link names and values are URLs.</param>
    /// <returns>The current `ApiResponse<T>` instance for method chaining.</returns>
    public ApiResponse<T> WithLinks(Dictionary<string, string> links)
    {
        Links ??= new Dictionary<string, string>();
        foreach (KeyValuePair<string, string> link in links)
        {
            Links[key: link.Key] = link.Value;
        }
        return this;
    }

    /// <summary>
    /// Adds a single metadata entry to the response.
    /// </summary>
    /// <param name="key">The key for the metadata entry.</param>
    /// <param name="value">The value for the metadata entry.</param>
    /// <returns>The current `ApiResponse<T>` instance for method chaining.</returns>
    public ApiResponse<T> WithMetadata(string key, object value)
    {
        Metadata ??= new Dictionary<string, object>();
        Metadata[key: key] = value;
        return this;
    }

    /// <summary>
    /// Adds multiple metadata entries to the response.
    /// </summary>
    /// <param name="metadata">A dictionary of metadata entries.</param>
    /// <returns>The current `ApiResponse<T>` instance for method chaining.</returns>
    public ApiResponse<T> WithMetadata(Dictionary<string, object> metadata)
    {
        Metadata ??= new Dictionary<string, object>();
        foreach (KeyValuePair<string, object> entry in metadata)
        {
            Metadata[key: entry.Key] = entry.Value;
        }
        return this;
    }

    /// <summary>
    /// Adds structured errors to the response under a specified category.
    /// </summary>
    /// <param name="category">The category for the errors (e.g., a field name).</param>
    /// <param name="errors">An array of error messages for the category.</param>
    /// <returns>The current `ApiResponse<T>` instance for method chaining.</returns>
    public ApiResponse<T> WithError(string category, string[] errors)
    {
        Errors ??= new Dictionary<string, string[]>();
        Errors[key: category] = errors;
        return this;
    }

    /// <summary>
    /// Adds a single error message to the response under a specified category.
    /// </summary>
    /// <param name="category">The category for the error (e.g., a field name).</param>
    /// <param name="error">The single error message.</param>
    /// <returns>The current `ApiResponse<T>` instance for method chaining.</returns>
    public ApiResponse<T> WithError(string category, string error)
    {
        return WithError(category: category,
            errors:
            [
                error
            ]);
    }

    /// <summary>
    /// Sets the API version for the response.
    /// </summary>
    /// <param name="version">The API version string (e.g., "1.0").</param>
    /// <returns>The current `ApiResponse<T>` instance for method chaining.</returns>
    public ApiResponse<T> WithVersion(string version)
    {
        ApiVersion = version;
        return this;
    }

    /// <summary>
    /// Sets the HTTP status code for the response.
    /// </summary>
    /// <param name="statusCode">The HTTP status code (e.g., 200, 404).</param>
    /// <returns>The current `ApiResponse<T>` instance for method chaining.</returns>
    public ApiResponse<T> WithStatusCode(int statusCode)
    {
        Status = statusCode;
        return this;
    }

    /// <summary>
    /// Sets the RFC 7807 Problem Details fields for the response.
    /// </summary>
    /// <param name="type">A URI reference that identifies the problem type.</param>
    /// <param name="title">A short, human-readable summary of the problem type.</param>
    /// <param name="detail">An optional human-readable explanation specific to this occurrence of the problem.</param>
    /// <returns>The current `ApiResponse<T>` instance for method chaining.</returns>
    public ApiResponse<T> WithProblemDetails(string type, string title, string? detail = null)
    {
        Type = type;
        Title = title;
        Detail = detail ?? Message;
        return this;
    }

    /// <summary>
    /// Sets the unique request ID for tracing and debugging purposes.
    /// </summary>
    /// <param name="requestId">The unique request identifier.</param>
    /// <returns>The current `ApiResponse<T>` instance for method chaining.</returns>
    public ApiResponse<T> WithRequestId(string requestId)
    {
        RequestId = requestId;
        return this;
    }

    /// <summary>
    /// Sets the main human-readable message for the response.
    /// </summary>
    /// <param name="message">The message string.</param>
    /// <returns>The current `ApiResponse<T>` instance for method chaining.</returns>
    public ApiResponse<T> WithMessage(string message)
    {
        Message = message;
        return this;
    }
}