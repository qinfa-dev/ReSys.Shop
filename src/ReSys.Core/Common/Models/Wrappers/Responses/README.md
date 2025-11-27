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
