using ReSys.Core.Common.Domain.Concerns;

namespace ReSys.Core.Common.Domain.Entities;

/// <summary>
/// Represents a base entity combining identity and creator metadata.
/// </summary>
public interface IEntity<TId> : IHasCreator, IHasIdentity<TId>
    where TId : struct
{
}

/// <summary>
/// Abstract base entity providing identity and creator auditing.
/// </summary>
public abstract class Entity<TId> : IEntity<TId>
    where TId : struct
{
    /// <inheritdoc/>
    public TId Id { get; set; }

    /// <inheritdoc/>
    public DateTimeOffset CreatedAt { get; set; } = DateTimeOffset.Now;

    /// <inheritdoc/>
    public string? CreatedBy { get; set; }

    protected Entity() { }

    protected Entity(TId id)
    {
        Id = id;
    }

    protected Entity(TId id, string? createdBy)
    {
        Id = id;
        CreatedBy = createdBy;
        CreatedAt = DateTimeOffset.UtcNow;
    }
}

/// <summary>
/// Non-generic convenience base class for entities using <see cref="long"/> as identifier.
/// </summary>
public abstract class Entity : Entity<Guid>
{
    protected Entity()
    {
        Id = Guid.NewGuid();
    }
    protected Entity(Guid id) : base(id: id) { }
    protected Entity(Guid id, string? createdBy) : base(id: id,
        createdBy: createdBy) { }
}

