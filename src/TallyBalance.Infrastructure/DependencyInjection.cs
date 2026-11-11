using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;
using TallyBalance.Application.AI.Abstractions;
using TallyBalance.Infrastructure.AI;

namespace TallyBalance.Infrastructure;

/// <summary>
/// Composition-root wiring for infrastructure adapters. Only the Api and Worker hosts
/// call this — that restriction is what keeps the dependency direction clean and is
/// enforced by the architecture tests.
/// </summary>
public static class DependencyInjection
{
    /// <summary>
    /// Registers infrastructure services. Deliberately does NOT register persistence
    /// here in Phase 1; that arrives with the first real entity in Phase 2.
    /// </summary>
    /// <remarks>
    /// AI registration is a separate, explicit call (<see cref="AddAiProviders"/>) rather
    /// than part of this method. AI options are validated on start, so a host that has no
    /// <c>AI</c> configuration section — the Worker in Phase 1, which does not call any
    /// model yet — would fail at boot for a reason unrelated to its own code. Making the
    /// host ask for AI is also honest about which process talks to providers.
    /// </remarks>
    public static IServiceCollection AddInfrastructure(this IServiceCollection services)
    {
        services.AddHttpClient();
        return services;
    }

    /// <summary>
    /// Registers one <see cref="ILLMProvider"/> per configured provider name, plus a
    /// keyed resolution path so a use case can ask for a specific provider by name.
    ///
    /// Named registration rather than a single resolved instance is the design choice:
    /// it means switching the default provider is a config edit, and comparing two
    /// models on the same prompt — which evaluation in Phase 10 requires — is a matter
    /// of resolving two keys, not changing configuration and restarting.
    /// </summary>
    public static IServiceCollection AddAiProviders(
        this IServiceCollection services, IConfiguration configuration)
    {
        services.AddOptions<AiProviderOptions>()
            .BindConfiguration(AiProviderOptions.SectionName)
            .ValidateDataAnnotations()
            .ValidateOnStart();

        var section = configuration.GetSection($"{AiProviderOptions.SectionName}:Providers");
        var configuredNames = section.GetChildren().Select(c => c.Key).ToArray();

        if (configuredNames.Length == 0)
        {
            throw new InvalidOperationException(
                $"No AI providers are configured under '{AiProviderOptions.SectionName}:Providers'. " +
                "A host that calls AddAiProviders must define at least one endpoint.");
        }

        var defaultProvider = configuration[$"{AiProviderOptions.SectionName}:DefaultProvider"];

        // Fail fast on a default that names nothing. Without this check the mistake
        // surfaces as a confusing resolution failure on the first AI call, far from the
        // one-character typo that caused it.
        if (string.IsNullOrWhiteSpace(defaultProvider))
        {
            throw new InvalidOperationException(
                $"'{AiProviderOptions.SectionName}:DefaultProvider' is not set. " +
                $"Configured providers: {string.Join(", ", configuredNames)}.");
        }

        if (!configuredNames.Contains(defaultProvider, StringComparer.OrdinalIgnoreCase))
        {
            throw new InvalidOperationException(
                $"'{AiProviderOptions.SectionName}:DefaultProvider' is '{defaultProvider}', which is not a " +
                $"configured provider. Configured providers: {string.Join(", ", configuredNames)}.");
        }

        Func<IServiceProvider, object?, ILLMProvider> createProvider = (sp, key) =>
        {
            var name = key as string
                ?? sp.GetRequiredService<IOptions<AiProviderOptions>>().Value.DefaultProvider;

            var clientFactory = sp.GetRequiredService<IHttpClientFactory>();
            var client = clientFactory.CreateClient(name);

            var options = sp.GetRequiredService<IOptions<AiProviderOptions>>();
            var timeout = options.Value.Providers.TryGetValue(name, out var ep)
                ? TimeSpan.FromSeconds(ep.TimeoutSeconds)
                : TimeSpan.FromSeconds(120);
            client.Timeout = timeout;

            return new OpenAiCompatibleLlmProvider(
                client,
                name,
                options,
                sp.GetRequiredService<Microsoft.Extensions.Logging.ILogger<OpenAiCompatibleLlmProvider>>());
        };

        // Each provider is registered under its own key. Note the explicit serviceKey
        // argument: AddKeyedTransient also has an overload whose second parameter is the
        // key, so an inline lambda without a key binds to that instead — the service then
        // registers itself as its own implementation type, compiles cleanly, and throws
        // "Cannot instantiate implementation type" at host start. Passing the key
        // positionally removes the ambiguity, and the boot test is what catches it.
        foreach (var name in configuredNames)
        {
            services.AddKeyedTransient<ILLMProvider>(name, createProvider);
        }

        // The unkeyed default: whatever DefaultProvider names. Application code depends
        // on ILLMProvider and stays indifferent to which endpoint answers.
        services.AddTransient<ILLMProvider>(sp =>
        {
            var defaultName = sp.GetRequiredService<IOptions<AiProviderOptions>>()
                .Value.DefaultProvider;
            return sp.GetRequiredKeyedService<ILLMProvider>(defaultName);
        });

        return services;
    }
}
