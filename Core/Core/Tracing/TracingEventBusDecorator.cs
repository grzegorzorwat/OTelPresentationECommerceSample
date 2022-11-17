using Core.Events;
using OpenTelemetry.Trace;
using System.Diagnostics;

namespace Core.Tracing;

internal sealed class TracingEventBusDecorator: IEventBus
{
    private readonly IEventBus decorated;
    private readonly Tracer tracer;

    public TracingEventBusDecorator(IEventBus decorated, Tracer tracer)
    {
        this.decorated = decorated;
        this.tracer = tracer;
    }

    public async Task Publish(IEventEnvelope @event, CancellationToken ct)
    {
        using var span = tracer.StartActiveSpan(@event.Data.GetType().Name, SpanKind.Internal);

        try
        {
            await decorated.Publish(@event, ct);
        }
        catch (Exception ex)
        {
            span?.SetStatus(Status.Error);
            span?.RecordException(ex);
            throw;
        }
    }
}
