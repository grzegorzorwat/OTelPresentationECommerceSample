using OpenTelemetry.Trace;
using System.Diagnostics;

namespace Core.Tracing;

public static class TracingHelper
{
    public static string ToTraceparent(this SpanContext spanContext)
    {
        return "00-" + spanContext.TraceId + "-" + spanContext.SpanId + "-" + ((int)spanContext.TraceFlags).ToString("D2");
    }

    public static SpanContext FromTraceparent(string traceParent)
    {
        return new SpanContext(ActivityContext.Parse(traceParent, null);
    }
}
