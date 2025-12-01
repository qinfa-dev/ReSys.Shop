using ReSys.Core.Common.Domain.Concerns;

namespace ReSys.Core.Common.Domain.Entities;

public interface IAuditableEntity<TId> :
    IEntity<TId>, IHasAuditable where TId : struct
{
}

public abstract class AuditableEntity<TId> : Entity<TId>, IAuditableEntity<TId> where TId : struct
{
    public DateTimeOffset? UpdatedAt { get; set; }
    public string? UpdatedBy { get; set; }

    protected AuditableEntity() 
    {
    }

    protected AuditableEntity(TId id) : base(id: id) { }

    protected AuditableEntity(TId id, string? createdBy) : base(id: id) 
    {
        CreatedAt = DateTimeOffset.UtcNow;
        CreatedBy = createdBy;
    }
}

public abstract class AuditableEntity : AuditableEntity<Guid>
{
    protected AuditableEntity() 
    {
        Id = Guid.NewGuid();
    }
    protected AuditableEntity(Guid id) : base(id: id) { }

    protected AuditableEntity(Guid id, string? createdBy) : base(id: id) 
    {
        CreatedAt = DateTimeOffset.UtcNow;
        CreatedBy = createdBy;
    }
}