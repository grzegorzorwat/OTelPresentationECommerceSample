using Microsoft.Extensions.DependencyInjection;
using Npgsql;
using OpenTelemetry.Exporter;
using OpenTelemetry.Resources;
using OpenTelemetry.Trace;
using System.Reflection;

namespace Core.WebApi.Tracing;

public static class TracingConfig
{
    public static IServiceCollection AddTracing(this IServiceCollection services, Assembly assembly) =>
        services.AddTracing(assembly.GetName().FullName, assembly.GetName().Version?.ToString());

    public static IServiceCollection AddTracing(this IServiceCollection services, string serviceName, string? serviceVersion = null)
    {
        services.AddOpenTelemetryTracing(tracerProviderBuilder =>
        {
            tracerProviderBuilder
                .AddSource(serviceName)
                .SetResourceBuilder(
                    ResourceBuilder.CreateDefault()
                        .AddService(serviceName: serviceName, serviceVersion: serviceVersion))
                .AddHttpClientInstrumentation()
                .AddAspNetCoreInstrumentation()
                .AddNpgsql()
                .AddConsoleExporter()
                .AddJaegerExporter(opt => opt.Protocol = JaegerExportProtocol.HttpBinaryThrift);
        });

        services.AddSingleton(TracerProvider.Default.GetTracer(serviceName));

        return services;
    }
}
