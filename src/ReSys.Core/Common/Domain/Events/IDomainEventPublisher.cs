namespace ReSys.Core.Common.Domain.Events;

/// <summary>
/// Defines a publisher for domain events in DDD style.
/// </summary>
public interface IDomainEventPublisher
{
    Task PublishAsync(IReadOnlyList<IDomainEvent>? events, CancellationToken ct = default);
}
