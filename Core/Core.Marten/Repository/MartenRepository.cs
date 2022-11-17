using Core.Aggregates;
using Marten;
using OpenTelemetry.Trace;

namespace Core.Marten.Repository;

public interface IMartenRepository<T> where T : class, IAggregate
{
    Task<T?> Find(Guid id, CancellationToken cancellationToken);
    Task<long> Add(T aggregate, CancellationToken cancellationToken = default);

    Task<long> Update(T aggregate, long? expectedVersion = null, CancellationToken cancellationToken = default);

    Task<long> Delete(T aggregate, long? expectedVersion = null, CancellationToken cancellationToken = default);
}

public class MartenRepository<T>: IMartenRepository<T> where T : class, IAggregate
{
    private readonly IDocumentSession documentSession;

    public MartenRepository(
        IDocumentSession documentSession
    )
    {
        this.documentSession = documentSession;
    }

    public Task<T?> Find(Guid id, CancellationToken cancellationToken) =>
        documentSession.Events.AggregateStreamAsync<T>(id, token: cancellationToken);

    public async Task<long> Add(T aggregate, CancellationToken cancellationToken = default)
    {
        documentSession.CorrelationId = Tracer.CurrentSpan.Context.TraceId.ToString();
        documentSession.CausationId = Tracer.CurrentSpan.Context.SpanId.ToString();

        var events = aggregate.DequeueUncommittedEvents();

        documentSession.Events.StartStream<Aggregate>(
            aggregate.Id,
            events
        );

        await documentSession.SaveChangesAsync(cancellationToken);

        return events.Length;
    }

    public async Task<long> Update(T aggregate, long? expectedVersion = null, CancellationToken cancellationToken = default)
    {
        documentSession.CorrelationId = Tracer.CurrentSpan.Context.TraceId.ToString();
        documentSession.CausationId = Tracer.CurrentSpan.Context.SpanId.ToString();

        var events = aggregate.DequeueUncommittedEvents();

        var nextVersion = (expectedVersion ?? aggregate.Version) + events.Length;

        documentSession.Events.Append(
            aggregate.Id,
            nextVersion,
            events
        );

        await documentSession.SaveChangesAsync(cancellationToken);

        return nextVersion;
    }

    public Task<long> Delete(T aggregate, long? expectedVersion = null, CancellationToken cancellationToken = default) =>
        Update(aggregate, expectedVersion, cancellationToken);
}
