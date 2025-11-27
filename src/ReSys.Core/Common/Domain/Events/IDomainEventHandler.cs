namespace ReSys.Core.Common.Domain.Events;

public interface IDomainEventHandler<in TDomainEvent> : IEventHandler<TDomainEvent>
    where TDomainEvent : IDomainEvent;