using Serilog;
using TallyBalance.Infrastructure;
using TallyBalance.Worker;

var builder = Host.CreateApplicationBuilder(args);

builder.Services.AddSerilog((services, configuration) => configuration
    .ReadFrom.Configuration(builder.Configuration)
    .ReadFrom.Services(services)
    .Enrich.FromLogContext()
    .Enrich.WithProperty("Application", "TallyBalance.Worker")
    .WriteTo.Console());

// Note: AddAiProviders() is deliberately NOT called here. The worker does not call a
// model in Phase 1, and AI options are validated on start — registering them in a host
// with no AI section would fail boot for an unrelated reason. When Phase 9 moves AI
// work into the worker, this call is added and the config section comes with it.
builder.Services.AddInfrastructure();
builder.Services.AddHostedService<OutboxProcessor>();

var host = builder.Build();
host.Run();

/// <summary>Exposed for integration tests via <c>HostFactoryResolver</c>.</summary>
public partial class Program;
