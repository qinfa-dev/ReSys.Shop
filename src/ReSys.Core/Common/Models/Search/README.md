# Dynamic & Fluent Search System

This directory provides a flexible and powerful system for adding full-text search capabilities to `IQueryable` collections. It is designed to support two primary scenarios:

1.  **Dynamic Search**: Driven by parameters from an HTTP request, allowing clients to specify search terms and fields dynamically.
2.  **Fluent & Strongly-Typed Search**: A builder pattern that uses lambda expressions for constructing type-safe search queries in server-side code.

---

## Features

-   **Dynamic Search**: Searches across all string properties of an entity or a specified list of fields based on `ISearchParams`.
-   **Fluent API**: A `SearchBuilder<T>` for constructing strongly-typed search queries with a chainable syntax.
-   **Configurable Logic**: Control search behavior with options for case sensitivity, exact matching, and "starts with" logic.
-   **Flexible Naming Conventions**: Automatically maps API query field names (e.g., `camelCase`, `snake_case`) to entity property names during dynamic searches.
-   **Performance**: Caches reflection lookups for entity properties to minimize overhead on repeated searches.

---

## Usage Guide

### 1. Dynamic Search from API Requests

This is the most common use case for handling search functionality driven by a user interface. The `ApplySearch` extension method takes an `ISearchParams` object, which can be populated directly from query string parameters.

**Example Controller:**

```csharp
[ApiController]
[Route("api/products")]
public class ProductsController : ControllerBase
{
    private readonly MyDbContext _context;

    // ... constructor ...

    [HttpGet]
    public async Task<IActionResult> GetProducts([FromQuery] SearchParams searchParams)
    {
        var query = _context.Products.AsQueryable();

        // Apply search based on the query string parameters
        var searchedQuery = query.ApplySearch(searchParams);

        var products = await searchedQuery.ToListAsync();
        return Ok(products);
    }
}
```

**Sample API Requests:**

-   **Search across all string properties:**
    `GET /api/products?searchTerm=Laptop`

-   **Search within specific fields:**
    `GET /api/products?searchTerm=Pro&searchFields=name,description`

-   **Perform an exact, case-sensitive match:**
    `GET /api/products?searchTerm=SKU-12345&exactMatch=true&caseSensitive=true`

### 2. Fluent & Strongly-Typed Search

This approach is ideal for server-side logic where you want the compiler to ensure that the specified search fields are valid. It provides a clean, readable, and type-safe way to build search queries.

#### A. Using the Search Builder

The `Search()` extension method returns a `SearchBuilder<T>` that allows you to chain search criteria together.

```csharp
var results = _context.Products
    .Search("Macbook") // The term to search for
    .In(p => p.Name)         // Specify a field to search in
    .In(p => p.Description)  // Add another field
    .CaseSensitive(false)    // Configure options
    .Execute()               // Build and apply the expression
    .ToList();
```

#### B. Using the Direct Extension Method

For simpler cases, you can use the `ApplySearch` overload that accepts lambda expressions directly.

```csharp
var results = _context.Products
    .ApplySearch("Macbook", 
        p => p.Name, 
        p => p.Description
    )
    .ToList();
```
