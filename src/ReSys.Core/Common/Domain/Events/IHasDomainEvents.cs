
namespace ReSys.Core.Common.Domain.Events;

/// <summary>
/// Defines an entity that tracks uncommitted domain events in DDD.
/// </summary>
public interface IHasDomainEvents
{
    IReadOnlyList<IDomainEvent> DomainEvents { get; }
    void AddDomainEvent(IDomainEvent domainEvent);
    void ClearDomainEvents(); // Call after publish/commit
}