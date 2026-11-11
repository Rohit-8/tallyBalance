namespace TallyBalance.Application.Common.Abstractions;

/// <summary>
/// Abstraction over the system clock. Domain and application code must never call
/// <see cref="DateTimeOffset.UtcNow"/> directly: untestable time makes financial
/// behaviour unreproducible, and this application has hard date-dependent rules
/// (GST rate windows resolve against the <em>invoice date</em>, TDS thresholds are
/// annual aggregates, approval deadlines and due-date ageing are all date-driven).
/// </summary>
public interface IClock
{
    /// <summary>Current instant in UTC. Never local time — storage is always UTC.</summary>
    DateTimeOffset UtcNow { get; }

    /// <summary>
    /// Today's date in the organization's operating timezone (India: <c>Asia/Kolkata</c>).
    /// Separate from <see cref="UtcNow"/> on purpose: a "day" for accounting purposes is a
    /// local-calendar concept, and deriving it by truncating a UTC instant produces off-by-one
    /// errors for anything near midnight IST (which is 18:30 UTC the previous day).
    /// </summary>
    DateOnly Today { get; }
}
