using Confluent.Kafka;
using Core.Events;
using Core.Events.External;
using Core.Serialization.Newtonsoft;
using Core.Tracing;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using OpenTelemetry.Trace;
using System.Diagnostics;
using System.Text;

namespace Core.Kafka.Producers;

public class KafkaProducer: IExternalEventProducer
{
    private readonly Tracer tracer;
    private readonly ILogger<KafkaProducer> logger;
    private readonly KafkaProducerConfig config;

    public KafkaProducer(
        IConfiguration configuration,
        Tracer tracer,
        ILogger<KafkaProducer> logger
    )
    {
        this.tracer = tracer;
        this.logger = logger;
        // get configuration from appsettings.json
        config = configuration.GetKafkaProducerConfig();
    }

    public async Task Publish(IEventEnvelope @event, CancellationToken ct)
    {
        using var span = tracer.StartActiveSpan(nameof(KafkaProducer), SpanKind.Producer);
        span?.SetAttribute("messaging.system", "kafka");
        span?.SetAttribute("messaging.destination", config.Topic);
        span?.SetAttribute("messaging.destination_kind", "topic");

        try
        {
            using var p = new ProducerBuilder<string, string>(config.ProducerConfig).Build();
            // publish event to kafka topic taken from config

            await p.ProduceAsync(config.Topic,
                new Message<string, string>
                {
                    // store event type name in message Key
                    Key = @event.Data.GetType().Name,
                    // serialize event to message Value
                    Value = @event.ToJson(),
                    Headers = new Headers()
                    {
                        new Header("traceparent", Encoding.UTF8.GetBytes(span?.Context.ToTraceparent()!))
                    }
                }, ct).ConfigureAwait(false);
        }
        catch (Exception e)
        {
            logger.LogError("Error producing Kafka message: {Message} {StackTrace}",e.Message, e.StackTrace);
            throw;
        }
    }
}
