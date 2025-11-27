# Dynamic & Fluent Sorting System

This directory contains a flexible system for applying single or multi-level sorting to `IQueryable` collections. It supports both dynamic, string-based sorting (ideal for APIs) and a fluent builder for constructing sort orders in server-side code.

---

## Features

-   **Dynamic Sorting**: Applies `OrderBy` / `OrderByDescending` clauses based on string field names provided via `ISortParam`.
-   **Multi-Level Sorting**: Supports secondary, tertiary, etc., sorting by applying `ThenBy` / `ThenByDescending` for each subsequent sort parameter.
-   **Fluent API**: A `SortBuilder<T>` provides a clean, readable, and chainable syntax for defining complex sort orders.
-   **Case-Insensitive**: Field names from sort parameters are matched to entity properties case-insensitively.
-   **Performance**: Caches reflection lookups for property and method information to minimize overhead on repeated operations.

---

## How to Use

### 1. Dynamic Sorting from API Requests

This approach allows an API client to specify how a list of results should be sorted. The `ApplySort` extension method accepts an `ISortParam` object, which can be populated from query string parameters.

**Example Controller:**

```csharp
[ApiController]
[Route("api/products")]
public class ProductsController : ControllerBase
{
    private readonly MyDbContext _context;

    // ... constructor ...

    [HttpGet]
    public async Task<IActionResult> GetProducts([FromQuery] SortParams sortParams)
    {
        var query = _context.Products.AsQueryable();

        // Apply sorting based on the query string parameters
        var sortedQuery = query.ApplySort(sortParams);

        var products = await sortedQuery.ToListAsync();
        return Ok(products);
    }
}
```

**Sample API Requests:**

-   **Sort by price in ascending order:**
    `GET /api/products?sortBy=Price&sortOrder=asc`

-   **Sort by name in descending order:**
    `GET /api/products?sortBy=Name&sortOrder=desc`

### 2. Multi-Level Sorting

For more complex sorting, you can pass multiple `ISortParam` objects to the `ApplySort` method. The first parameter will be used for `OrderBy`, and all subsequent parameters will be used for `ThenBy`.

This is most commonly done on the server side or with the fluent builder.

**Example using `params` array:**

```csharp
var results = _context.Products
    .ApplySort(
        new SortParams("CategoryName", "asc"), // Primary sort: by category name
        new SortParams("Price", "desc")      // Secondary sort: then by price descending
    )
    .ToList();
```

### 3. Fluent Sorting Builder

The `Sort()` extension method provides a clean, chainable API for building sort queries in your application code.

```csharp
var results = _context.Products
    .Sort()
    .ByDescending("CategoryName") // OrderByDescending("CategoryName")
    .ThenBy("Price")             // ThenBy("Price")
    .Execute()                   // Applies the sorting to the IQueryable
    .ToList();
```
