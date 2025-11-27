# CQRS (Command Query Responsibility Segregation) Core

This folder provides the core interfaces for implementing a CQRS architecture using the Mediator pattern (via the [MediatR](https://github.com/jbogard/MediatR) library). This architectural pattern separates read operations (**Queries**) from write operations (**Commands**), leading to a more organized, scalable, and maintainable system.

A key feature of this implementation is the standardized use of `ErrorOr<T>` for all handler return types, which provides a robust and explicit way to manage validation and business rule failures without relying on exceptions for control flow.

---

## Core Components

### Commands

Commands represent an intent to change the state of the system. They are imperative and should be named accordingly (e.g., `CreateProductCommand`, `UpdateOrderStatusCommand`).

-   `ICommand`: A command that does not return a value. Its handler returns `ErrorOr<Unit>`.
-   `ICommand<TResult>`: A command that returns a value upon successful completion. Its handler returns `ErrorOr<TResult>`.
-   `ICommandHandler<TCommand, TResult>`: The interface that a command handler must implement.

### Queries

Queries represent a request for information and should not alter the state of the system. They are descriptive and should be named accordingly (e.g., `GetProductByIdQuery`, `GetAllCustomersQuery`).

-   `IQuery<TResult>`: A query that returns a result of type `TResult`. Its handler returns `ErrorOr<TResult>`.
-   `IQueryHandler<TQuery, TResult>`: The interface that a query handler must implement.

---

## Error Handling with `ErrorOr<T>`

Every command and query handler returns an `ErrorOr<T>` object. This enforces a consistent pattern for handling operations that can fail.

-   **On Success**: The handler returns the expected result (or `Unit.Value` for void commands).
-   **On Failure**: Instead of throwing an exception for a predictable error (e.g., validation failed, entity not found), the handler returns an `Error` object.

In the application layer (e.g., an API endpoint), you can then use the `Match` method on the result to handle success and failure cases explicitly and transform them into the appropriate HTTP response.

---

## How to Use

### 1. Creating a Command

First, define the command and its handler.

**Command Definition:**
```csharp
// The command holds all the data needed to perform the action.
public record CreateProductCommand(string Sku, string Name) : ICommand<Guid>;
```

**Command Handler:**
```csharp
public class CreateProductCommandHandler : ICommandHandler<CreateProductCommand, Guid>
{
    private readonly IProductRepository _productRepository;

    public CreateProductCommandHandler(IProductRepository productRepository)
    {
        _productRepository = productRepository;
    }

    public async Task<ErrorOr<Guid>> Handle(CreateProductCommand command, CancellationToken cancellationToken)
    {
        // 1. Perform validation
        if (await _productRepository.SkuExistsAsync(command.Sku))
        {
            return Error.Conflict("Product.Sku.Exists", "A product with this SKU already exists.");
        }

        // 2. Create and save the entity
        var product = Product.Create(command.Sku, command.Name, "system");
        await _productRepository.AddAsync(product);

        // 3. Return the result
        return product.Id;
    }
}
```

### 2. Creating a Query

Define the query and its corresponding handler.

**Query Definition:**
```csharp
public record GetProductByIdQuery(Guid ProductId) : IQuery<ProductDto>;
```

**Query Handler:**
```csharp
public class GetProductByIdQueryHandler : IQueryHandler<GetProductByIdQuery, ProductDto>
{
    private readonly IProductReadService _productReadService;

    public GetProductByIdQueryHandler(IProductReadService productReadService)
    {
        _productReadService = productReadService;
    }

    public async Task<ErrorOr<ProductDto>> Handle(GetProductByIdQuery query, CancellationToken cancellationToken)
    {
        var product = await _productReadService.GetByIdAsync(query.ProductId);

        if (product is null)
        {
            return Error.NotFound("Product.NotFound", "The product was not found.");
        }

        return product;
    }
}
```

### 3. Dispatching from an Endpoint

Inject `IMediator` into your controller or API endpoint and use `Send()` to dispatch the command or query. Use `Match()` to handle the result.

```csharp
[ApiController]
[Route("api/products")]
public class ProductsController : ControllerBase
{
    private readonly IMediator _mediator;

    public ProductsController(IMediator mediator)
    {
        _mediator = mediator;
    }

    [HttpPost]
    public async Task<IActionResult> CreateProduct(CreateProductCommand command)
    {
        ErrorOr<Guid> result = await _mediator.Send(command);

        return result.Match(
            // Success: Return a 201 Created response
            id => CreatedAtAction(nameof(GetProductById), new { id }, id),
            // Failure: Map the first error to an appropriate HTTP status code
            errors => Problem(errors)
        );
    }

    [HttpGet("{id:guid}")]
    public async Task<IActionResult> GetProductById(Guid id)
    {
        var query = new GetProductByIdQuery(id);
        ErrorOr<ProductDto> result = await _mediator.Send(query);

        return result.Match(
            product => Ok(product),
            errors => Problem(errors)
        );
    }
}
```
