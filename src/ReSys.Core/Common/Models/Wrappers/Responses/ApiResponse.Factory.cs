namespace ReSys.Core.Common.Models.Wrappers.Responses;

public partial class ApiResponse<T>
{
    /// <summary>
    /// Creates a successful 200 OK response with a data payload.
    /// </summary>
    /// <param name="data">The data payload to include in the response.</param>
    /// <param name="message">An optional message to accompany the response.</param>
    /// <param name="requestId">An optional unique identifier for the request.</param>
    /// <returns>A successful `ApiResponse<T>` containing the data.</returns>
    public static ApiResponse<T> Success(T data, string? message = null, string? requestId = null)
    {
        return new ApiResponse<T>
        {
            IsSuccess = true,
            Data = data,
            Message = message,
            RequestId = requestId,
            Status = 200
        };
    }

    /// <summary>
    /// Creates a successful 201 Created response, typically after a resource has been created.
    /// </summary>
    /// <param name="data">The created resource to include in the response payload.</param>
    /// <param name="message">An optional message. Defaults to "Resource created successfully".</param>
    /// <param name="requestId">An optional unique identifier for the request.</param>
    /// <returns>A successful `ApiResponse<T>` indicating resource creation.</returns>
    public static ApiResponse<T> Created(T data, string? message = null, string? requestId = null)
    {
        return new ApiResponse<T>
        {
            IsSuccess = true,
            Data = data,
            Message = message ?? "Resource created successfully",
            RequestId = requestId,
            Status = 201
        };
    }

    /// <summary>
    /// Creates a successful 202 Accepted response, indicating that the request has been accepted for processing.
    /// This is typically used for asynchronous operations.
    /// </summary>
    /// <param name="data">The data payload to include in the response (optional, as processing might not be complete).</param>
    /// <param name="message">An optional message. Defaults to "Request accepted for processing".</param>
    /// <param name="requestId">An optional unique identifier for the request.</param>
    /// <returns>A successful `ApiResponse<T>` indicating the request was accepted.</returns>
    public static ApiResponse<T> Accepted(T? data = default, string? message = null, string? requestId = null)
    {
        return new ApiResponse<T>
        {
            IsSuccess = true,
            Data = data,
            Message = message ?? "Request accepted for processing",
            RequestId = requestId,
            Status = 202
        };
    }

    /// <summary>
    /// Creates a successful 204 No Content response for operations that don't return a body (e.g., delete).
    /// </summary>
    /// <param name="message">An optional message. Defaults to "Operation completed successfully".</param>
    /// <param name="requestId">An optional unique identifier for the request.</param>
    /// <returns>A successful `ApiResponse<T>` with no data payload.</returns>
    public static ApiResponse<T> SuccessWithoutData(string? message = null, string? requestId = null)
    {
        return new ApiResponse<T>
        {
            IsSuccess = true,
            Message = message ?? "Operation completed successfully",
            RequestId = requestId,
            Status = 204
        };
    }

    /// <summary>
    /// Creates a successful 200 OK response for a paginated list of resources.
    /// </summary>
    /// <param name="data">The data payload for the current page.</param>
    /// <param name="pagination">The pagination metadata.</param>
    /// <param name="message">An optional message to accompany the response.</param>
    /// <param name="requestId">An optional unique identifier for the request.</param>
    /// <returns>A successful `ApiResponse<T>` containing a page of data and pagination details.</returns>
    public static ApiResponse<T> Paginated(T data, PaginationMetadata pagination, string? message = null, string? requestId = null)
    {
        return new ApiResponse<T>
        {
            IsSuccess = true,
            Data = data,
            Pagination = pagination,
            Message = message,
            RequestId = requestId,
            Status = 200
        };
    }
}