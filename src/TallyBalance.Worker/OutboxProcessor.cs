namespace TallyBalance.Worker;

/// <summary>
/// Placeholder for the transactional-outbox dispatcher that becomes load-bearing in
/// Phase 9, when the first domain event needs to reach RabbitMQ.
///
/// Why an outbox at all: the API writes an invoice to SQL Server and must also publish
/// "invoice approved" to RabbitMQ. Those are two systems, so they cannot share a
/// transaction. Publishing after a successful commit loses the message if the process
/// dies in between; publishing before the commit sends events for data that never
/// landed. The outbox resolves it by writing the message into an
/// <c>OUTBOX_MESSAGES</c> table <em>inside the same database transaction</em> as the
/// business change, then delivering it here, out of band, with retries. Delivery is
/// at-least-once, so consumers must be idempotent.
///
/// It is registered now, in Phase 1, for two reasons: the worker host's lifecycle,
/// configuration, and logging get exercised against something real rather than
/// described in the abstract, and the eventual shape is visible in the tree instead of
/// arriving as a surprise in Phase 9. It intentionally does no work yet — there is no
/// outbox table until persistence exists in Phase 2.
/// </summary>
public sealed class OutboxProcessor(
    ILogger<OutboxProcessor> logger,
    IConfiguration configuration) : BackgroundService
{
    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        logger.LogInformation(
            "OutboxProcessor started. Poll interval: {Interval}s. It is intentionally idle until the outbox table exists (Phase 9).",
            configuration.GetValue("Outbox:PollIntervalSeconds", 5));

        while (!stoppingToken.IsCancellationRequested)
        {
            try
            {
                await Task.Delay(
                    TimeSpan.FromSeconds(configuration.GetValue("Outbox:PollIntervalSeconds", 5)),
                    stoppingToken);

                // Phase 9: load a bounded batch of undelivered OUTBOX_MESSAGES ordered by
                // OccurredAt, publish each to RabbitMQ, mark published, and record
                // delivery attempts. Bounded batches matter: an unbounded query after an
                // outage replays millions of rows and blocks the API's own writes.
            }
            catch (OperationCanceledException) when (stoppingToken.IsCancellationRequested)
            {
                // Normal shutdown. Swallowing here is correct — a cancelled host is not an error.
                break;
            }
            catch (Exception ex)
            {
                // A background service that throws escapes ExecuteAsync and, depending on
                // host configuration, can take the process down silently. Catching and
                // continuing keeps one poison message from stopping all delivery — the
                // per-message failure handling belongs inside the batch loop in Phase 9.
                logger.LogError(ex, "Outbox dispatch iteration failed. Continuing.");
            }
        }

        logger.LogInformation("OutboxProcessor stopped.");
    }
}
