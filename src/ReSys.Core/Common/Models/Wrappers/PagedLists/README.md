# PagedList<T> and Pagination Helpers

This folder contains the `PagedList<T>` class and the extension methods (`ToPagedListAsync`, `ToPagedList`) used to create instances of it. A `PagedList<T>` is a container that holds a single page of data along with all the metadata required for a UI to render pagination controls.

---

## Core Components

-   `PagedList<T>`
    A generic class that encapsulates a page of items. It provides rich metadata properties, including:
    -   `Items`: The list of items on the current page.
    -   `TotalCount`: The total number of items across all pages.
    -   `PageNumber`: The current page number (1-based).
    -   `PageSize`: The number of items per page.
    -   `TotalPages`: The total number of pages.
    -   `HasNextPage` & `HasPreviousPage`: Booleans to indicate if more pages are available.
    -   `IsFirstPage` & `IsLastPage`: Booleans for the current page's boundary status.
    -   `StartIndex` & `EndIndex`: The 1-based index of the first and last items on the page.

-   `ToPagedListAsync<T>(this IQueryable<T>, ...)`
    The primary extension method used to create a `PagedList<T>` from a database query. It efficiently performs a `CountAsync` on the query, then applies `Skip()` and `Take()`, and finally executes `ToListAsync()` to fetch only the items for the requested page.

-   `ToPagedList<T>(this IEnumerable<T>, ...)`
    A synchronous version of the extension method for use with in-memory collections.

---

## How to Use

The most common workflow is to apply any filtering or sorting to an `IQueryable` and then call `ToPagedListAsync` as the final step before returning from an API endpoint.

### 1. Creating a PagedList in an API Endpoint

```csharp
[ApiController]
[Route("api/products")]
public class ProductsController : ControllerBase
{
    private readonly MyDbContext _context;

    // ... constructor ...

    [HttpGet]
    public async Task<ActionResult<PagedList<Product>>> GetProducts(
        [FromQuery] PagingParams pagingParams,
        [FromQuery] SortParams sortParams)
    {
        var query = _context.Products.AsQueryable();

        // 1. Apply other operations like sorting or filtering first
        query = query.ApplySort(sortParams);

        // 2. Create the PagedList. This executes the database query.
        var pagedProducts = await query.ToPagedListAsync(pagingParams);

        // 3. Return the result. The PagedList object can be serialized directly to JSON.
        return Ok(pagedProducts);
    }
}
```

**Sample JSON Response:**

```json
{
  "items": [
    { "id": "...", "name": "Product A" },
    { "id": "...", "name": "Product B" }
  ],
  "pageNumber": 1,
  "pageSize": 2,
  "totalPages": 5,
  "totalCount": 10,
  "hasPreviousPage": false,
  "hasNextPage": true
}
```

### 2. Mapping to DTOs

It is a best practice to map your domain entities to Data Transfer Objects (DTOs) before returning them from an API. The `PagedList<T>` includes a handy `.Map()` function that transforms the `Items` on the page while preserving all the pagination metadata.

```csharp
[HttpGet("dtos")]
public async Task<ActionResult<PagedList<ProductDto>>> GetProductDtos([FromQuery] PagingParams pagingParams)
{
    var query = _context.Products.AsQueryable();

    // Create the PagedList of domain entities
    var pagedProducts = await query.ToPagedListAsync(pagingParams);

    // Map the items to DTOs
    var pagedProductDtos = pagedProducts.Map(product => new ProductDto
    {
        Id = product.Id,
        Name = product.Name,
        Price = product.Price
    });

    return Ok(pagedProductDtos);
}
```
