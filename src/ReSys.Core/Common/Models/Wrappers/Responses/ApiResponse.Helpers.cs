namespace ReSys.Core.Common.Models.Wrappers.Responses;

public partial class ApiResponse<T>
{
    /// <summary>
    /// Gets the Problem Details type URI for a given HTTP status code, conforming to RFC 7807.
    /// </summary>
    /// <param name="statusCode">The HTTP status code (e.g., 400, 404).</param>
    /// <returns>A URI string identifying the problem type.</returns>
    public static string GetProblemTypeUri(int statusCode) => statusCode switch
    {
        400 => "https://datatracker.ietf.org/doc/html/rfc7231#section-6.5.1",    // Bad Request
        401 => "https://datatracker.ietf.org/doc/html/rfc7235#section-3.1",    // Unauthorized
        403 => "https://datatracker.ietf.org/doc/html/rfc7231#section-6.5.3",    // Forbidden
        404 => "https://datatracker.ietf.org/doc/html/rfc7231#section-6.5.4",    // Not Found
        405 => "https://datatracker.ietf.org/doc/html/rfc7231#section-6.5.5",    // Method Not Allowed
        409 => "https://datatracker.ietf.org/doc/html/rfc7231#section-6.5.8",    // Conflict
        410 => "https://datatracker.ietf.org/doc/html/rfc7231#section-6.5.9",    // Gone
        422 => "https://datatracker.ietf.org/doc/html/rfc4918#section-11.2",   // Unprocessable Entity
        429 => "https://datatracker.ietf.org/doc/html/rfc6585#section-4",     // Too Many Requests
        500 => "https://datatracker.ietf.org/doc/html/rfc7231#section-6.6.1",    // Internal Server Error
        501 => "https://datatracker.ietf.org/doc/html/rfc7231#section-6.6.2",    // Not Implemented
        503 => "https://datatracker.ietf.org/doc/html/rfc7231#section-6.6.4",    // Service Unavailable
        _ => $"https://httpstatuses.com/{statusCode}"
    };

    /// <summary>
    /// Gets the Problem Details title for a given HTTP status code, conforming to RFC 7807.
    /// </summary>
    /// <param name="statusCode">The HTTP status code (e.g., 400, 404).</param>
    /// <returns>A short, human-readable summary of the problem type.</returns>
    public static string GetProblemTitle(int statusCode) => statusCode switch
    {
        400 => "Bad Request",
        401 => "Unauthorized",
        403 => "Forbidden",
        404 => "Not Found",
        405 => "Method Not Allowed",
        409 => "Conflict",
        410 => "Gone",
        422 => "Unprocessable Entity",
        429 => "Too Many Requests",
        500 => "Internal Server Error",
        501 => "Not Implemented",
        503 => "Service Unavailable",
        _ => "Error"
    };
}