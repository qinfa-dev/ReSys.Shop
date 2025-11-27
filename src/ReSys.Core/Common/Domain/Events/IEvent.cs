using MediatR;

namespace ReSys.Core.Common.Domain.Events;
/// <summary>
/// The base event interface that integrates with MediatR.
/// All events can be published through MediatR's IMediator.
/// </summary>
public interface IEvent : INotification
{
}
public interface IEventHandler<in TEvent> : INotificationHandler<TEvent>
    where TEvent : IEvent;