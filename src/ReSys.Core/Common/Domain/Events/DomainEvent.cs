using ReSys.Core.Common.Domain.Concerns;

namespace ReSys.Core.Common.Domain.Events;

// -------------------------------------------------------------------------
// REGION: CORE INTERFACES
// -------------------------------------------------------------------------

/// <summary>
/// Domain event interface representing an event in the domain model.
/// Extends IEvent to be compatible with MediatR for event publishing.
/// </summary>
public interface IDomainEvent : IEvent, IHasVersion
{
    Guid EventId { get; set; }
    DateTimeOffset OccurredOn { get; set; }
    string EventType { get; }
    object? AggregateId { get; set; }
}

// -------------------------------------------------------------------------
// REGION: BASE DOMAIN EVENT
// -------------------------------------------------------------------------

/// <summary>
/// Base implementation of domain events.
/// Can be published directly through MediatR: await mediator.Publish(BaseDomainEvent.Instance);
/// </summary>
public record DomainEvent : IDomainEvent
{
    public DomainEvent()
    {
    }

    public DomainEvent(object? aggregateId, long version)
    {
        AggregateId = aggregateId;
        Version = version;
    }

    public Guid EventId { get; set; } = Guid.NewGuid();
    public object? AggregateId { get; set; }
    public long Version { get; set; } = 1;
    public DateTimeOffset OccurredOn { get; set; } = DateTimeOffset.Now;
    public string EventType => GetType().Name;

    /// <summary>
    /// Creates a new instance of BaseDomainEvent (similar to Guid.NewGuid()).
    /// Use this for quick instantiation and chaining with SetVersion or SetAggregateId.
    /// </summary>
    public static DomainEvent Instance => new();

}
