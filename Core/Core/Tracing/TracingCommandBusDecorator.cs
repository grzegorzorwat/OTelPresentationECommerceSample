using Core.Commands;
using OpenTelemetry.Trace;

namespace Core.Tracing;

internal sealed class TracingCommandBusDecorator: ICommandBus
{
    private readonly ICommandBus decorated;
    private readonly Tracer tracer;

    public TracingCommandBusDecorator(ICommandBus decorated, Tracer tracer)
    {
        this.decorated = decorated;
        this.tracer = tracer;
    }

    public async Task Send<TCommand>(TCommand command) where TCommand : ICommand
    {
        using var span = tracer.StartActiveSpan(typeof(TCommand).Name, SpanKind.Internal);

        try
        {
            await decorated.Send(command);
        }
        catch(Exception ex)
        {
            span?.SetStatus(Status.Error);
            span?.RecordException(ex);
            throw;
        }
    }
}
