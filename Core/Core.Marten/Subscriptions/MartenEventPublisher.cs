using Core.Events;
using Core.Tracing;
using Marten;
using Marten.Events;
using Microsoft.Extensions.DependencyInjection;
using OpenTelemetry.Trace;

namespace Core.Marten.Subscriptions;

public class MartenEventPublisher: IMartenEventsConsumer
{
    private readonly IServiceProvider serviceProvider;

    public MartenEventPublisher(
        IServiceProvider serviceProvider
    )
    {
        this.serviceProvider = serviceProvider;
    }

    public async Task ConsumeAsync(IDocumentOperations documentOperations, IReadOnlyList<StreamAction> streamActions,
        CancellationToken ct)
    {
        foreach (var @event in streamActions.SelectMany(streamAction => streamAction.Events))
        {
            using var scope = serviceProvider.CreateScope();
            var eventBus = scope.ServiceProvider.GetRequiredService<IEventBus>();
            var tracer = scope.ServiceProvider.GetRequiredService<Tracer>();

            using var span = tracer.StartActiveSpan(nameof(MartenEventPublisher), SpanKind.Producer,
                TracingHelper.Parse(@event.CorrelationId, @event.CausationId));

            var eventMetadata = new EventMetadata(
                @event.Id.ToString(),
                (ulong)@event.Version,
                (ulong)@event.Sequence
            );

            await eventBus.Publish(EventEnvelopeFactory.From(@event.Data, eventMetadata), ct);
        }
    }
}
