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
        return new SpanContext(ActivityContext.Parse(traceParent, null));
    }

    public static SpanContext Parse(string? traceId, string? spanId, bool recorded = true)
    {
        ActivityTraceId activityTraceId;

        try
        {
            activityTraceId = traceId is not null
                ? ActivityTraceId.CreateFromString(traceId)
                : ActivityTraceId.CreateRandom();
        }
        catch (ArgumentOutOfRangeException)
        {
            activityTraceId = ActivityTraceId.CreateRandom();
        }

        ActivitySpanId activitySpanId;

        try
        {
            activitySpanId = spanId is not null
                ? ActivitySpanId.CreateFromString(spanId)
                : ActivitySpanId.CreateRandom();
        }
        catch (ArgumentOutOfRangeException)
        {
            activitySpanId = ActivitySpanId.CreateRandom();
        }

        return new SpanContext(activityTraceId, activitySpanId, ActivityTraceFlags.Recorded);
    }
}
