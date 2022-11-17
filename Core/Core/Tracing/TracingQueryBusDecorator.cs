using Core.Queries;
using OpenTelemetry.Trace;

namespace Core.Tracing;

internal sealed class TracingQueryBusDecorator: IQueryBus
{
    private readonly IQueryBus decorated;
    private readonly Tracer tracer;

    public TracingQueryBusDecorator(IQueryBus decorated, Tracer tracer)
    {
        this.decorated = decorated;
        this.tracer = tracer;
    }

    public async Task<TResponse> Send<TQuery, TResponse>(TQuery query) where TQuery : IQuery<TResponse>
    {
        using var span = tracer.StartActiveSpan(typeof(TQuery).Name, SpanKind.Internal);
        return await decorated.Send<TQuery, TResponse>(query);
    }
}
