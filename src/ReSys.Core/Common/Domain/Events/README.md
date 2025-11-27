# Core Domain Events System

This directory contains the core components for a Domain-Driven Design (DDD) eventing system built upon the MediatR library. It provides a structured way for domain entities to raise events about things that have happened. These events can then be handled by other parts of the application in a decoupled and maintainable manner.

This pattern is central to creating a reactive and side-effect-free domain model, where handlers are responsible for executing logic that is not core to the entity's state change (e.g., sending emails, updating read models, or integrating with other systems).

---

## Core Components

-   `IEvent`
    The root marker interface for all events in the system. It inherits from MediatR's `INotification`, allowing any event to be published via an `IMediator` instance.

-   `IDomainEvent`
    The primary interface for events that originate from the domain model. It extends `IEvent` with DDD-specific context, such as `EventId`, `OccurredOn`, `AggregateId`, and `Version`.

-   `DomainEvent` (Record)
    A concrete base implementation of `IDomainEvent` that can be used to create new domain event types easily.

-   `IDomainEventHandler<T>`
    The interface that event handlers must implement to subscribe to a specific domain event. It inherits from MediatR's `INotificationHandler<T>`.

-   `IDomainEventPublisher`
    An abstraction for a service that can publish a list of domain events. This is typically used to dispatch all events collected from an entity after a successful database transaction.

-   `MediatRDomainEventPublisher`
    The default implementation of `IDomainEventPublisher`. It uses an `IMediator` instance to publish each domain event sequentially.

---

## Workflow

The typical flow of a domain event is as follows:

1.  **Event Creation**: A domain entity (which should implement `IHasDomainEvents`) creates an instance of a `DomainEvent` and adds it to its internal list of uncommitted events.
2.  **Event Dispatch**: After the primary database transaction is successfully committed, a mechanism (typically a `DbContext` interceptor) retrieves the collected events from all tracked entities.
3.  **Publication**: The list of events is passed to the `IDomainEventPublisher.PublishAsync` method.
4.  **Handling**: The `MediatRDomainEventPublisher` iterates through the events and uses `mediator.Publish()` for each one. MediatR then locates all registered `IDomainEventHandler` implementations for that specific event type and invokes their `Handle` method.

---

## How to Use

### 1. Define a Domain Event

Create a new record that inherits from `DomainEvent` to represent something that happened in your domain.

```csharp
public record ProductCreatedEvent(Guid ProductId, string Sku) : DomainEvent(ProductId, 1);
```

### 2. Create an Event Handler

Implement `IDomainEventHandler<T>` to handle the event. This class will be automatically discovered by MediatR.

```csharp
public class ProductCreatedEventHandler : IDomainEventHandler<ProductCreatedEvent>
{
    private readonly ILogger<ProductCreatedEventHandler> _logger;

    public ProductCreatedEventHandler(ILogger<ProductCreatedEventHandler> logger)
    {
        _logger = logger;
    }

    public Task Handle(ProductCreatedEvent notification, CancellationToken cancellationToken)
    {
        // Logic to run when a product is created, e.g., send an email, update a read model, etc.
        _logger.LogInformation("New product created: {Sku} (ID: {ProductId})", notification.Sku, notification.ProductId);
        return Task.CompletedTask;
    }
}
```

### 3. Raise the Event in an Entity

In your entity logic, add the event after a state change.

```csharp
public class Product : BaseEntity, IHasDomainEvents
{
    // This would be implemented by a base entity or explicitly.
    private readonly List<IDomainEvent> _domainEvents = new();
    public IReadOnlyList<IDomainEvent> DomainEvents => _domainEvents.AsReadOnly();
    public void AddDomainEvent(IDomainEvent domainEvent) => _domainEvents.Add(domainEvent);
    public void ClearDomainEvents() => _domainEvents.Clear();

    // Factory method to create a product
    public static Product Create(string name, string sku)
    {
        var product = new Product { Name = name, Sku = sku };

        // Add the event to the entity's list of uncommitted events
        product.AddDomainEvent(new ProductCreatedEvent(product.Id, product.Sku));

        return product;
    }
}
```

### 4. Register the Publisher in Dependency Injection

In your `Program.cs` or `Startup.cs`, ensure MediatR is registered and then add the domain event publisher.

```csharp
// In Program.cs
var builder = WebApplication.CreateBuilder(args);

// 1. Add MediatR
builder.Services.AddMediatR(cfg => 
    cfg.RegisterServicesFromAssembly(typeof(Program).Assembly));

// 2. Add the Domain Event Publisher implementation
builder.Services.AddMediatRDomainEventPublisher();
```
