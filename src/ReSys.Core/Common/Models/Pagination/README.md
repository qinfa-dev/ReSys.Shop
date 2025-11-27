# IQueryable Pagination System

This folder contains a set of extension methods and parameter classes to apply consistent and safe pagination logic to `IQueryable` sources. This is an essential feature for any API that returns lists of data, as it ensures that responses are sent in manageable, predictable chunks.

---

## Features

-   **Standardized Parameters**: Uses `IPagingParam` (`PageSize`, `PageIndex`) as the contract for all pagination requests.
-   **Safe by Default**: Automatically applies a default page size if no parameters are specified, preventing accidental queries that could return millions of records.
-   **Normalized Page Sizes**: Enforces a maximum page size to maintain API performance and prevent abuse.
-   **Flexible Paging Logic**: Provides two distinct extension methods for common scenarios:
    -   `ApplyPagingOrDefault`: **Always paginates** the query. If no parameters are given, it returns the first page with a default page size.
    -   `ApplyPagingOrAll`: Paginates only if parameters are provided. If not, it returns all results up to a configurable safety limit (`MaxAllItemsLimit`).

---

## How to Use

The primary use case is to accept paging parameters in an API endpoint and apply them to a query before executing it.

### 1. Accepting Paging Parameters

Your API endpoint can accept `PagingParams` directly from the query string.

```csharp
[ApiController]
[Route("api/products")]
public class ProductsController : ControllerBase
{
    // ...

    [HttpGet]
    public async Task<IActionResult> GetProducts([FromQuery] PagingParams pagingParams)
    {
        // ...
    }
}
```

**Sample Request:**

`GET /api/products?pageIndex=0&pageSize=20`

This requests the first page of products with a page size of 20.

### 2. Applying Pagination to a Query

Inside your endpoint, use the `ApplyPagingOrDefault` extension method on your `IQueryable`.

```csharp
[HttpGet]
public async Task<IActionResult> GetProducts([FromQuery] PagingParams pagingParams)
{
    var query = _context.Products.OrderBy(p => p.Name);

    // Apply the pagination logic to the query
    var pagedQuery = query.ApplyPagingOrDefault(pagingParams);

    // Execute the query to get only the items for the requested page
    var products = await pagedQuery.ToListAsync();

    return Ok(products);
}
```

### 3. Creating a Full Paged Response

Typically, you need to return not just the items for the current page, but also the pagination metadata (like `totalCount`, `totalPages`, etc.). To do this, you must first count the total number of items *before* applying pagination.

```csharp
[HttpGet("paged-list")]
public async Task<IActionResult> GetProductsAsPagedList([FromQuery] PagingParams pagingParams)
{
    var query = _context.Products.AsQueryable();

    // 1. Get the total count BEFORE applying pagination
    var totalCount = await query.CountAsync();

    // 2. Apply paging to get the items for the current page
    var pagedQuery = query.ApplyPagingOrDefault(pagingParams);
    var items = await pagedQuery.ToListAsync();

    // 3. Create a PagedList or a custom response object with the items and metadata
    // (Assuming a PagedList<T> class exists that takes items, count, page number, and page size)
    var pagedResponse = new PagedList<Product>(
        items, 
        totalCount, 
        pagingParams.EffectivePageNumber(), // Helper extension method
        pagingParams.PageSize ?? 10
    );

    return Ok(pagedResponse);
}
```
> **Note**: This manual process can be simplified by creating a `ToPagedListAsync` extension method that encapsulates the counting, paging, and list creation steps into a single call. See the `Paging.Extensions.cs` file for how this could be implemented.
