# ApiResponse Wrapper

This directory contains the `ApiResponse` wrapper, a standardized solution for structuring API responses across the application. It is designed to provide a consistent and predictable format for all API interactions, adhering to industry best practices and supporting RFC 7807 Problem Details for error reporting.

## Key Features

*   **Consistent Structure**: Ensures all API responses follow a uniform format, making client-side consumption easier.
*   **Success and Error Handling**: Provides clear mechanisms for indicating successful operations and detailed error reporting.
*   **RFC 7807 Problem Details**: Integrates seamlessly with the Problem Details for HTTP APIs standard, offering rich, machine-readable error information.
*   **Pagination Support**: Includes metadata for paginated responses, simplifying the handling of large datasets.
*   **Fluent API**: Offers a fluent interface for building and modifying responses, enhancing readability and ease of use.
*   **HATEOAS and Metadata**: Supports adding Hypermedia as the Engine of Application State (HATEOAS) links and custom metadata to responses.
*   **Non-Generic Version**: A non-generic `ApiResponse` is available for scenarios where a specific data payload is not required.

## Structure

The `ApiResponse<T>` class is implemented as a partial class, with its functionality split across several files for better organization and maintainability:

*   `ApiResponse.cs`: Contains the core properties and the main definition of the `ApiResponse<T>` class.
*   `ApiResponse.Factory.cs`: Provides static factory methods for creating various successful responses (e.g., `Success`, `Created`, `Accepted`, `SuccessWithoutData`, `Paginated`).
*   `ApiResponse.Error.cs`: Offers static factory methods for generating different types of error responses (e.g., `Error`, `NotFound`, `Unauthorized`, `Forbidden`, `Conflict`, `ValidationFailed`).
*   `ApiResponse.Fluent.cs`: Implements fluent methods (`WithLink`, `WithMetadata`, `WithError`, etc.) for building and modifying `ApiResponse` instances in a chained manner.
*   `ApiResponse.Helpers.cs`: Contains helper methods, primarily for resolving RFC 7807 Problem Details URIs and titles based on HTTP status codes.
*   `ApiResponse.NonGeneric.cs`: Defines a non-generic `ApiResponse` class that inherits from `ApiResponse<object>`, providing convenience methods for responses without a specific data type.
*   `ApiResponse.Validation.cs`: Provides specialized methods for creating validation error responses.
*   `ApiResponse.PagingMetadata.cs`: Defines the `PaginationMetadata` class, used to encapsulate pagination details within `ApiResponse`.

## Usage Examples

### Successful Response with Data

```csharp
// Example: Fetching a single product
var product = new { Id = 1, Name = "Example Product" };
var response = ApiResponse<object>.Success(product, "Product retrieved successfully.");
// Result: { IsSuccess: true, Data: { Id: 1, Name: "Example Product" }, Message: "Product retrieved successfully.", Status: 200, ... }
```

### Successful Response without Data (e.g., Delete Operation)

```csharp
var response = ApiResponse<object>.SuccessWithoutData("Resource deleted.");
// Result: { IsSuccess: true, Message: "Resource deleted.", Status: 204, ... }
```

### Paginated Response

```csharp
var items = new List<string> { "Item 1", "Item 2" };
var pagination = new PaginationMetadata(currentPage: 1, pageSize: 10, totalCount: 25);
var paginatedResponse = ApiResponse<List<string>>.Paginated(items, pagination, "Items fetched.");
// Result: { IsSuccess: true, Data: ["Item 1", "Item 2"], Message: "Items fetched.", Pagination: { CurrentPage: 1, ... }, Status: 200, ... }
```

### Error Response (Not Found)

```csharp
var errorResponse = ApiResponse<object>.NotFound("Product with ID 123 not found.");
// Result: { IsSuccess: false, Message: "Product with ID 123 not found.", Type: ".../404", Title: "Not Found", Status: 404, Detail: "The requested resource was not found", ... }
```

### Validation Error Response

```csharp
var validationErrors = new Dictionary<string, string[]> {
    { "Name", new[] { "Name is required." } },
    { "Price", new[] { "Price must be greater than zero." } }
};
var validationErrorResponse = ApiResponse<object>.ValidationFailed(validationErrors, "Validation failed for product creation.");
// Result: { IsSuccess: false, Message: "Validation failed for product creation.", Errors: { "Name": [...], "Price": [...] }, Type: ".../422", Title: "Unprocessable Entity", Status: 422, Detail: "One or more validation errors occurred", ... }
```

### Using the Fluent API

```csharp
var fluentResponse = ApiResponse<object>.Success(null, "Operation successful")
    .WithLink("self", "/api/resource/1")
    .WithMetadata("version", "v1.0");
// Result: { IsSuccess: true, Message: "Operation successful", Links: { "self": "/api/resource/1" }, Metadata: { "version": "v1.0" }, ... }
```
# Common Wrappers for API Responses

This directory provides a set of extension methods designed to streamline the handling of `ErrorOr` results and convert them into standardized API responses for different ASP.NET Core application types (MVC Controllers and Minimal APIs). The goal is to offer consistent and predictable ways to return success or error outcomes from service layers to HTTP clients, adhering to best practices like RFC 7807 Problem Details.

---

## Modules

### 1. `ErrorOrActionResultExtensions.cs`

This class provides extension methods specifically for ASP.NET Core MVC Controllers, enabling the conversion of `ErrorOr` results into `Microsoft.AspNetCore.Mvc.ActionResult` types.

-   **Purpose**: To seamlessly integrate the `ErrorOr` functional error handling library with traditional MVC controller actions, ensuring that API responses correctly reflect the operation's outcome using appropriate HTTP status codes.
-   **Key Features**:
    -   **Direct HTTP Status Mapping**: Automatically maps `ErrorOr.ErrorType` (e.g., `Validation`, `NotFound`, `Conflict`) to corresponding HTTP status codes (e.g., 400 Bad Request, 404 Not Found, 409 Conflict).
    -   **RFC 7807 Problem Details**: Generates `ProblemDetails` for general errors and `ValidationProblemDetails` for validation errors, providing structured and machine-readable error responses.
    -   **Convenience Methods**: Includes methods like `ToActionResult<T>`, `ToCreatedAtActionResult<T>`, `ToNoContentResult` (for `Updated` and `Deleted` results), and `ToAcceptedResult<T>` for common API response patterns.
    -   **Performance**: Utilizes a `FrozenDictionary` for efficient error type to status code mapping.

### 2. `ErrorOrApiResponseExtensions.cs`

This class offers extension methods to wrap `ErrorOr` results within a standardized `ApiResponse` object. This approach ensures a consistent JSON response structure, regardless of the operation's success or failure.

-   **Purpose**: To provide a unified response format for all API endpoints, where the HTTP status code is always 200 OK (on the wire), but the actual operation status and details are conveyed within the `ApiResponse` object itself. This is often preferred in scenarios like Single Page Applications (SPAs) or mobile clients that benefit from predictable response parsing.
-   **Key Features**:
    -   **Consistent Wrapper**: All responses are encapsulated in an `ApiResponse<T>` or `ApiResponse` object, containing `IsSuccess`, `Message`, `Status` (the actual HTTP status code of the operation), `Errors`, and `Data`.
    -   **Error Details**: Maps `ErrorOr.Error` details into the `Errors` dictionary of the `ApiResponse`, providing structured error messages.
    -   **Advanced Features**: Supports pagination metadata (`ToApiResponsePaged`), HATEOAS links (`ToApiResponseWithLinks`), and additional custom metadata (`ToApiResponseWithMetadata`).
    -   **Minimal API Integration**: Includes `ToTypedApiResponse` and `ToTypedApiResponseCreated` for use with Minimal APIs, returning `IResult` wrapped in `TypedResults.Ok`.
    -   **Performance**: Also uses a `FrozenDictionary` for efficient status code mapping.

### 3. `ErrorOrExtensions.cs`

This class acts as a central documentation and entry point for all `ErrorOr` conversion extensions within this directory. It doesn't contain implementation logic but guides developers on choosing the appropriate extension based on their API design philosophy.

-   **Purpose**: To provide a high-level overview and usage guide for the different `ErrorOr` integration strategies, helping developers understand when to use direct HTTP status codes versus a standardized API response wrapper.
-   **Content**: Includes remarks and examples illustrating the use cases for `ErrorOrTypedResultsExtensions`, `ErrorOrActionResultExtensions`, and `ErrorOrApiResponseExtensions`, along with considerations for performance and API design.

### 4. `ErrorOrTypedResultsExtensions.cs`

This class provides extension methods tailored for ASP.NET Core Minimal APIs, converting `ErrorOr` results into `Microsoft.AspNetCore.Http.IResult` types.

-   **Purpose**: To enable clean and idiomatic integration of `ErrorOr` with Minimal APIs, allowing developers to return direct HTTP status codes and RFC 7807 compliant Problem Details from their endpoint handlers.
-   **Key Features**:
    -   **Direct HTTP Status Mapping**: Similar to `ErrorOrActionResultExtensions`, it maps `ErrorOr.ErrorType` to appropriate HTTP status codes.
    -   **RFC 7807 Problem Details**: Generates `ProblemDetails` and `ValidationProblemDetails` using `Results.Problem` and `Results.ValidationProblem`.
    -   **Convenience Methods**: Offers methods like `ToTypedResult<T>`, `ToTypedResultCreated<T>`, `ToTypedResultNoContent` (for `Updated` and `Deleted` results), and `ToTypedResultAccepted<T>` for common Minimal API response patterns.
    -   **Performance**: Employs a `FrozenDictionary` for optimized status code lookups.

---

## Purpose

The components within this directory collectively aim to:

-   **Standardize API Responses**: Provide consistent ways to handle and return operation results across the backend API.
-   **Improve Developer Experience**: Simplify the process of converting `ErrorOr` results into HTTP responses, reducing boilerplate code.
-   **Enhance Error Reporting**: Ensure that error responses are structured, informative, and adhere to industry standards (RFC 7807).
-   **Support Different API Styles**: Offer flexible options for API design, whether preferring direct HTTP status codes or a wrapped `ApiResponse` structure.
-   **Promote Maintainability**: Centralize response handling logic, making it easier to manage and update API response formats.
