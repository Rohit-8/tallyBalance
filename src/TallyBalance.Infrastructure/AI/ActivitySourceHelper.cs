using System.Diagnostics;

namespace TallyBalance.Infrastructure.AI;

/// <summary>
/// Single <see cref="ActivitySource"/> for AI calls, so spans are emitted under one
/// name and OpenTelemetry configuration stays in one place.
///
/// A named source (rather than <c>Activity.Current</c> reuse) is what lets the
/// OpenTelemetry SDK pick these up without instrumenting <see cref="HttpClient"/>
/// generically — and generic HTTP instrumentation would send prompt-derived URLs and
/// headers to the exporter.
/// </summary>
internal static class ActivitySourceHelper
{
    public const string Name = "TallyBalance.AI";

    public static readonly ActivitySource Source = new(Name, "1.0.0");
}
