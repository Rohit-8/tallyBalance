using System.Net;
using System.Net.Http.Json;
using Microsoft.AspNetCore.Mvc.Testing;

namespace TallyBalance.IntegrationTests;

/// <summary>
/// Boots the real API in-process via <see cref="WebApplicationFactory{TEntryPoint}"/>.
///
/// Why this matters beyond "it returns 200": it proves the whole composition graph
/// resolves. DI cycles, a missing registration, a malformed options section, and a
/// middleware ordering mistake are all startup failures that unit tests cannot see
/// because they never build a service provider. Catching them here costs milliseconds;
/// catching them in a container costs a rebuild and a redeploy.
///
/// External dependencies are deliberately not required. The health checks for SQL
/// Server and Redis are registered only when a connection string is present, and the
/// test configuration supplies none — so this test verifies the HTTP surface without
/// needing containers, and stays runnable on a machine with Podman stopped. The
/// container-backed path is covered separately by the compose smoke test and, from
/// Phase 2, by Testcontainers-based tests.
/// </summary>
public sealed class ApiBootTests(WebApplicationFactory<Program> factory)
    : IClassFixture<WebApplicationFactory<Program>>
{
    [Fact]
    public async Task Liveness_endpoint_returns_200_without_touching_external_systems()
    {
        var client = factory.CreateClient();

        var response = await client.GetAsync("/health/live");

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
    }

    [Fact]
    public async Task System_status_reports_the_configured_ai_providers()
    {
        // Proves the AI options section binds and is readable end-to-end. The provider
        // list coming back populated is what tells us appsettings is shaped the way
        // AiProviderOptions expects — a section-name typo here would otherwise only
        // surface when someone tries to use AI.
        var client = factory.CreateClient();

        var response = await client.GetFromJsonAsync<SystemStatusResponse>("/api/v1/system/status");

        Assert.NotNull(response);
        Assert.Equal("ok", response.Status);
        Assert.Equal("TallyBalance.Api", response.Application);
        Assert.Contains("maq", response.ConfiguredAiProviders);
        Assert.Contains("gemini", response.ConfiguredAiProviders);
        Assert.Contains("nim", response.ConfiguredAiProviders);
        Assert.Contains("groq", response.ConfiguredAiProviders);
    }

    [Fact]
    public async Task OpenApi_document_is_served_in_development()
    {
        var client = factory.CreateClient();

        var response = await client.GetAsync("/openapi/v1.json");

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        var body = await response.Content.ReadAsStringAsync();
        Assert.Contains("TallyBalance", body);
    }

    [Fact]
    public async Task Unknown_api_route_returns_404_not_500()
    {
        // A 500 on an unmatched route usually means middleware ordering went wrong —
        // typically the exception handler sitting after something that throws. Cheap to
        // assert, and it catches a class of wiring bug nothing else covers.
        var client = factory.CreateClient();

        var response = await client.GetAsync("/api/v1/does-not-exist");

        Assert.Equal(HttpStatusCode.NotFound, response.StatusCode);
    }

    private sealed record SystemStatusResponse(
        string Status,
        string Application,
        string Environment,
        DateTimeOffset UtcNow,
        string? DefaultAiProvider,
        string[] ConfiguredAiProviders);
}
