using Core.Events;
using Core.Tracing;
using Marten;
using Marten.Events;
using Microsoft.Extensions.DependencyInjection;
using OpenTelemetry.Trace;
using System.Diagnostics;

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
        var tracer = serviceProvider.GetRequiredService<Tracer>();
        using var consumeSpan = tracer.StartActiveSpan($"{nameof(MartenEventPublisher)}.{nameof(MartenEventPublisher.ConsumeAsync)}", SpanKind.Internal);

        foreach (var @event in streamActions.SelectMany(streamAction => streamAction.Events))
        {
            try
            {
                Activity.Current = null;
                var links = new List<Link>(1);

                if (consumeSpan is not null)
                {
                    links.Add(new Link(consumeSpan.Context));
                }

                using var span = tracer.StartActiveSpan(nameof(MartenEventPublisher), SpanKind.Producer,
                    TracingHelper.Parse(@event.CorrelationId, @event.CausationId), links: links);

                using var scope = serviceProvider.CreateScope();
                var eventBus = scope.ServiceProvider.GetRequiredService<IEventBus>();

                var eventMetadata = new EventMetadata(
                    @event.Id.ToString(),
                    (ulong)@event.Version,
                    (ulong)@event.Sequence
                );

                await eventBus.Publish(EventEnvelopeFactory.From(@event.Data, eventMetadata), ct);
            }
            finally
            {
                Tracer.WithSpan(consumeSpan);
            }
        }
    }
}
