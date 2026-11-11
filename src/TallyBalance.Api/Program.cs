using HealthChecks.UI.Client;
using Microsoft.AspNetCore.Diagnostics.HealthChecks;
using Serilog;
using TallyBalance.Api.Health;
using TallyBalance.Infrastructure;

var builder = WebApplication.CreateBuilder(args);

// ---------------------------------------------------------------------------
// Logging
// ---------------------------------------------------------------------------
// Serilog replaces the default host logging. Bootstrap logging writes to console
// only until configuration is read; a startup failure that happens *during*
// configuration must still be visible, which is why Log.CloseAndFlush is in a
// finally block rather than after Run().
builder.Host.UseSerilog((context, services, configuration) => configuration
    .ReadFrom.Configuration(context.Configuration)
    .ReadFrom.Services(services)
    .Enrich.FromLogContext()
    .Enrich.WithProperty("Application", "TallyBalance.Api")
    .WriteTo.Console());

// ---------------------------------------------------------------------------
// Services
// ---------------------------------------------------------------------------
builder.Services.AddOpenApi();

builder.Services.AddProblemDetails();

builder.Services.AddInfrastructure();
builder.Services.AddAiProviders(builder.Configuration);
builder.Services.AddTallyBalanceHealthChecks(builder.Configuration);

builder.Services.AddCors(options => options.AddPolicy(
    "frontend",
    policy => policy
        .WithOrigins(builder.Configuration.GetSection("Cors:AllowedOrigins").Get<string[]>()
            ?? ["http://localhost:5173"])
        .AllowAnyHeader()
        .AllowAnyMethod()));

var app = builder.Build();

// ---------------------------------------------------------------------------
// Pipeline
// ---------------------------------------------------------------------------
app.UseSerilogRequestLogging();
app.UseExceptionHandler();
app.UseStatusCodePages();

if (app.Environment.IsDevelopment())
{
    app.MapOpenApi();
    app.UseSwaggerUI(options => options.SwaggerEndpoint("/openapi/v1.json", "TallyBalance API v1"));
}

app.UseCors("frontend");

// Liveness: is this process up? Deliberately does not depend on external systems —
// an orchestrator that restarts the API because SQL Server is slow turns one outage
// into two.
app.MapHealthChecks("/health/live", new HealthCheckOptions
{
    Predicate = _ => false,
    ResponseWriter = UIResponseWriter.WriteHealthCheckUIResponse,
});

// Readiness: can this instance actually serve traffic? Includes the dependencies the
// running code calls in this phase — SQL Server and Redis. RabbitMQ, Qdrant and MinIO
// are added when the phases that first use them arrive (see HealthCheckRegistration).
// This is the endpoint compose and any load balancer should gate on.
app.MapHealthChecks("/health/ready", new HealthCheckOptions
{
    Predicate = check => check.Tags.Contains("ready"),
    ResponseWriter = UIResponseWriter.WriteHealthCheckUIResponse,
});

app.MapGet("/", () => Results.Redirect("/health/ready"));

// A minimal authenticated-free probe the frontend calls to prove the full
// browser -> API -> (config) path works. Replaced by real endpoints in Phase 2.
app.MapGet("/api/v1/system/status", (IConfiguration config) =>
    Results.Ok(new
    {
        status = "ok",
        application = "TallyBalance.Api",
        environment = app.Environment.EnvironmentName,
        utcNow = DateTimeOffset.UtcNow,
        defaultAiProvider = config["AI:DefaultProvider"],
        configuredAiProviders = config.GetSection("AI:Providers").GetChildren()
            .Select(c => c.Key)
            .ToArray(),
    }))
    .WithName("SystemStatus");

app.Run();

/// <summary>
/// Exposed so the integration test project can reference the entry point with
/// <c>WebApplicationFactory&lt;Program&gt;</c>. Public partial class is the documented
/// pattern for top-level-statement apps.
/// </summary>
public partial class Program;
