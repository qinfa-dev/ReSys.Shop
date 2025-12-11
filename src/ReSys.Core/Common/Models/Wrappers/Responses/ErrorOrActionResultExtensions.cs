using System.Collections.Frozen;

using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;

namespace ReSys.Core.Common.Models.Wrappers.Responses;

/// <summary>
/// Extension methods for converting ErrorOr results to ActionResult for MVC Controllers.
/// Provides seamless integration between ErrorOr library and ASP.NET Core MVC Controllers.
/// </summary>
/// <remarks>
/// This class focuses specifically on MVC Controller ActionResult conversion with proper HTTP status codes.
/// All methods return appropriate ActionResult types with correct status codes for API responses.
/// 
/// Key Features:
/// - Automatic error-to-HTTP status code mapping
/// - RFC 7807 compliant ProblemDetails responses
/// - Validation error handling with ValidationProblemDetails
/// - Consistent controller response patterns
/// - Performance optimized with frozen dictionaries
/// </remarks>
/// <example>
/// Basic usage in MVC Controllers:
/// <code>
/// [HttpGet("{id:int}")]
/// public async Task&lt;IActionResult&gt; GetProduct(int id)
/// {
///     var result = await _productService.GetProductAsync(id);
///     return result.ToActionResult(); // Returns Ok(product) or NotFound with problem details
/// }
/// </code>
/// </example>
public static class ErrorOrActionResultExtensions
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
    private const string ValidationProblemType = "https://tools.ietf.org/html/rfc7231#section-6.5.1";

    #region Core ActionResult Extensions

    /// <summary>
    /// Converts an ErrorOr&lt;T&gt; result to an ActionResult for MVC controllers.
    /// Returns Ok(value) on success or appropriate error ActionResult with proper HTTP status codes.
    /// </summary>
    /// <typeparam name="T">The type of the success result</typeparam>
    /// <param name="result">The ErrorOr result to convert</param>
    /// <returns>ActionResult representing either success (200 OK) or error response with appropriate status code</returns>
    /// <example>
    /// <code>
    /// [HttpGet("{id:int}")]
    /// public async Task&lt;ActionResult&lt;Product&gt;&gt; GetProduct(int id)
    /// {
    ///     var result = await _productService.GetProductAsync(id);
    ///     return result.ToActionResult(); // Returns Ok(product) or NotFound with problem details
    /// }
    /// </code>
    /// </example>
    public static ActionResult<T> ToActionResult<T>(this ErrorOr<T> result)
    {
        if (result.IsError)
        {
            IActionResult problemResult = result.Errors.ToProblemDetailsActionResult();
            // Cast IActionResult to ActionResult to use the constructor that accepts ActionResult
            return (ActionResult)problemResult;
        }
        
        return new OkObjectResult(value: result.Value);
    }

    /// <summary>
    /// Converts an ErrorOr&lt;T&gt; result to a CreatedAtAction ActionResult for MVC controllers.
    /// Returns CreatedAtAction on success or appropriate error ActionResult with proper HTTP status codes.
    /// </summary>
    /// <typeparam name="T">The type of the created resource</typeparam>
    /// <param name="result">The ErrorOr result to convert</param>
    /// <param name="actionName">The name of the action to generate URL for</param>
    /// <param name="routeValues">Route values for generating the URL</param>
    /// <returns>ActionResult representing either created response (201 Created) or error with appropriate status code</returns>
    /// <example>
    /// <code>
    /// [HttpPost]
    /// public async Task&lt;ActionResult&lt;Product&gt;&gt; CreateProduct(CreateProductRequest command)
    /// {
    ///     var result = await _productService.CreateProductAsync(command);
    ///     return result.ToCreatedAtActionResult(nameof(GetProduct), new { id = result.Value?.Id });
    /// }
    /// </code>
    /// </example>
    public static ActionResult<T> ToCreatedAtActionResult<T>(this ErrorOr<T> result, string actionName, object? routeValues = null)
    {
        if (result.IsError)
        {
            IActionResult problemResult = result.Errors.ToProblemDetailsActionResult();
            return (ActionResult)problemResult;
        }
        
        return new CreatedAtActionResult(actionName: actionName,
            controllerName: null,
            routeValues: routeValues,
            value: result.Value);
    }

    /// <summary>
    /// Converts an ErrorOr&lt;T&gt; result to a CreatedAtRoute ActionResult for MVC controllers.
    /// Returns CreatedAtRoute on success or appropriate error ActionResult with proper HTTP status codes.
    /// </summary>
    /// <typeparam name="T">The type of the created resource</typeparam>
    /// <param name="result">The ErrorOr result to convert</param>
    /// <param name="routeName">The name of the route to generate URL for</param>
    /// <param name="routeValues">Route values for generating the URL</param>
    /// <returns>ActionResult representing either created response (201 Created) or error with appropriate status code</returns>
    /// <example>
    /// <code>
    /// [HttpPost]
    /// public async Task&lt;ActionResult&lt;Product&gt;&gt; CreateProduct(CreateProductRequest command)
    /// {
    ///     var result = await _productService.CreateProductAsync(command);
    ///     return result.ToCreatedAtRouteResult("GetProduct", new { id = result.Value?.Id });
    /// }
    /// </code>
    /// </example>
    public static ActionResult<T> ToCreatedAtRouteResult<T>(this ErrorOr<T> result, string routeName, object? routeValues = null)
    {
        if (result.IsError)
        {
            IActionResult problemResult = result.Errors.ToProblemDetailsActionResult();
            return (ActionResult)problemResult;
        }
        
        return new CreatedAtRouteResult(routeName: routeName,
            routeValues: routeValues,
            value: result.Value);
    }

    /// <summary>
    /// Converts an ErrorOr&lt;Updated&gt; result to a NoContent ActionResult for MVC controllers.
    /// Returns NoContent on success or appropriate error ActionResult with proper HTTP status codes.
    /// </summary>
    /// <param name="result">The ErrorOr&lt;Updated&gt; result to convert</param>
    /// <returns>ActionResult representing either no content (204 No Content) or error response</returns>
    /// <example>
    /// <code>
    /// [HttpPut("{id:int}")]
    /// public async Task&lt;IActionResult&gt; UpdateProduct(int id, UpdateProductRequest command)
    /// {
    ///     var result = await _productService.UpdateProductAsync(id, command);
    ///     return result.ToNoContentResult(); // Returns NoContent or error details
    /// }
    /// </code>
    /// </example>
    public static IActionResult ToNoContentResult(this ErrorOr<Updated> result)
        => result.Match(
            onValue: _ => new NoContentResult(),
            onError: errors => errors.ToProblemDetailsActionResult());

    /// <summary>
    /// Converts an ErrorOr&lt;Deleted&gt; result to a NoContent ActionResult for MVC controllers.
    /// Returns NoContent on success or appropriate error ActionResult with proper HTTP status codes.
    /// </summary>
    /// <param name="result">The ErrorOr&lt;Deleted&gt; result to convert</param>
    /// <returns>ActionResult representing either no content (204 No Content) or error response</returns>
    /// <example>
    /// <code>
    /// [HttpDelete("{id:int}")]
    /// public async Task&lt;IActionResult&gt; DeleteProduct(int id)
    /// {
    ///     var result = await _productService.DeleteProductAsync(id);
    ///     return result.ToNoContentResult(); // Returns NoContent or error details
    /// }
    /// </code>
    /// </example>
    public static IActionResult ToNoContentResult(this ErrorOr<Deleted> result)
        => result.Match(
            onValue: _ => new NoContentResult(),
            onError: errors => errors.ToProblemDetailsActionResult());

    /// <summary>
    /// Converts an ErrorOr&lt;T&gt; result to an Accepted ActionResult for MVC controllers.
    /// Returns Accepted on success or appropriate error ActionResult with proper HTTP status codes.
    /// </summary>
    /// <typeparam name="T">The type of the accepted resource</typeparam>
    /// <param name="result">The ErrorOr result to convert</param>
    /// <param name="location">Optional URL where the status of the operation can be monitored</param>
    /// <returns>ActionResult representing either accepted response (202 Accepted) or error</returns>
    /// <example>
    /// <code>
    /// [HttpPost("{id:int}/process")]
    /// public async Task&lt;ActionResult&lt;ProcessResult&gt;&gt; ProcessProduct(int id)
    /// {
    ///     var result = await _productService.ProcessProductAsync(id);
    ///     return result.ToAcceptedResult($"/api/products/{id}/status");
    /// }
    /// </code>
    /// </example>
    public static ActionResult<T> ToAcceptedResult<T>(this ErrorOr<T> result, string? location = null)
    {
        if (result.IsError)
        {
            IActionResult problemResult = result.Errors.ToProblemDetailsActionResult();
            return (ActionResult)problemResult;
        }
        
        return location != null 
            ? new AcceptedResult(location: location,
                value: result.Value)
            : new AcceptedResult(location: null,
                result.Value);
    }

    #endregion

    #region ProblemDetails Conversion

    /// <summary>
    /// Converts a list of errors to an IActionResult with appropriate ProblemDetails for MVC controllers.
    /// Handles validation errors specially by creating ValidationProblemDetails with proper HTTP status codes.
    /// </summary>
    /// <param name="errors">The list of errors to convert</param>
    /// <returns>IActionResult with appropriate HTTP status and ProblemDetails</returns>
    /// <remarks>
    /// Error types mapping:
    /// - Validation → 400 Bad Request with ValidationProblemDetails
    /// - NotFound → 404 Not Found with ProblemDetails
    /// - Unauthorized → 401 Unauthorized with ProblemDetails
    /// - Forbidden → 403 Forbidden with ProblemDetails
    /// - Conflict → 409 Conflict with ProblemDetails
    /// - Failure → 500 Internal Server Error with ProblemDetails
    /// - Unexpected → 422 Unprocessable Entity with ProblemDetails
    /// </remarks>
    /// <example>
    /// <code>
    /// [HttpPost]
    /// public async Task&lt;IActionResult&gt; CreateProduct(CreateProductRequest command)
    /// {
    ///     var result = await _productService.CreateProductAsync(command);
    ///     
    ///     if (result.IsError)
    ///         return result.Errors.ToProblemDetailsActionResult();
    ///         
    ///     return CreatedAtAction(nameof(GetProduct), new { id = result.Value.Id }, result.Value);
    /// }
    /// </code>
    /// </example>
    public static IActionResult ToProblemDetailsActionResult(this IReadOnlyList<Error> errors)
    {
        if (errors.Count == 0)
            return CreateGenericProblemActionResult();

        Error firstError = errors[index: 0];

        if (firstError.Type == ErrorType.Validation)
            return CreateValidationProblemActionResult(errors: errors);

        return CreateProblemActionResult(error: firstError);
    }

    #endregion

    #region Private Helper Methods

    private static IActionResult CreateValidationProblemActionResult(IReadOnlyList<Error> errors)
    {
        Dictionary<string, string[]> errorsByProperty = errors
            .ToLookup(keySelector: e => e.Code,
                elementSelector: e => e.Description)
            .ToDictionary(keySelector: g => g.Key,
                elementSelector: g => g.ToArray());

        return new BadRequestObjectResult(error: new ValidationProblemDetails(errors: errorsByProperty)
        {
            Title = "Validation Failed",
            Type = ValidationProblemType,
            Status = StatusCodes.Status400BadRequest
        });
    }

    private static IActionResult CreateProblemActionResult(Error error)
    {
        int statusCode = GetStatusCode(type: error.Type);
        ProblemDetails problemDetails = new()
        {
            Title = error.Code,
            Detail = error.Description,
            Status = statusCode,
            Type = GetProblemTypeUri(statusCode: statusCode)
        };

        return new ObjectResult(value: problemDetails) { StatusCode = statusCode };
    }

    private static IActionResult CreateGenericProblemActionResult()
    {
        ProblemDetails problemDetails = new()
        {
            Title = "Unknown Error",
            Detail = "An unknown error occurred.",
            Status = DefaultStatusCode,
            Type = GetProblemTypeUri(statusCode: DefaultStatusCode)
        };

        return new ObjectResult(value: problemDetails) { StatusCode = DefaultStatusCode };
    }

    private static int GetStatusCode(ErrorType type)
        => ErrorTypeToStatusCode.GetValueOrDefault(key: type,
            defaultValue: DefaultStatusCode);

    private static string GetProblemTypeUri(int statusCode)
        => $"https://httpstatuses.com/{statusCode}";

    #endregion
}

#region Usage Examples

/// <summary>
/// Example service layer that returns ErrorOr results for MVC Controller usage
/// </summary>
public sealed class MvcControllerExampleService
{
    public async Task<ErrorOr<Customer>> GetCustomerByIdAsync(int id)
    {
        if (id <= 0)
            return Error.Validation(code: "Customer.Id",
                description: "Customer ID must be greater than 0");

        Customer? customer = await FindCustomerInDatabaseAsync(id: id);
        if (customer == null)
            return Error.NotFound(code: "Customer.NotFound",
                description: $"Customer with ID {id} was not found");

        return customer;
    }

    public async Task<ErrorOr<Customer>> CreateCustomerAsync(CreateCustomerRequest request)
    {
        List<Error> validationErrors = ValidateCreateCustomerRequest(request: request);
        if (validationErrors.Any())
            return validationErrors;

        Customer? existingCustomer = await FindCustomerByEmailAsync(email: request.Email);
        if (existingCustomer != null)
            return Error.Conflict(code: "Customer.EmailExists",
                description: "A customer with this email already exists");

        Customer customer = new(name: request.Name,
            email: request.Email);
        await SaveCustomerAsync(customer: customer);
        return customer;
    }

    public async Task<ErrorOr<Updated>> UpdateCustomerAsync(int id, UpdateCustomerRequest request)
    {
        ErrorOr<Customer> getCustomerResult = await GetCustomerByIdAsync(id: id);
        if (getCustomerResult.IsError)
            return getCustomerResult.Errors;

        Customer customer = getCustomerResult.Value;
        customer.UpdateName(name: request.Name);
        await SaveCustomerAsync(customer: customer);
        return Result.Updated;
    }

    public async Task<ErrorOr<Deleted>> DeleteCustomerAsync(int id)
    {
        ErrorOr<Customer> getCustomerResult = await GetCustomerByIdAsync(id: id);
        if (getCustomerResult.IsError)
            return getCustomerResult.Errors;

        await DeleteCustomerFromDatabaseAsync(id: id);
        return Result.Deleted;
    }

    public async Task<ErrorOr<List<Customer>>> GetCustomersPagedAsync(int page, int pageSize)
    {
        if (page <= 0)
            return Error.Validation(code: "Page",
                description: "Page number must be greater than 0");
        
        if (pageSize <= 0 || pageSize > 100)
            return Error.Validation(code: "PageSize",
                description: "Page size must be between 1 and 100");

        List<Customer> customers = await GetCustomersFromDatabaseAsync(page: page,
            pageSize: pageSize);
        return customers;
    }

    private List<Error> ValidateCreateCustomerRequest(CreateCustomerRequest request)
    {
        List<Error> errors = [];

        if (string.IsNullOrWhiteSpace(value: request.Name))
            errors.Add(item: Error.Validation(code: "Name",
                description: "Customer name is required"));

        if (string.IsNullOrWhiteSpace(value: request.Email))
            errors.Add(item: Error.Validation(code: "Email",
                description: "Email is required"));
        else if (!MustBeValidEmail(email: request.Email))
            errors.Add(item: Error.Validation(code: "Email",
                description: "Email format is invalid"));

        return errors;
    }

    // Placeholder methods - implement with your actual data access
    private Task<Customer?> FindCustomerInDatabaseAsync(int id) => throw new NotImplementedException();
    private Task<Customer?> FindCustomerByEmailAsync(string email) => throw new NotImplementedException();
    private Task SaveCustomerAsync(Customer customer) => throw new NotImplementedException();
    private Task DeleteCustomerFromDatabaseAsync(int id) => throw new NotImplementedException();
    private Task<List<Customer>> GetCustomersFromDatabaseAsync(int page, int pageSize) => throw new NotImplementedException();
    private bool MustBeValidEmail(string email) => throw new NotImplementedException();
}

/// <summary>
/// Comprehensive MVC Controller using ErrorOr ActionResult extensions
/// </summary>
[ApiController]
[Route(template: "api/[controller]")]
[Produces(contentType: "application/json")]
[ApiExplorerSettings(IgnoreApi = true)]
internal sealed class CustomersController(MvcControllerExampleService customerService) : ControllerBase
{
    /// <summary>
    /// Get customer by ID
    /// </summary>
    /// <param name="id">The customer ID</param>
    /// <returns>The customer details</returns>
    /// <response code="200">Customer found and returned</response>
    /// <response code="404">Customer not found</response>
    /// <response code="400">Invalid customer ID provided</response>
    [HttpGet(template: "{id:int}", Name = "GetCustomer")]
    [ProducesResponseType(type: typeof(Customer), statusCode: StatusCodes.Status200OK)]
    [ProducesResponseType(type: typeof(ProblemDetails), statusCode: StatusCodes.Status404NotFound)]
    [ProducesResponseType(type: typeof(ValidationProblemDetails), statusCode: StatusCodes.Status400BadRequest)]
    public async Task<ActionResult<Customer>> GetCustomer(int id)
    {
        ErrorOr<Customer> result = await customerService.GetCustomerByIdAsync(id: id);
        return result.ToActionResult();
    }

    /// <summary>
    /// Create a new customer
    /// </summary>
    /// <param name="request">Customer creation command</param>
    /// <returns>The created customer</returns>
    /// <response code="201">Customer created successfully</response>
    /// <response code="400">Invalid command data</response>
    /// <response code="409">Customer with email already exists</response>
    [HttpPost]
    [ProducesResponseType(type: typeof(Customer), statusCode: StatusCodes.Status201Created)]
    [ProducesResponseType(type: typeof(ValidationProblemDetails), statusCode: StatusCodes.Status400BadRequest)]
    [ProducesResponseType(type: typeof(ProblemDetails), statusCode: StatusCodes.Status409Conflict)]
    public async Task<ActionResult<Customer>> CreateCustomer(CreateCustomerRequest request)
    {
        ErrorOr<Customer> result = await customerService.CreateCustomerAsync(request: request);
        return result.ToCreatedAtActionResult(actionName: nameof(GetCustomer),
            routeValues: new
            {
                id = result.Value?.Id
            });
    }

    /// <summary>
    /// Update an existing customer
    /// </summary>
    /// <param name="id">The customer ID</param>
    /// <param name="request">Customer update command</param>
    /// <returns>No content on success</returns>
    /// <response code="204">Customer updated successfully</response>
    /// <response code="404">Customer not found</response>
    /// <response code="400">Invalid command data</response>
    [HttpPut(template: "{id:int}")]
    [ProducesResponseType(statusCode: StatusCodes.Status204NoContent)]
    [ProducesResponseType(type: typeof(ProblemDetails), statusCode: StatusCodes.Status404NotFound)]
    [ProducesResponseType(type: typeof(ValidationProblemDetails), statusCode: StatusCodes.Status400BadRequest)]
    public async Task<IActionResult> UpdateCustomer(int id, UpdateCustomerRequest request)
    {
        ErrorOr<Updated> result = await customerService.UpdateCustomerAsync(id: id,
            request: request);
        return result.ToNoContentResult();
    }

    /// <summary>
    /// Delete a customer
    /// </summary>
    /// <param name="id">The customer ID</param>
    /// <returns>No content on success</returns>
    /// <response code="204">Customer deleted successfully</response>
    /// <response code="404">Customer not found</response>
    [HttpDelete(template: "{id:int}")]
    [ProducesResponseType(statusCode: StatusCodes.Status204NoContent)]
    [ProducesResponseType(type: typeof(ProblemDetails), statusCode: StatusCodes.Status404NotFound)]
    public async Task<IActionResult> DeleteCustomer(int id)
    {
        ErrorOr<Deleted> result = await customerService.DeleteCustomerAsync(id: id);
        return result.ToNoContentResult();
    }

    /// <summary>
    /// Get paginated list of customers
    /// </summary>
    /// <param name="page">Page number (1-based)</param>
    /// <param name="pageSize">Number of items per page</param>
    /// <returns>Paginated list of customers</returns>
    /// <response code="200">Customers retrieved successfully</response>
    /// <response code="400">Invalid pagination parameters</response>
    [HttpGet]
    [ProducesResponseType(type: typeof(List<Customer>), statusCode: StatusCodes.Status200OK)]
    [ProducesResponseType(type: typeof(ValidationProblemDetails), statusCode: StatusCodes.Status400BadRequest)]
    public async Task<ActionResult<List<Customer>>> GetCustomers([FromQuery] int page = 1, [FromQuery] int pageSize = 10)
    {
        ErrorOr<List<Customer>> result = await customerService.GetCustomersPagedAsync(page: page,
            pageSize: pageSize);
        return result.ToActionResult();
    }

    /// <summary>
    /// Alternative approach using explicit error handling
    /// </summary>
    [HttpGet(template: "{id:int}/alternative")]
    public async Task<IActionResult> GetCustomerAlternative(int id)
    {
        ErrorOr<Customer> result = await customerService.GetCustomerByIdAsync(id: id);

        return result.Match(
            onValue: customer => Ok(value: customer),
            onError: errors => errors.ToProblemDetailsActionResult()
        );
    }

    /// <summary>
    /// Example with custom validation and response handling
    /// </summary>
    [HttpPost(template: "validate")]
    public async Task<IActionResult> ValidateCustomer(CreateCustomerRequest request)
    {
        // Additional controller-level validation
        if (string.IsNullOrWhiteSpace(value: request.Email?.Trim()))
        {
            ModelState.AddModelError(key: nameof(request.Email),
                errorMessage: "Email cannot be empty or whitespace");
        }

        if (!ModelState.IsValid)
        {
            return ValidationProblem(modelStateDictionary: ModelState);
        }

        ErrorOr<Customer> result = await customerService.CreateCustomerAsync(request: request);
        
        if (result.IsError)
            return result.Errors.ToProblemDetailsActionResult();

        return CreatedAtAction(actionName: nameof(GetCustomer),
            routeValues: new
            {
                id = result.Value.Id
            },
            value: result.Value);
    }
}

/// <summary>
/// Example DTOs for the customer endpoints
/// </summary>
public record Customer(int Id, string Name, string Email, DateTime CreatedAt)
{
    public Customer(string name, string email) : this(Id: 0,
        Name: name,
        Email: email,
        CreatedAt: DateTime.UtcNow) { }
    public Customer UpdateName(string name) => this with { Name = name };
}

public record CreateCustomerRequest(string Name, string Email);
public record UpdateCustomerRequest(string Name);

/// <summary>
/// Example of expected HTTP responses for MVC Controllers
/// </summary>
public static class MvcControllerResponseExamples
{
    /*
    Successful GET /api/customers/1:
    HTTP 200 OK
    Content-Type: application/json
    {
        "id": 1,
        "name": "John Doe",
        "email": "john@example.com",
        "createdAt": "2024-01-15T10:30:00Z"
    }
    
    Not Found GET /api/customers/999:
    HTTP 404 Not Found
    Content-Type: application/problem+json
    {
        "type": "https://httpstatuses.com/404",
        "title": "Customer.NotFound",
        "status": 404,
        "detail": "Customer with ID 999 was not found"
    }
    
    Validation Error POST /api/customers:
    HTTP 400 Bad Request
    Content-Type: application/problem+json
    {
        "type": "https://tools.ietf.org/html/rfc7231#section-6.5.1",
        "title": "Validation Failed",
        "status": 400,
        "errors": {
            "Name": ["Customer name is required"],
            "Email": ["Email is required"]
        }
    }
    
    Successful Creation POST /api/customers:
    HTTP 201 Created
    Location: /api/customers/2
    Content-Type: application/json
    {
        "id": 2,
        "name": "Jane Smith",
        "email": "jane@example.com",
        "createdAt": "2024-01-15T10:30:00Z"
    }
    
    Successful Update PUT /api/customers/1:
    HTTP 204 No Content
    
    Conflict Error POST /api/customers:
    HTTP 409 Conflict
    Content-Type: application/problem+json
    {
        "type": "https://httpstatuses.com/409",
        "title": "Customer.EmailExists",
        "status": 409,
        "detail": "A customer with this email already exists"
    }
    */
}

#endregion