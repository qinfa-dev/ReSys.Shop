using System.Collections.Frozen;

using ErrorOr;

using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Routing;

using ReSys.Core.Common.Models.Wrappers.PagedLists;

namespace ReSys.Core.Common.Models.Wrappers.Responses;

/// <summary>
/// Extension methods for converting ErrorOr results to standardized ApiResponse format.
/// Provides seamless integration between ErrorOr library and the ApiResponse wrapper with proper HTTP status codes.
/// </summary>
/// <remarks>
/// This class focuses specifically on converting ErrorOr results to the standardized ApiResponse format
/// while preserving HTTP status codes for proper client handling.
/// 
/// Key Features:
/// - Automatic error-to-HTTP status code mapping preserved in ApiResponse.StatusCode
/// - Consistent ApiResponse wrapper for all API responses
/// - Support for pagination with PaginationMetadata
/// - HATEOAS links support
/// - Additional metadata support
/// - E-commerce specific response types (Product, Cart, etc.)
/// - Performance optimized with frozen dictionaries
/// </remarks>
/// <example>
/// Basic usage in Controllers:
/// <code>
/// [HttpGet("{id:int}")]
/// public async Task&lt;ActionResult&lt;ApiResponse&lt;Product&gt;&gt;&gt; GetProduct(int id)
/// {
///     var result = await _productService.GetProductAsync(id);
///     var apiResponse = result.ToApiResponse("Product retrieved successfully");
///     return Ok(apiResponse); // Always returns 200 OK with ApiResponse wrapper
/// }
/// </code>
/// </example>
public static class ErrorOrApiResponseExtensions
{
    // Pre-computed frozen dictionary for better performance
    private static readonly FrozenDictionary<ErrorType, int> ErrorTypeToStatusCode =
        new Dictionary<ErrorType, int>
        {
            [key: ErrorType.Validation] = StatusCodes.Status400BadRequest,
            [key: ErrorType.Unauthorized] = StatusCodes.Status401Unauthorized,
            [key: ErrorType.Forbidden] = StatusCodes.Status403Forbidden,
            [key: ErrorType.NotFound] = StatusCodes.Status404NotFound,
            [key: ErrorType.Conflict] = StatusCodes.Status409Conflict,
            [key: ErrorType.Failure] = StatusCodes.Status500InternalServerError,
            [key: ErrorType.Unexpected] = StatusCodes.Status422UnprocessableEntity
        }.ToFrozenDictionary();

    private const int DefaultStatusCode = StatusCodes.Status500InternalServerError;

    #region Core ApiResponse Extensions

    /// <summary>
    /// Converts an ErrorOr&lt;T&gt; result to an ApiResponse&lt;T&gt; with preserved HTTP status codes.
    /// Returns a successful ApiResponse with data on success or error ApiResponse on failure with appropriate StatusCode.
    /// </summary>
    /// <typeparam name="T">The type of the success result</typeparam>
    /// <param name="result">The ErrorOr result to convert</param>
    /// <param name="message">Optional success message</param>
    /// <param name="requestId">Optional command identifier for tracing</param>
    /// <returns>ApiResponse&lt;T&gt; representing either success or error with proper StatusCode</returns>
    /// <example>
    /// <code>
    /// public async Task&lt;ActionResult&lt;ApiResponse&lt;Product&gt;&gt;&gt; GetProduct(int id)
    /// {
    ///     var result = await _productService.GetProductAsync(id);
    ///     var apiResponse = result.ToApiResponse("Product retrieved successfully");
    ///     return Ok(apiResponse); // StatusCode preserved in apiResponse.StatusCode
    /// }
    /// </code>
    /// </example>
    public static ApiResponse<T> ToApiResponse<T>(this ErrorOr<T> result, string? message = null, string? requestId = null)
        => result.Match(
            onValue: value => ApiResponse<T>.Success(data: value,
                message: message,
                requestId: requestId),
            onError: errors => ConvertErrorsToApiResponse<T>(errors: errors,
                requestId: requestId));

    /// <summary>
    /// Converts an ErrorOr&lt;T&gt; result to an ApiResponse&lt;T&gt; with Created status (201).
    /// Returns a successful Created ApiResponse with data on success or error ApiResponse on failure with appropriate StatusCode.
    /// </summary>
    /// <typeparam name="T">The type of the created resource</typeparam>
    /// <param name="result">The ErrorOr result to convert</param>
    /// <param name="message">Optional creation message</param>
    /// <param name="requestId">Optional command identifier for tracing</param>
    /// <returns>ApiResponse&lt;T&gt; with Created status (201) or error details with appropriate StatusCode</returns>
    /// <example>
    /// <code>
    /// public async Task&lt;ActionResult&lt;ApiResponse&lt;Product&gt;&gt;&gt; CreateProduct(CreateProductRequest command)
    /// {
    ///     var result = await _productService.CreateProductAsync(command);
    ///     var apiResponse = result.ToApiResponseCreated("Product created successfully");
    ///     return Ok(apiResponse); // apiResponse.StatusCode will be 201 on success
    /// }
    /// </code>
    /// </example>
    public static ApiResponse<T> ToApiResponseCreated<T>(this ErrorOr<T> result, string? message = null, string? requestId = null)
        => result.Match(
            onValue: value => ApiResponse<T>.Created(data: value,
                message: message,
                requestId: requestId),
            onError: errors => ConvertErrorsToApiResponse<T>(errors: errors,
                requestId: requestId));

    /// <summary>
    /// Converts an ErrorOr&lt;Updated&gt; result to a non-generic ApiResponse with appropriate StatusCode.
    /// Returns a successful no-data ApiResponse on success or error ApiResponse on failure with preserved StatusCode.
    /// </summary>
    /// <param name="result">The ErrorOr&lt;Updated&gt; result to convert</param>
    /// <param name="message">Optional success message</param>
    /// <param name="requestId">Optional command identifier for tracing</param>
    /// <returns>ApiResponse representing either success (200) or error with appropriate StatusCode</returns>
    /// <example>
    /// <code>
    /// public async Task&lt;ActionResult&lt;ApiResponse&gt;&gt; UpdateProduct(int id, UpdateProductRequest command)
    /// {
    ///     var result = await _productService.UpdateProductAsync(id, command);
    ///     var apiResponse = result.ToApiResponseUpdated("Product updated successfully");
    ///     return Ok(apiResponse); // apiResponse.StatusCode will be 200 on success
    /// }
    /// </code>
    /// </example>
    public static ApiResponse ToApiResponseUpdated(this ErrorOr<Updated> result, string? message = null, string? requestId = null)
        => result.Match(
            onValue: _ => ApiResponse.Success(message: message ?? "Resource updated successfully",
                requestId: requestId),
            onError: errors => ConvertErrorsToApiResponse(errors: errors,
                requestId: requestId));

    /// <summary>
    /// Converts an ErrorOr&lt;Deleted&gt; result to a non-generic ApiResponse with appropriate StatusCode.
    /// Returns a successful no-data ApiResponse on success or error ApiResponse on failure with preserved StatusCode.
    /// </summary>
    /// <param name="result">The ErrorOr&lt;Deleted&gt; result to convert</param>
    /// <param name="message">Optional success message</param>
    /// <param name="requestId">Optional command identifier for tracing</param>
    /// <returns>ApiResponse representing either success (200) or error with appropriate StatusCode</returns>
    /// <example>
    /// <code>
    /// public async Task&lt;ActionResult&lt;ApiResponse&gt;&gt; DeleteProduct(int id)
    /// {
    ///     var result = await _productService.DeleteProductAsync(id);
    ///     var apiResponse = result.ToApiResponseDeleted("Product deleted successfully");
    ///     return Ok(apiResponse); // apiResponse.StatusCode will be 200 on success
    /// }
    /// </code>
    /// </example>
    public static ApiResponse ToApiResponseDeleted(this ErrorOr<Deleted> result, string? message = null, string? requestId = null)
        => result.Match(
            onValue: _ => ApiResponse.Success(message: message ?? "Resource deleted successfully",
                requestId: requestId),
            onError: errors => ConvertErrorsToApiResponse(errors: errors,
                requestId: requestId));

    #endregion

    #region Advanced ApiResponse Extensions

    /// <summary>
    /// Converts an ErrorOr&lt;PagedList&lt;T&gt;&gt; result to an ApiResponse&lt;List&lt;T&gt;&gt; with pagination metadata and appropriate StatusCode.
    /// Returns a successful paginated ApiResponse on success or error ApiResponse on failure with preserved StatusCode.
    /// </summary>
    /// <typeparam name="T">The type of items in the paged list</typeparam>
    /// <param name="result">The ErrorOr&lt;PagedList&lt;T&gt;&gt; result to convert</param>
    /// <param name="message">Optional success message</param>
    /// <param name="requestId">Optional command identifier for tracing</param>
    /// <returns>ApiResponse&lt;List&lt;T&gt;&gt; with pagination metadata or error details with appropriate StatusCode</returns>
    /// <example>
    /// <code>
    /// public async Task&lt;ActionResult&lt;ApiResponse&lt;List&lt;Product&gt;&gt;&gt;&gt; GetProducts(int page, int pageSize)
    /// {
    ///     var result = await _productService.GetProductsPagedAsync(page, pageSize);
    ///     var apiResponse = result.ToApiResponsePaged("Products retrieved successfully");
    ///     return Ok(apiResponse); // Includes pagination metadata
    /// }
    /// </code>
    /// </example>
    public static ApiResponse<List<T>> ToApiResponsePaged<T>(this ErrorOr<PagedList<T>> result, string? message = null, string? requestId = null)
        => result.Match(
            onValue: pagedList => ApiResponse<List<T>>.Paginated(
                data: pagedList.Items.ToList(),
                pagination: PaginationMetadata.FromPagedList(pagedList: pagedList),
                message: message,
                requestId: requestId),
            onError: errors => ConvertErrorsToApiResponse<List<T>>(errors: errors,
                requestId: requestId));

    /// <summary>
    /// Converts an ErrorOr&lt;T&gt; result to an ApiResponse&lt;T&gt; with additional HATEOAS links and appropriate StatusCode.
    /// Returns a successful ApiResponse with links on success or error ApiResponse on failure with preserved StatusCode.
    /// </summary>
    /// <typeparam name="T">The type of the success result</typeparam>
    /// <param name="result">The ErrorOr result to convert</param>
    /// <param name="links">Dictionary of HATEOAS links to include</param>
    /// <param name="message">Optional success message</param>
    /// <param name="requestId">Optional command identifier for tracing</param>
    /// <returns>ApiResponse&lt;T&gt; with HATEOAS links or error details with appropriate StatusCode</returns>
    /// <example>
    /// <code>
    /// public async Task&lt;ActionResult&lt;ApiResponse&lt;Product&gt;&gt;&gt; GetProductWithLinks(int id)
    /// {
    ///     var result = await _productService.GetProductAsync(id);
    ///     var links = new Dictionary&lt;string, string&gt;
    ///     {
    ///         ["self"] = $"/api/products/{id}",
    ///         ["edit"] = $"/api/products/{id}",
    ///         ["delete"] = $"/api/products/{id}"
    ///     };
    ///     var apiResponse = result.ToApiResponseWithLinks(links, "Product retrieved with links");
    ///     return Ok(apiResponse);
    /// }
    /// </code>
    /// </example>
    public static ApiResponse<T> ToApiResponseWithLinks<T>(this ErrorOr<T> result, Dictionary<string, string> links, string? message = null, string? requestId = null)
        => result.Match(
            onValue: value => ApiResponse<T>.Success(data: value,
                message: message,
                requestId: requestId).WithLinks(links: links),
            onError: errors => ConvertErrorsToApiResponse<T>(errors: errors,
                requestId: requestId));

    /// <summary>
    /// Converts an ErrorOr&lt;T&gt; result to an ApiResponse&lt;T&gt; with additional metadata and appropriate StatusCode.
    /// Returns a successful ApiResponse with metadata on success or error ApiResponse on failure with preserved StatusCode.
    /// </summary>
    /// <typeparam name="T">The type of the success result</typeparam>
    /// <param name="result">The ErrorOr result to convert</param>
    /// <param name="metadata">Dictionary of metadata to include</param>
    /// <param name="message">Optional success message</param>
    /// <param name="requestId">Optional command identifier for tracing</param>
    /// <returns>ApiResponse&lt;T&gt; with metadata or error details with appropriate StatusCode</returns>
    /// <example>
    /// <code>
    /// public async Task&lt;ActionResult&lt;ApiResponse&lt;Product&gt;&gt;&gt; GetProductWithMetadata(int id)
    /// {
    ///     var result = await _productService.GetProductAsync(id);
    ///     var metadata = new Dictionary&lt;string, object&gt;
    ///     {
    ///         ["cached"] = true,
    ///         ["cacheExpiry"] = DateTime.UtcNow.AddMinutes(15),
    ///         ["version"] = "1.2.0"
    ///     };
    ///     var apiResponse = result.ToApiResponseWithMetadata(metadata, "Product retrieved with metadata");
    ///     return Ok(apiResponse);
    /// }
    /// </code>
    /// </example>
    public static ApiResponse<T> ToApiResponseWithMetadata<T>(this ErrorOr<T> result, Dictionary<string, object> metadata, string? message = null, string? requestId = null)
        => result.Match(
            onValue: value =>
            {
                ApiResponse<T> response = ApiResponse<T>.Success(data: value,
                    message: message,
                    requestId: requestId);
                foreach ((string key, object val) in metadata)
                {
                    response.WithMetadata(key: key,
                        value: val);
                }
                return response;
            },
            onError: errors => ConvertErrorsToApiResponse<T>(errors: errors,
                requestId: requestId));

    #endregion

    #region Minimal API Integration

    /// <summary>
    /// Converts an ErrorOr&lt;T&gt; result to an IResult with ApiResponse wrapper for minimal APIs with appropriate StatusCode.
    /// Returns Ok(ApiResponse) on success or error ApiResponse wrapped in Ok with StatusCode preserved in the response.
    /// </summary>
    /// <typeparam name="T">The type of the success result</typeparam>
    /// <param name="result">The ErrorOr result to convert</param>
    /// <param name="message">Optional success message</param>
    /// <param name="requestId">Optional command identifier for tracing</param>
    /// <returns>IResult with ApiResponse wrapper and preserved StatusCode</returns>
    /// <example>
    /// <code>
    /// app.MapGet("/products/{id}", async (int id, IProductService service) =>
    /// {
    ///     var result = await service.GetProductAsync(id);
    ///     return result.ToTypedApiResponse("Product retrieved successfully");
    ///     // Always returns 200 OK, but StatusCode is preserved in apiResponse.StatusCode
    /// });
    /// </code>
    /// </example>
    public static IResult ToTypedApiResponse<T>(this ErrorOr<T> result, string? message = null, string? requestId = null)
    {
        ApiResponse<T> apiResponse = result.ToApiResponse(message: message,
            requestId: requestId);
        return TypedResults.Ok(value: apiResponse);
    }

    /// <summary>
    /// Converts an ErrorOr&lt;T&gt; result to an IResult with ApiResponse wrapper for minimal APIs with Created response.
    /// Returns Ok(ApiResponse) with Created status preserved in ApiResponse.StatusCode.
    /// </summary>
    /// <typeparam name="T">The type of the created resource</typeparam>
    /// <param name="result">The ErrorOr result to convert</param>
    /// <param name="message">Optional creation message</param>
    /// <param name="requestId">Optional command identifier for tracing</param>
    /// <returns>IResult with ApiResponse wrapper and Created status</returns>
    /// <example>
    /// <code>
    /// app.MapPost("/products", async (CreateProductRequest command, IProductService service) =>
    /// {
    ///     var result = await service.CreateProductAsync(command);
    ///     return result.ToTypedApiResponseCreated("Product created successfully");
    ///     // Returns 200 OK, but apiResponse.StatusCode will be 201
    /// });
    /// </code>
    /// </example>
    public static IResult ToTypedApiResponseCreated<T>(this ErrorOr<T> result, string? message = null, string? requestId = null)
    {
        ApiResponse<T> apiResponse = result.ToApiResponseCreated(message: message,
            requestId: requestId);
        return TypedResults.Ok(value: apiResponse);
    }

    #endregion

    #region Private Helper Methods

    private static ApiResponse<T> ConvertErrorsToApiResponse<T>(IReadOnlyList<Error> errors, string? requestId = null)
    {
        if (errors.Count == 0)
            return ApiResponse<T>.Error(errors: new Dictionary<string, string[]>
                {
                    [key: "General"] =
                    [
                        "An unknown error occurred"
                    ]
                },
                message: "An unknown error occurred",
                requestId: requestId);

        Error firstError = errors[index: 0];
        GetStatusCode(type: firstError.Type);

        // Group errors by full error code (not just category)
        Dictionary<string, string[]> errorGroups = errors
            .GroupBy(keySelector: e => GetErrorCode(error: e))
            .ToDictionary(
                keySelector: g => g.Key,
                elementSelector: g => g.Select(selector: e => e.Description).ToArray() // Use only description, not formatted message
            );

        return firstError.Type switch
        {
            ErrorType.Validation => CreateValidationApiResponse<T>(errorGroups: errorGroups,
                requestId: requestId),
            ErrorType.NotFound => CreateNotFoundApiResponse<T>(error: firstError,
                requestId: requestId),
            ErrorType.Unauthorized => CreateUnauthorizedApiResponse<T>(error: firstError,
                requestId: requestId),
            ErrorType.Conflict => CreateConflictApiResponse<T>(firstError: firstError,
                errorGroups: errorGroups,
                requestId: requestId),
            ErrorType.Forbidden => CreateForbiddenApiResponse<T>(firstError: firstError,
                errorGroups: errorGroups,
                requestId: requestId),
            ErrorType.Failure => CreateFailureApiResponse<T>(firstError: firstError,
                errorGroups: errorGroups,
                requestId: requestId),
            ErrorType.Unexpected => CreateUnexpectedApiResponse<T>(firstError: firstError,
                errorGroups: errorGroups,
                requestId: requestId),
            _ => CreateGenericErrorApiResponse<T>(firstError: firstError,
                errorGroups: errorGroups,
                requestId: requestId)
        };
    }

    private static ApiResponse ConvertErrorsToApiResponse(IReadOnlyList<Error> errors, string? requestId = null)
    {
        if (errors.Count == 0)
            return ApiResponse.Error(errors: new Dictionary<string, string[]>
                {
                    [key: "General"] =
                    [
                        "An unknown error occurred"
                    ]
                },
                message: "An unknown error occurred",
                requestId: requestId);

        Error firstError = errors[index: 0];

        // Group errors by full error code (not just category)
        Dictionary<string, string[]> errorGroups = errors
            .GroupBy(keySelector: e => GetErrorCode(error: e))
            .ToDictionary(
                keySelector: g => g.Key,
                elementSelector: g => g.Select(selector: e => e.Description).ToArray() // Use only description, not formatted message
            );

        return firstError.Type switch
        {
            ErrorType.NotFound => CreateNotFoundApiResponse(error: firstError,
                requestId: requestId),
            ErrorType.Unauthorized => CreateUnauthorizedApiResponse(error: firstError,
                requestId: requestId),
            _ => CreateGenericErrorApiResponse(firstError: firstError,
                errorGroups: errorGroups,
                requestId: requestId)
        };
    }

    #region Specific Error Result Creators

    private static ApiResponse<T> CreateValidationApiResponse<T>(Dictionary<string, string[]> errorGroups, string? requestId)
    {
        return new ApiResponse<T>
        {
            IsSuccess = false,
            Errors = errorGroups,
            Message = "Validation failed",
            Type = "https://tools.ietf.org/html/rfc7231#section-6.5.1",
            Title = "Validation Failed",
            Status = 422,
            Detail = "One or more validation errors occurred",
            RequestId = requestId,
            Timestamp = DateTimeOffset.UtcNow
        };
    }

    private static ApiResponse<T> CreateNotFoundApiResponse<T>(Error error, string? requestId)
    {
        string title = GetErrorCode(error: error);
        string detail = error.Description;

        return new ApiResponse<T>
        {
            IsSuccess = false,
            Message = detail,
            Type = "https://httpstatuses.com/404",
            Title = title,
            Status = 404,
            Detail = detail,
            RequestId = requestId,
            Timestamp = DateTimeOffset.UtcNow
        };
    }

    private static ApiResponse CreateNotFoundApiResponse(Error error, string? requestId)
    {
        string title = GetErrorCode(error: error);
        string detail = error.Description;

        return new ApiResponse
        {
            IsSuccess = false,
            Message = detail,
            Type = "https://httpstatuses.com/404",
            Title = title,
            Status = 404,
            Detail = detail,
            RequestId = requestId,
            Timestamp = DateTimeOffset.UtcNow
        };
    }

    private static ApiResponse<T> CreateUnauthorizedApiResponse<T>(Error error, string? requestId)
    {
        string title = GetErrorCode(error: error);
        string detail = error.Description;

        return new ApiResponse<T>
        {
            IsSuccess = false,
            Message = detail,
            Type = "https://httpstatuses.com/401",
            Title = title,
            Status = 401,
            Detail = detail,
            RequestId = requestId,
            Timestamp = DateTimeOffset.UtcNow
        };
    }

    private static ApiResponse CreateUnauthorizedApiResponse(Error error, string? requestId)
    {
        string title = GetErrorCode(error: error);
        string detail = error.Description;

        return new ApiResponse
        {
            IsSuccess = false,
            Message = detail,
            Type = "https://httpstatuses.com/401",
            Title = title,
            Status = 401,
            Detail = detail,
            RequestId = requestId,
            Timestamp = DateTimeOffset.UtcNow
        };
    }

    private static ApiResponse<T> CreateConflictApiResponse<T>(Error firstError, Dictionary<string, string[]> errorGroups, string? requestId)
    {
        string title = GetErrorCode(error: firstError);
        string detail = firstError.Description;

        return new ApiResponse<T>
        {
            IsSuccess = false,
            Errors = errorGroups,
            Message = "A conflict occurred",
            Type = "https://httpstatuses.com/409",
            Title = title,
            Status = 409,
            Detail = detail,
            RequestId = requestId,
            Timestamp = DateTimeOffset.UtcNow
        };
    }

    private static ApiResponse<T> CreateForbiddenApiResponse<T>(Error firstError, Dictionary<string, string[]> errorGroups, string? requestId)
    {
        string title = GetErrorCode(error: firstError);
        string detail = firstError.Description;

        return new ApiResponse<T>
        {
            IsSuccess = false,
            Errors = errorGroups,
            Message = "Access forbidden",
            Type = "https://httpstatuses.com/403",
            Title = title,
            Status = 403,
            Detail = detail,
            RequestId = requestId,
            Timestamp = DateTimeOffset.UtcNow
        };
    }

    private static ApiResponse<T> CreateFailureApiResponse<T>(Error firstError, Dictionary<string, string[]> errorGroups, string? requestId)
    {
        string title = GetErrorCode(error: firstError);
        string detail = firstError.Description;

        return new ApiResponse<T>
        {
            IsSuccess = false,
            Errors = errorGroups,
            Message = "An internal error occurred",
            Type = "https://httpstatuses.com/500",
            Title = title,
            Status = 500,
            Detail = detail,
            RequestId = requestId,
            Timestamp = DateTimeOffset.UtcNow
        };
    }

    private static ApiResponse<T> CreateUnexpectedApiResponse<T>(Error firstError, Dictionary<string, string[]> errorGroups, string? requestId)
    {
        string title = GetErrorCode(error: firstError);
        string detail = firstError.Description;

        return new ApiResponse<T>
        {
            IsSuccess = false,
            Errors = errorGroups,
            Message = "An unexpected error occurred",
            Type = "https://tools.ietf.org/html/rfc7231#section-6.5.22",
            Title = title,
            Status = 422,
            Detail = detail,
            RequestId = requestId,
            Timestamp = DateTimeOffset.UtcNow
        };
    }

    private static ApiResponse<T> CreateGenericErrorApiResponse<T>(Error firstError, Dictionary<string, string[]> errorGroups, string? requestId)
    {
        int statusCode = GetStatusCode(type: firstError.Type);
        string title = GetErrorCode(error: firstError);
        string detail = firstError.Description;

        return new ApiResponse<T>
        {
            IsSuccess = false,
            Errors = errorGroups,
            Message = "An error occurred",
            Type = $"https://httpstatuses.com/{statusCode}",
            Title = title,
            Status = statusCode,
            Detail = detail,
            RequestId = requestId,
            Timestamp = DateTimeOffset.UtcNow
        };
    }

    private static ApiResponse CreateGenericErrorApiResponse(Error firstError, Dictionary<string, string[]> errorGroups, string? requestId)
    {
        int statusCode = GetStatusCode(type: firstError.Type);
        string title = GetErrorCode(error: firstError);
        string detail = firstError.Description;

        return new ApiResponse
        {
            IsSuccess = false,
            Errors = errorGroups,
            Message = "An error occurred",
            Type = $"https://httpstatuses.com/{statusCode}",
            Title = title,
            Status = statusCode,
            Detail = detail,
            RequestId = requestId,
            Timestamp = DateTimeOffset.UtcNow
        };
    }

    #endregion

    /// <summary>
    /// Gets the full error code to use as the key in errors dictionary.
    /// If no code is provided, falls back to error type.
    /// Examples: "Role.AlreadyExists" -> "Role.AlreadyExists", "" -> "Validation"
    /// </summary>
    private static string GetErrorCode(Error error)
    {
        if (string.IsNullOrWhiteSpace(value: error.Code))
            return error.Type.ToString();

        return error.Code;
    }

    private static int GetStatusCode(ErrorType type)
        => ErrorTypeToStatusCode.GetValueOrDefault(key: type,
            defaultValue: DefaultStatusCode);

    #endregion
}

#region Usage Examples

/// <summary>
/// Example service layer that returns ErrorOr results for ApiResponse conversion
/// </summary>
public sealed class ApiResponseWrappedExampleService
{
    public async Task<ErrorOr<OrderRecord>> GetOrderRecordByIdAsync(int id)
    {
        if (id <= 0)
            return Error.Validation(code: "OrderRecord.InvalidId",
                description: "OrderRecord ID must be greater than 0");

        OrderRecord? order = await FindOrderRecordInDatabaseAsync(id: id);
        if (order == null)
            return Error.NotFound(code: "OrderRecord.NotFound",
                description: $"OrderRecord with ID {id} was not found");

        return order;
    }

    public async Task<ErrorOr<OrderRecord>> CreateOrderRecordAsync(CreateOrderRecordRequest request)
    {
        List<Error> validationErrors = ValidateCreateOrderRecordRequest(request: request);
        if (validationErrors.Any())
            return validationErrors;

        Customer? customer = await FindCustomerAsync(customerId: request.CustomerId);
        if (customer == null)
            return Error.NotFound(code: "Customer.NotFound",
                description: "Customer not found");

        OrderRecord order = new(customerId: request.CustomerId,
            items: request.Items);
        await SaveOrderRecordAsync(order: order);
        return order;
    }

    public async Task<ErrorOr<Updated>> UpdateOrderRecordStatusAsync(int id, string status)
    {
        ErrorOr<OrderRecord> getOrderRecordResult = await GetOrderRecordByIdAsync(id: id);
        if (getOrderRecordResult.IsError)
            return getOrderRecordResult.Errors;

        OrderRecord order = getOrderRecordResult.Value;
        order.UpdateStatus(status: status);
        await SaveOrderRecordAsync(order: order);
        return Result.Updated;
    }

    public async Task<ErrorOr<Deleted>> CancelOrderRecordAsync(int id)
    {
        ErrorOr<OrderRecord> getOrderRecordResult = await GetOrderRecordByIdAsync(id: id);
        if (getOrderRecordResult.IsError)
            return getOrderRecordResult.Errors;

        OrderRecord order = getOrderRecordResult.Value;
        if (order.Status == "Shipped")
            return Error.Conflict(code: "OrderRecord.CannotCancel",
                description: "Cannot cancel a shipped order");

        await DeleteOrderRecordAsync(id: id);
        return Result.Deleted;
    }

    public async Task<ErrorOr<PagedList<OrderRecord>>> GetOrderRecordsPagedAsync(int page, int pageSize)
    {
        if (page <= 0)
            return Error.Validation(code: "Pagination.InvalidPage",
                description: "Page number must be greater than 0");

        if (pageSize <= 0 || pageSize > 100)
            return Error.Validation(code: "Pagination.InvalidPageSize",
                description: "Page size must be between 1 and 100");

        PagedList<OrderRecord> orders = await GetOrderRecordsFromDatabaseAsync(page: page,
            pageSize: pageSize);
        return orders;
    }

    private List<Error> ValidateCreateOrderRecordRequest(CreateOrderRecordRequest request)
    {
        List<Error> errors = [];

        if (request.CustomerId <= 0)
            errors.Add(item: Error.Validation(code: "Customer.InvalidId",
                description: "Customer ID is required"));

        if (request.Items == null || !request.Items.Any())
            errors.Add(item: Error.Validation(code: "OrderRecord.ItemsRequired",
                description: "At least one order item is required"));

        return errors;
    }

    // Placeholder methods - implement with your actual data access
    private Task<OrderRecord?> FindOrderRecordInDatabaseAsync(int id) => throw new NotImplementedException();
    private Task<Customer?> FindCustomerAsync(int customerId) => throw new NotImplementedException();
    private Task SaveOrderRecordAsync(OrderRecord order) => throw new NotImplementedException();
    private Task DeleteOrderRecordAsync(int id) => throw new NotImplementedException();
    private Task<PagedList<OrderRecord>> GetOrderRecordsFromDatabaseAsync(int page, int pageSize) => throw new NotImplementedException();
}

/// <summary>
/// Comprehensive MVC Controller using ErrorOr ApiResponse extensions
/// </summary>
[ApiController]
[Route(template: "api/[controller]")]
[Produces(contentType: "application/json")]
internal sealed class OrderRecordsApiResponseController(ApiResponseWrappedExampleService orderService) : ControllerBase
{
    /// <summary>
    /// Get order by ID - Returns ApiResponse wrapper with preserved status codes.
    /// </summary>
    /// <param name="id">The order ID</param>
    /// <returns>ApiResponse containing order details</returns>
    /// <response code="200">Always returns 200 OK with ApiResponse wrapper</response>
    [HttpGet(template: "{id:int}")]
    [ProducesResponseType(type: typeof(ApiResponse<OrderRecord>), statusCode: StatusCodes.Status200OK)]
    public async Task<ActionResult<ApiResponse<OrderRecord>>> GetOrderRecord(int id)
    {
        ErrorOr<OrderRecord> result = await orderService.GetOrderRecordByIdAsync(id: id);
        ApiResponse<OrderRecord> apiResponse = result.ToApiResponse(message: "OrderRecord retrieved successfully");

        // Note: Always returns 200 OK, but the actual status is in apiResponse.Status
        // For a 404 error, apiResponse.Status will be 404, but HTTP response is 200
        return Ok(value: apiResponse);
    }

    /// <summary>
    /// Create a new order - Returns ApiResponse wrapper with Created status preserved
    /// </summary>
    /// <param name="request">OrderRecord creation command</param>
    /// <returns>ApiResponse containing created order</returns>
    /// <response code="200">Always returns 200 OK with ApiResponse wrapper</response>
    [HttpPost]
    [ProducesResponseType(type: typeof(ApiResponse<OrderRecord>), statusCode: StatusCodes.Status200OK)]
    public async Task<ActionResult<ApiResponse<OrderRecord>>> CreateOrderRecord(CreateOrderRecordRequest request)
    {
        ErrorOr<OrderRecord> result = await orderService.CreateOrderRecordAsync(request: request);
        ApiResponse<OrderRecord> apiResponse = result.ToApiResponseCreated(message: "OrderRecord created successfully");

        // Note: Returns 200 OK, but apiResponse.Status will be 201 on success
        return Ok(value: apiResponse);
    }

    /// <summary>
    /// Update order status - Returns ApiResponse wrapper without data
    /// </summary>
    /// <param name="id">The order ID</param>
    /// <param name="status">New order status</param>
    /// <returns>ApiResponse without data</returns>
    /// <response code="200">Always returns 200 OK with ApiResponse wrapper</response>
    [HttpPatch(template: "{id:int}/status")]
    [ProducesResponseType(type: typeof(ApiResponse), statusCode: StatusCodes.Status200OK)]
    public async Task<ActionResult<ApiResponse>> UpdateOrderRecordStatus(int id, [FromBody] string status)
    {
        ErrorOr<Updated> result = await orderService.UpdateOrderRecordStatusAsync(id: id,
            status: status);
        ApiResponse apiResponse = result.ToApiResponseUpdated(message: "OrderRecord status updated successfully");

        return Ok(value: apiResponse);
    }

    /// <summary>
    /// Cancel an order - Returns ApiResponse wrapper without data
    /// </summary>
    /// <param name="id">The order ID</param>
    /// <returns>ApiResponse without data</returns>
    /// <response code="200">Always returns 200 OK with ApiResponse wrapper</response>
    [HttpDelete(template: "{id:int}")]
    [ProducesResponseType(type: typeof(ApiResponse), statusCode: StatusCodes.Status200OK)]
    public async Task<ActionResult<ApiResponse>> CancelOrderRecord(int id)
    {
        ErrorOr<Deleted> result = await orderService.CancelOrderRecordAsync(id: id);
        ApiResponse apiResponse = result.ToApiResponseDeleted(message: "OrderRecord cancelled successfully");

        return Ok(value: apiResponse);
    }

    /// <summary>
    /// Get paginated orders with pagination metadata
    /// </summary>
    /// <param name="page">Page number (1-based)</param>
    /// <param name="pageSize">Number of items per page</param>
    /// <returns>ApiResponse with paginated orders and metadata</returns>
    /// <response code="200">Always returns 200 OK with ApiResponse wrapper</response>
    [HttpGet]
    [ProducesResponseType(type: typeof(ApiResponse<List<OrderRecord>>), statusCode: StatusCodes.Status200OK)]
    public async Task<ActionResult<ApiResponse<List<OrderRecord>>>> GetOrderRecords([FromQuery] int page = 1, [FromQuery] int pageSize = 10)
    {
        ErrorOr<PagedList<OrderRecord>> result = await orderService.GetOrderRecordsPagedAsync(page: page,
            pageSize: pageSize);
        ApiResponse<List<OrderRecord>> apiResponse = result.ToApiResponsePaged(message: "OrderRecords retrieved successfully");

        return Ok(value: apiResponse);
    }

    /// <summary>
    /// Get order with HATEOAS links
    /// </summary>
    /// <param name="id">The order ID</param>
    /// <returns>ApiResponse with order and navigation links</returns>
    /// <response code="200">Always returns 200 OK with ApiResponse wrapper</response>
    [HttpGet(template: "{id:int}/with-links")]
    [ProducesResponseType(type: typeof(ApiResponse<OrderRecord>), statusCode: StatusCodes.Status200OK)]
    public async Task<ActionResult<ApiResponse<OrderRecord>>> GetOrderRecordWithLinks(int id)
    {
        ErrorOr<OrderRecord> result = await orderService.GetOrderRecordByIdAsync(id: id);
        Dictionary<string, string> links = new()
        {
            [key: "self"] = $"/api/orders/{id}",
            [key: "update-status"] = $"/api/orders/{id}/status",
            [key: "cancel"] = $"/api/orders/{id}",
            [key: "customer"] = $"/api/customers/{result.Value?.CustomerId}"
        };

        ApiResponse<OrderRecord> apiResponse = result.ToApiResponseWithLinks(links: links,
            message: "OrderRecord retrieved with navigation links");
        return Ok(value: apiResponse);
    }

    /// <summary>
    /// Get order with additional metadata
    /// </summary>
    /// <param name="id">The order ID</param>
    /// <returns>ApiResponse with order and metadata</returns>
    /// <response code="200">Always returns 200 OK with ApiResponse wrapper</response>
    [HttpGet(template: "{id:int}/with-metadata")]
    [ProducesResponseType(type: typeof(ApiResponse<OrderRecord>), statusCode: StatusCodes.Status200OK)]
    public async Task<ActionResult<ApiResponse<OrderRecord>>> GetOrderRecordWithMetadata(int id)
    {
        ErrorOr<OrderRecord> result = await orderService.GetOrderRecordByIdAsync(id: id);
        Dictionary<string, object> metadata = new()
        {
            [key: "cached"] = true,
            [key: "cacheExpiry"] = DateTime.UtcNow.AddMinutes(value: 15),
            [key: "source"] = "database",
            [key: "processingTimeMs"] = 125
        };

        ApiResponse<OrderRecord> apiResponse = result.ToApiResponseWithMetadata(metadata: metadata,
            message: "OrderRecord retrieved with metadata");
        return Ok(value: apiResponse);
    }
}

/// <summary>
/// Minimal API endpoints using ErrorOr ApiResponse conversions
/// </summary>
public static class ApiResponseMinimalApiExamples
{
    [Obsolete(message: "Obsolete")]
    public static void MapOrderRecordApiResponseEndpoints(this WebApplication app)
    {
        RouteGroupBuilder orders = app.MapGroup(prefix: "/api/orders-wrapped")
            .WithTags(tags: "OrderRecords with ApiResponse");

        // GET /api/orders-wrapped/{id} - Returns ApiResponse wrapper
        orders.MapGet(pattern: "/{id:int}",
                handler: async (int id,
                    ApiResponseWrappedExampleService orderService) =>
                {
                    ErrorOr<OrderRecord> result = await orderService.GetOrderRecordByIdAsync(id: id);
                    return result.ToTypedApiResponse(message: "OrderRecord retrieved successfully");
                })
        .WithName(endpointName: "GetOrderRecordWrapped")
        .WithSummary(summary: "Get order with ApiResponse wrapper")
        .Produces<ApiResponse<OrderRecord>>();

        // POST /api/orders-wrapped - Returns ApiResponse with Created status
        orders.MapPost(pattern: "/",
                handler: async (CreateOrderRecordRequest request,
                    ApiResponseWrappedExampleService orderService) =>
                {
                    ErrorOr<OrderRecord> result = await orderService.CreateOrderRecordAsync(request: request);
                    return result.ToTypedApiResponseCreated(message: "OrderRecord created successfully");
                })
        .WithName(endpointName: "CreateOrderRecordWrapped")
        .WithSummary(summary: "Create order with ApiResponse wrapper")
        .Produces<ApiResponse<OrderRecord>>();

        // PATCH /api/orders-wrapped/{id}/status - Returns ApiResponse without data
        orders.MapPatch(pattern: "/{id:int}/status",
                handler: async (int id,
                    string status,
                    ApiResponseWrappedExampleService orderService) =>
                {
                    ErrorOr<Updated> result = await orderService.UpdateOrderRecordStatusAsync(id: id,
                        status: status);
                    ApiResponse apiResponse = result.ToApiResponseUpdated(message: "OrderRecord status updated successfully");
                    return TypedResults.Ok(value: apiResponse);
                })
        .WithName(endpointName: "UpdateOrderRecordStatusWrapped")
        .WithSummary(summary: "Update order status with ApiResponse wrapper")
        .Produces<ApiResponse>();

        // DELETE /api/orders-wrapped/{id} - Returns ApiResponse without data
        orders.MapDelete(pattern: "/{id:int}",
                handler: async (int id,
                    ApiResponseWrappedExampleService orderService) =>
                {
                    ErrorOr<Deleted> result = await orderService.CancelOrderRecordAsync(id: id);
                    ApiResponse apiResponse = result.ToApiResponseDeleted(message: "OrderRecord cancelled successfully");
                    return TypedResults.Ok(value: apiResponse);
                })
        .WithName(endpointName: "CancelOrderRecordWrapped")
        .WithSummary(summary: "Cancel order with ApiResponse wrapper")
        .Produces<ApiResponse>();

        // GET /api/orders-wrapped - Returns paginated ApiResponse
        orders.MapGet(pattern: "/",
                handler: async (int page,
                    int pageSize,
                    ApiResponseWrappedExampleService orderService) =>
                {
                    ErrorOr<PagedList<OrderRecord>> result = await orderService.GetOrderRecordsPagedAsync(page: page,
                        pageSize: pageSize);
                    return result.ToTypedApiResponse(message: "OrderRecords retrieved successfully");
                })
        .WithName(endpointName: "GetOrderRecordsPagedWrapped")
        .WithSummary(summary: "Get paginated orders with ApiResponse wrapper")
        .Produces<ApiResponse<List<OrderRecord>>>();
    }
}

/// <summary>
/// Example DTOs for the order endpoints
/// </summary>
public record OrderRecord(int Id, int CustomerId, List<OrderRecordItem> Items, string Status, decimal Total, DateTime CreatedAt)
{
    public OrderRecord(int customerId, List<OrderRecordItem> items)
        : this(Id: 0,
            CustomerId: customerId,
            Items: items,
            Status: "Pending",
            Total: items.Sum(selector: i => i.Price * i.Quantity),
            CreatedAt: DateTime.UtcNow) { }

    public OrderRecord UpdateStatus(string status) => this with { Status = status };
}

public record OrderRecordItem(int ProductId, string ProductName, decimal Price, int Quantity);
public record CreateOrderRecordRequest(int CustomerId, List<OrderRecordItem> Items);

/// <summary>
/// Example of expected ApiResponse JSON outputs with RFC 7807 Problem Details and full error codes
/// </summary>
public static class ApiResponseJsonExamples
{
    /*
    Successful GET /api/orders-wrapped/1:
    HTTP 200 OK
    Content-Type: application/json
    {
        "isSuccess": true,
        "data": {
            "id": 1,
            "customerId": 123,
            "items": [
                { "productId": 1, "productName": "iPhone 15", "price": 999.99, "quantity": 1 }
            ],
            "status": "Pending",
            "total": 999.99,
            "createdAt": "2024-01-15T10:30:00Z"
        },
        "message": "OrderRecord retrieved successfully",
        "status": 200,
        "timestamp": "2024-01-15T10:30:00Z",
        "apiVersion": "1.0",
        "requestId": "abc123"
    }
    
    Not Found GET /api/orders-wrapped/999:
    HTTP 200 OK (Note: Always 200, but status shows the real status)
    Content-Type: application/json
    {
        "isSuccess": false,
        "data": null,
        "message": "OrderRecord with ID 999 was not found",
        "type": "https://httpstatuses.com/404",
        "title": "OrderRecord.NotFound",
        "status": 404,
        "detail": "OrderRecord with ID 999 was not found",
        "timestamp": "2024-01-15T10:30:00Z",
        "apiVersion": "1.0",
        "requestId": "def456"
    }
    
    Validation Error POST /api/orders-wrapped:
    HTTP 200 OK (Note: Always 200, but status shows the real status)
    Content-Type: application/json
    {
        "isSuccess": false,
        "data": null,
        "message": "Validation failed",
        "type": "https://tools.ietf.org/html/rfc7231#section-6.5.1",
        "title": "Validation Failed",
        "status": 422,
        "detail": "One or more validation errors occurred",
        "errors": {
            "Customer.InvalidId": ["Customer ID is required"],
            "OrderRecord.ItemsRequired": ["At least one order item is required"]
        },
        "timestamp": "2024-01-15T10:30:00Z",
        "apiVersion": "1.0",
        "requestId": "ghi789"
    }
    
    Successful Creation POST /api/orders-wrapped:
    HTTP 200 OK (Note: Always 200, but status shows Created)
    Content-Type: application/json
    {
        "isSuccess": true,
        "data": {
            "id": 2,
            "customerId": 123,
            "items": [
                { "productId": 1, "productName": "iPhone 15", "price": 999.99, "quantity": 1 }
            ],
            "status": "Pending",
            "total": 999.99,
            "createdAt": "2024-01-15T10:30:00Z"
        },
        "message": "OrderRecord created successfully",
        "status": 201,
        "timestamp": "2024-01-15T10:30:00Z",
        "apiVersion": "1.0",
        "requestId": "jkl012"
    }
    
    Conflict Error DELETE /api/orders-wrapped/1:
    HTTP 200 OK (Note: Always 200, but status shows Conflict)
    Content-Type: application/json
    {
        "isSuccess": false,
        "data": null,
        "message": "A conflict occurred",
        "type": "https://httpstatuses.com/409",
        "title": "OrderRecord.CannotCancel",
        "status": 409,
        "detail": "Cannot cancel a shipped order",
        "errors": {
            "OrderRecord.CannotCancel": ["Cannot cancel a shipped order"]
        },
        "timestamp": "2024-01-15T10:30:00Z",
        "apiVersion": "1.0",
        "requestId": "mno345"
    }
    
    Paginated Result GET /api/orders-wrapped?page_index=1&page_size=10:
    HTTP 200 OK
    Content-Type: application/json
    {
        "isSuccess": true,
        "data": [
            { "id": 1, "customerId": 123, "status": "Pending", "total": 999.99 },
            { "id": 2, "customerId": 124, "status": "Shipped", "total": 1499.99 }
        ],
        "message": "OrderRecords retrieved successfully",
        "status": 200,
        "pagination": {
            "currentPage": 1,
            "pageSize": 10,
            "totalItems": 25,
            "totalPages": 3,
            "hasPrevious": false,
            "hasNext": true,
            "firstItemIndex": 1,
            "lastItemIndex": 10
        },
        "links": {
            "self": "/api/orders-wrapped?page_index=1&page_size=10",
            "next": "/api/orders-wrapped?page_index=2&page_size=10",
            "first": "/api/orders-wrapped?page_index=1&page_size=10",
            "last": "/api/orders-wrapped?page_index=3&page_size=10"
        },
        "timestamp": "2024-01-15T10:30:00Z",
        "apiVersion": "1.0",
        "requestId": "pqr678"
    }

    Enhanced Error Result with Multiple Categories:
    HTTP 200 OK
    Content-Type: application/json
    {
        "isSuccess": false,
        "data": null,
        "message": "Validation failed",
        "type": "https://tools.ietf.org/html/rfc7231#section-6.5.1",
        "title": "Validation Failed",
        "status": 422,
        "detail": "One or more validation errors occurred",
        "errors": {
            "Product.NameRequired": ["Product name is required"],
            "Product.PriceInvalid": ["Product price must be greater than 0"],
            "Customer.EmailInvalid": ["Email format is invalid"],
            "Address.ZipCodeRequired": ["ZIP code is required for shipping"]
        },
        "timestamp": "2024-01-15T10:30:00Z",
        "apiVersion": "1.0",
        "requestId": "stu901"
    }

    Result with HATEOAS Links and Metadata:
    HTTP 200 OK
    Content-Type: application/json
    {
        "isSuccess": true,
        "data": {
            "id": 1,
            "customerId": 123,
            "status": "Processing",
            "total": 999.99
        },
        "message": "OrderRecord retrieved with navigation links",
        "status": 200,
        "links": {
            "self": "/api/orders/1",
            "update-status": "/api/orders/1/status",
            "cancel": "/api/orders/1",
            "customer": "/api/customers/123"
        },
        "metadata": {
            "cached": true,
            "cacheExpiry": "2024-01-15T10:45:00Z",
            "source": "database",
            "processingTimeMs": 125
        },
        "timestamp": "2024-01-15T10:30:00Z",
        "apiVersion": "1.0",
        "requestId": "vwx234"
    }
    */
}

#endregion