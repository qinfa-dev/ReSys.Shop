using System.Text.Json.Serialization;

namespace ReSys.Core.Common.Models.Wrappers.Responses;

/// <summary>
/// A standard API response wrapper that provides a consistent structure for all API responses.
/// It follows industry standards and best practices, including compatibility with RFC 7807 for problem details.
/// </summary>
/// <typeparam name="T">The type of data being returned in the response payload.</typeparam>
/// <remarks>
/// This is a partial class. The implementation is split across several files:
/// - ApiResponse.Factory.cs: Contains static factory methods for creating successful responses (e.g., Success, Created).
/// - ApiResponse.Error.cs: Contains static factory methods for creating error responses (e.g., Error, NotFound, Unauthorized).
/// - ApiResponse.Fluent.cs: Contains fluent methods for building and modifying the response (e.g., WithLink, WithMetadata).
/// - ApiResponse.Helpers.cs: Contains helper methods for generating problem details.
/// </remarks>
public partial class ApiResponse<T>
{
    /// <summary>
    /// Gets or sets a value indicating whether the request was successful.
    /// </summary>
    /// <example>true</example>
    public bool IsSuccess { get; set; }

    /// <summary>
    /// Gets or sets the main data payload of the response.
    /// This will be null for unsuccessful responses.
    /// </summary>
    public T? Data { get; set; }

    /// <summary>
    /// Gets or sets a human-readable message describing the overall result of the operation.
    /// </summary>
    /// <example>Product created successfully.</example>
    public string? Message { get; set; }

    /// <summary>
    /// Gets or sets the RFC 7807 Problem Details 'type' URI reference that identifies the problem type.
    /// This is only present in error responses.
    /// </summary>
    /// <example>https://datatracker.ietf.org/doc/html/rfc7231#section-6.5.4</example>
    [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
    public string? Type { get; set; }

    /// <summary>
    /// Gets or sets the RFC 7807 Problem Details 'title', a short, human-readable summary of the problem type.
    /// This is only present in error responses.
    /// </summary>
    /// <example>Not Found</example>
    [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
    public string? Title { get; set; }

    /// <summary>
    /// Gets or sets the RFC 7807 Problem Details 'status', the HTTP status code for this occurrence of the problem.
    /// </summary>
    /// <example>404</example>
    [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
    public int? Status { get; set; }

    /// <summary>
    /// Gets or sets the RFC 7807 Problem Details 'detail', a human-readable explanation specific to this occurrence of the problem.
    /// </summary>
    /// <example>The product with ID '123' was not found.</example>
    [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
    public string? Detail { get; set; }

    /// <summary>
    /// Gets or sets a dictionary of structured error messages, typically used for validation errors.
    /// The key is the field or category of the error, and the value is an array of error messages.
    /// </summary>
    [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
    public Dictionary<string, string[]>? Errors { get; set; }

    /// <summary>
    /// Gets or sets the timestamp when the response was generated (in UTC).
    /// </summary>
    public DateTimeOffset Timestamp { get; set; } = DateTimeOffset.UtcNow;

    /// <summary>
    /// Gets or sets the API version associated with this response.
    /// </summary>
    /// <example>1.0</example>
    public string ApiVersion { get; set; } = "1.0";

    /// <summary>
    /// Gets or sets a unique identifier for the request, useful for tracing and debugging.
    /// </summary>
    /// <example>0HML9S4S2T4A7:00000001</example>
    [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
    public string? RequestId { get; set; }

    /// <summary>
    /// Gets or sets the pagination information, which is only included for paginated responses.
    /// </summary>
    [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
    public PaginationMetadata? Pagination { get; set; }

    /// <summary>
    /// Gets or sets a dictionary of HATEOAS links for discovering related resources and actions.
    /// </summary>
    [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
    public Dictionary<string, string>? Links { get; set; }

    /// <summary>
    /// Gets or sets a dictionary for any additional metadata that may be relevant to the response.
    /// </summary>
    [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
    public Dictionary<string, object>? Metadata { get; set; }
}