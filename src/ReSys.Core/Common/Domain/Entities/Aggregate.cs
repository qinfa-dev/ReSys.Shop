
using System.ComponentModel.DataAnnotations;
using ReSys.Core.Common.Domain.Concerns;
using ReSys.Core.Common.Domain.Events;

namespace ReSys.Core.Common.Domain.Entities;

/// <summary>
/// Base aggregate root class representing a domain entity with identity, auditing,
/// version control, and domain event tracking.
/// </summary>
public abstract class Aggregate<TId> : AuditableEntity<TId>, IAggregate<TId>
    where TId : struct
{
    private readonly List<IDomainEvent> _uncommittedEvents = [];

    public long Version { get; set; }



    public IReadOnlyList<IDomainEvent> DomainEvents => _uncommittedEvents.AsReadOnly();

    protected Aggregate() => Id = default!;

    protected Aggregate(TId id) : base(id: id) { }

    protected Aggregate(TId id, string? createdBy) : base(id: id,
        createdBy: createdBy)
    {
    }

    /// <summary>
    /// Adds a new domain event to the aggregate and increments the version safely.
    /// </summary>
    public void AddDomainEvent(IDomainEvent domainEvent)
    {
        ArgumentNullException.ThrowIfNull(argument: domainEvent);

        _uncommittedEvents.Add(item: domainEvent);
    }

    /// <summary>
    /// Clears all uncommitted domain events.
    /// </summary>
    public void ClearDomainEvents() => _uncommittedEvents.Clear();

    /// <summary>
    /// Indicates whether this aggregate has pending uncommitted domain events.
    /// </summary>
    public bool HasUncommittedEvents() => _uncommittedEvents.Count > 0;
}

/// <summary>
/// Non-generic convenience base class for aggregates using <see cref="Guid"/> as key.
/// </summary>
public abstract class Aggregate : Aggregate<Guid>
{
    [Timestamp]
    public byte[]? RowVersion { get; set; }
    protected Aggregate() => Id = Guid.NewGuid();
    protected Aggregate(Guid id) : base(id: id) { }

    protected Aggregate(Guid id, string? createdBy) : base(id: id,
        createdBy: createdBy)
    {
    }
}

/// <summary>
/// Defines a base aggregate root interface combining auditable entity,
/// versioning, and domain event handling.
/// </summary>
public interface IAggregate<TId> : IAuditableEntity<TId>, IHasVersion, IHasDomainEvents
    where TId : struct
{
}
