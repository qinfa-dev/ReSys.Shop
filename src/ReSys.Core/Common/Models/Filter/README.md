# Dynamic Query Filtering System

This directory contains a powerful and flexible system for dynamically filtering `IQueryable` collections based on HTTP query string parameters. It translates a filter string into a secure LINQ expression tree, allowing API clients to perform complex data queries without requiring custom server-side logic for every scenario.

---

## Features

-   **Dynamic Expression Tree Generation**: Safely converts query strings into LINQ `Where` clauses.
-   **Rich Operator Support**: Includes equality (`eq`, `ne`), comparison (`gt`, `gte`, `lt`, `lte`), string (`contains`, `startswith`, `endswith`), and collection (`in`, `notin`, `range`) operators.
-   **Logical Grouping**: Supports complex `AND` / `OR` conditions between filters.
-   **Nested Property Support**: Allows filtering on properties of related entities (e.g., `Product.Category.Name`).
-   **Flexible Naming Conventions**: Automatically maps `camelCase`, `PascalCase`, `snake_case`, and `kebab-case` from the query string to your model's properties.
-   **Fluent Builder API**: Includes a `QueryFilterBuilder` for programmatically constructing complex filter logic on the server side.

---

## Query String Format

The system parses a `filters` query string parameter. The format is designed to be expressive and URL-friendly.

### 1. Basic Filtering

The basic structure is `fieldName[operator]=value`.

`?filters=name[contains]=Laptop`

### 2. Supported Operators

| Operator | Description | Example |
| :--- | :--- | :--- |
| `eq` | Equal | `price[eq]=999` |
| `ne` | Not Equal | `status[ne]=disabled` |
| `gt` | Greater Than | `stock[gt]=0` |
| `gte` | Greater Than or Equal | `price[gte]=100` |
| `lt` | Less Than | `price[lt]=500` |
| `lte` | Less Than or Equal | `price[lte]=499.99` |
| `contains` | String contains value | `name[contains]=pro` |
| `startswith` | String starts with value | `sku[startswith]=ABC-` |
| `endswith` | String ends with value | `email[endswith]=@example.com` |
| `isnull` | Property is null | `updatedAt[isnull]` |
| `isnotnull` | Property is not null | `shippedAt[isnotnull]` |
| `in` | Value is in a list | `status[in]=active,pending` |
| `notin` | Value is not in a list | `category[notin]=legacy,old` |
| `range` | Value is between two numbers | `price[range]=100,500` |

### 3. Logical Operators (`AND` / `OR`)

-   **AND (Default)**: Multiple filter parameters are combined with `AND`.
    `?filters=price[gte]=100&stock[gt]=0`
    *(Finds items where price is >= 100 AND stock is > 0)*

-   **OR (Global)**: You can set the default logic to `OR` for all filters.
    `?filters=logic=or&name[contains]=a&name[contains]=b`
    *(Finds items where name contains 'a' OR 'b')*

-   **OR (Per-Filter)**: You can specify `OR` for individual filters by prefixing them with `or_`.
    `?filters=or_status[eq]=Shipped&or_status[eq]=Delivered`
    *(Finds items where status is Shipped OR Delivered)*

---

## How to Use

### 1. Consuming Filter Strings in an API

The easiest way to use the system is to accept `QueryFilterParams` in your API endpoint and pass it to the `ApplyFilters` extension method.

```csharp
[ApiController]
[Route("api/products")]
public class ProductsController : ControllerBase
{
    private readonly MyDbContext _context;

    // ... constructor ...

    [HttpGet]
    public async Task<IActionResult> GetProducts([FromQuery] QueryFilterParams filterParams)
    {
        var query = _context.Products.AsQueryable();

        // Apply the dynamic filters
        var filteredQuery = query.ApplyFilters(filterParams);

        var products = await filteredQuery.ToListAsync();
        return Ok(products);
    }
}
```

**Sample Request:**

`GET /api/products?filters=category.name[eq]=Electronics&price[gte]=500`

This would find all products in the "Electronics" category with a price of $500 or more.

### 2. Using the Fluent Builder

You can also construct filter logic programmatically on the server using the `QueryFilterBuilder`.

```csharp
// Build a complex filter on the server
var filters = QueryFilterBuilder.Create()
    .WithDefaultLogic(FilterLogicalOperator.All) // Top-level filters are ANDed
    .Equal("IsPublished", "true")
    .Or("Category.Name", "Laptops")
    .Or("Category.Name", "Desktops")
    .Build();

// Apply the built filters to a query
var results = _context.Products
    .ApplyFilters(filters)
    .ToList();
```
