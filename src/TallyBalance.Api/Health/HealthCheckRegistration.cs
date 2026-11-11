namespace TallyBalance.Api.Health;

/// <summary>
/// Health-check registration. Kept in its own file because the distinction between
/// the checks is the part people get wrong, and it deserves comments that would be
/// out of place in Program.cs.
/// </summary>
public static class HealthCheckRegistration
{
    public static IServiceCollection AddTallyBalanceHealthChecks(
        this IServiceCollection services, IConfiguration configuration)
    {
        var builder = services.AddHealthChecks();

        // "ready" marks a dependency check as part of READINESS. A dependency being
        // down means this instance cannot do useful work, so it should be taken out of
        // rotation — but it is NOT a reason to restart the API process itself.
        //
        // Each check is tagged rather than assigned a failure "level", because level
        // semantics differ across health-check UIs while tags do not.

        var connectionString = configuration.GetConnectionString("TallyBalance");
        if (!string.IsNullOrWhiteSpace(connectionString))
        {
            builder.AddSqlServer(
                connectionString,
                name: "sqlserver",
                tags: ["ready"]);
        }

        var redis = configuration.GetConnectionString("Redis");
        if (!string.IsNullOrWhiteSpace(redis))
        {
            builder.AddRedis(redis, name: "redis", tags: ["ready"]);
        }

        // RabbitMQ, Qdrant and MinIO are intentionally absent in Phase 1. Nothing in this
        // phase publishes to a queue, stores an object, or searches a vector index, and a
        // readiness probe that depends on services the running code never calls turns an
        // unrelated container outage into an API outage. They are added in the phases that
        // first use them (Phase 6 storage, Phase 9 messaging, Phase 11 vectors) with the
        // same "ready" tag.
        //
        // The RabbitMQ health-check package was also dropped rather than worked around:
        // its current release expects an IConnection factory, which invites a long-lived
        // connection held by the probe — a stale connection then reports the API unhealthy
        // while the application is fine. When messaging arrives, the check belongs on the
        // application's own connection pool, not a separate one.

        return services;
    }
}
