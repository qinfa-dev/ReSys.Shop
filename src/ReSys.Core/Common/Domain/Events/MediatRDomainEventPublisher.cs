using MediatR;

using Microsoft.Extensions.DependencyInjection;

namespace ReSys.Core.Common.Domain.Events;

/// <summary>
/// Implements the domain event publisher using MediatR's notification publishing.
/// Each domain event is published sequentially to ensure order, but can be parallelized if needed.
/// </summary>
public class MediatRDomainEventPublisher(IMediator mediator) : IDomainEventPublisher
{
    private readonly IMediator _mediator = mediator ?? throw new ArgumentNullException(paramName: nameof(mediator));

    /// <inheritdoc />
    public async Task PublishAsync(IReadOnlyList<IDomainEvent>? events, CancellationToken ct = default)
    {
        if (events == null || events.Count <= 0) return;

        // Sequential publishing to preserve order; use Task.WhenAll for parallel if events are independent
        foreach (IDomainEvent @event in events)
        {
            await _mediator.Publish(notification: @event,
                cancellationToken: ct).ConfigureAwait(continueOnCapturedContext: false);
        }
    }
}

/// <summary>
/// Extension methods for registering MediatR domain event publisher in DI containers (e.g., Microsoft.Extensions.DependencyInjection).
/// </summary>
public static class MediatRDomainEventPublisherExtensions
{
    /// <summary>
    /// Registers the MediatR domain event publisher as a singleton in the service collection.
    /// Assumes MediatR is already configured.
    /// </summary>
    public static IServiceCollection AddMediatRDomainEventPublisher(this IServiceCollection services)
    {
        services.AddSingleton<IDomainEventPublisher, MediatRDomainEventPublisher>();
        return services;
    }
}