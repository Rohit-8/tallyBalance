using System.Net;
using System.Text;
using System.Text.Json;
using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.Extensions.Options;
using TallyBalance.Application.AI.Abstractions;
using TallyBalance.Infrastructure.AI;

namespace TallyBalance.UnitTests.AI;

/// <summary>
/// Tests for the single LLM transport that every provider (MAQ AI, Gemini, NVIDIA NIM,
/// Groq) goes through. Because one class serves all four, these tests are load-bearing
/// in an unusual way: a bug here is a bug in every AI feature, on every provider, at once.
///
/// They run against a stubbed <see cref="HttpMessageHandler"/> — no network, no API key,
/// no provider account. That is deliberate and permanent: a test suite that calls real
/// LLM endpoints is slow, bills money, fails when a provider is down, and cannot assert
/// on non-deterministic output. The rule in this project is that no test ever makes a
/// real AI call.
/// </summary>
public sealed class OpenAiCompatibleLlmProviderTests
{
    private const string ProviderName = "testprovider";

    [Fact]
    public async Task CompleteAsync_posts_to_chat_completions_with_bearer_and_model()
    {
        var handler = new StubHttpMessageHandler(
            HttpStatusCode.OK,
            """{"id":"1","model":"qwen-3.8-27b","choices":[{"message":{"role":"assistant","content":"hi"},"finish_reason":"stop"}]}""");

        var (provider, client) = CreateProvider(handler, apiKey: "sk-secret-token");

        var result = await provider.CompleteAsync(new ChatRequest(
            [new ChatMessage("user", "Hello, MAQ AI!")],
            Model: "qwen-3.8-27b"));

        Assert.Equal("hi", result.Content);
        Assert.Equal("qwen-3.8-27b", result.Model);

        // The request shape is the contract. If it drifts, every provider breaks — so
        // assert on what went over the wire rather than trusting the serializer.
        Assert.Equal(HttpMethod.Post, client.CapturedMethod);
        Assert.NotNull(client.CapturedUri);
        Assert.EndsWith("chat/completions", client.CapturedUri);
        Assert.Equal("Bearer sk-secret-token", client.CapturedAuthorization);

        using var sent = JsonDocument.Parse(client.CapturedBody!);
        Assert.Equal("qwen-3.8-27b", sent.RootElement.GetProperty("model").GetString());
        Assert.Equal("user", sent.RootElement.GetProperty("messages")[0].GetProperty("role").GetString());
        Assert.Equal("Hello, MAQ AI!", sent.RootElement.GetProperty("messages")[0].GetProperty("content").GetString());
    }

    [Fact]
    public async Task CompleteAsync_omits_response_format_when_endpoint_lacks_the_capability()
    {
        // The capability flag is a main reason the adapter is hand-written rather than an
        // SDK. Sending response_format to an endpoint that does not implement it is a
        // coin flip between ignored and 400 — and a 400 on a request we believe is valid
        // gets misdiagnosed as a model problem rather than a capability mismatch.
        var handler = new StubHttpMessageHandler(
            HttpStatusCode.OK,
            """{"choices":[{"message":{"content":"{}"},"finish_reason":"stop"}]}""");

        var (provider, client) = CreateProvider(handler, supportsJson: false);

        await provider.CompleteAsync(new ChatRequest(
            [new ChatMessage("user", "extract")], Model: "m", ResponseFormatJson: true));

        using var sent = JsonDocument.Parse(client.CapturedBody!);
        Assert.False(sent.RootElement.TryGetProperty("response_format", out _));
    }

    [Fact]
    public async Task CompleteAsync_includes_response_format_when_endpoint_supports_it()
    {
        var handler = new StubHttpMessageHandler(
            HttpStatusCode.OK,
            """{"choices":[{"message":{"content":"{}"},"finish_reason":"stop"}]}""");

        var (provider, client) = CreateProvider(handler, supportsJson: true);

        await provider.CompleteAsync(new ChatRequest(
            [new ChatMessage("user", "extract")], Model: "m", ResponseFormatJson: true));

        using var sent = JsonDocument.Parse(client.CapturedBody!);
        Assert.Equal("json_object",
            sent.RootElement.GetProperty("response_format").GetProperty("type").GetString());
    }

    [Fact]
    public async Task CompleteAsync_reports_usage_counts_when_the_endpoint_provides_them()
    {
        var handler = new StubHttpMessageHandler(
            HttpStatusCode.OK,
            """{"choices":[{"message":{"content":"ok"},"finish_reason":"stop"}],"usage":{"prompt_tokens":12,"completion_tokens":5}}""");

        var (provider, _) = CreateProvider(handler);

        var result = await provider.CompleteAsync(new ChatRequest(
            [new ChatMessage("user", "x")], Model: "m"));

        Assert.Equal(12, result.PromptTokens);
        Assert.Equal(5, result.CompletionTokens);
    }

    [Fact]
    public async Task CompleteAsync_tolerates_an_endpoint_that_omits_usage()
    {
        // Not every OpenAI-compatible endpoint returns usage — some self-hosted servers
        // omit it entirely. The fields are nullable rather than int so a provider that
        // leaves them out is neither a crash nor a misleading zero: cost reporting must
        // distinguish "zero tokens" from "not reported".
        var handler = new StubHttpMessageHandler(
            HttpStatusCode.OK,
            """{"choices":[{"message":{"content":"ok"},"finish_reason":"stop"}]}""");

        var (provider, _) = CreateProvider(handler);

        var result = await provider.CompleteAsync(new ChatRequest(
            [new ChatMessage("user", "x")], Model: "m"));

        Assert.Null(result.PromptTokens);
        Assert.Null(result.CompletionTokens);
        Assert.Equal("ok", result.Content);
    }

    [Theory]
    [InlineData(HttpStatusCode.TooManyRequests)]
    [InlineData(HttpStatusCode.InternalServerError)]
    [InlineData(HttpStatusCode.BadGateway)]
    public async Task CompleteAsync_retries_retryable_failures_then_succeeds(HttpStatusCode status)
    {
        var handler = new StubHttpMessageHandler(retryableStatus: status, failFirst: 1);

        var (provider, _) = CreateProvider(handler, maxRetries: 2);

        var result = await provider.CompleteAsync(new ChatRequest(
            [new ChatMessage("user", "x")], Model: "m"));

        Assert.Equal("recovered", result.Content);
        Assert.Equal(2, handler.CallCount);
    }

    [Theory]
    [InlineData(HttpStatusCode.BadRequest)]   // malformed request — same request fails forever
    [InlineData(HttpStatusCode.Unauthorized)] // bad key — retrying cannot fix credentials
    [InlineData(HttpStatusCode.Forbidden)]
    [InlineData(HttpStatusCode.NotFound)]     // wrong BaseUrl path — a config error
    public async Task CompleteAsync_does_not_retry_deterministic_failures(HttpStatusCode status)
    {
        // The most expensive bug this test prevents is retrying a 401. Each retry costs a
        // request and, against a rate-limited provider, consumes budget a legitimate call
        // needed. Deterministic failures must surface immediately.
        var handler = new StubHttpMessageHandler(status, """{"error":{"message":"nope"}}""");

        var (provider, _) = CreateProvider(handler, maxRetries: 3);

        var ex = await Assert.ThrowsAsync<LlmProviderException>(() =>
            provider.CompleteAsync(new ChatRequest([new ChatMessage("user", "x")], Model: "m")));

        Assert.False(ex.IsRetryable);
        Assert.Equal((int)status, ex.StatusCode);
        Assert.Equal(1, handler.CallCount);
    }

    [Fact]
    public async Task CompleteAsync_throws_non_retryable_when_a_200_body_is_not_a_valid_completion()
    {
        // A 200 whose body is not the documented shape means the endpoint is not actually
        // OpenAI-compatible — typically a BaseUrl pointing at the wrong path, which is the
        // classic way to hit an HTML login page. Retrying a malformed body spends money to
        // learn nothing new, so this must not be classified as transient.
        var handler = new StubHttpMessageHandler(HttpStatusCode.OK, "<html>gateway error</html>");

        var (provider, _) = CreateProvider(handler, maxRetries: 3);

        var ex = await Assert.ThrowsAsync<LlmProviderException>(() =>
            provider.CompleteAsync(new ChatRequest([new ChatMessage("user", "x")], Model: "m")));

        Assert.False(ex.IsRetryable);
        Assert.Equal(200, ex.StatusCode);
        Assert.Equal(1, handler.CallCount);
    }

    [Fact]
    public async Task CompleteAsync_throws_when_the_endpoint_returns_no_choices()
    {
        // Some gateways return 200 with an empty choices array when a model is not loaded.
        // Treating that as an empty string would silently produce empty extractions, which
        // downstream reads as "this document had no line items" — a wrong answer presented
        // as a successful call.
        var handler = new StubHttpMessageHandler(HttpStatusCode.OK, """{"choices":[]}""");

        var (provider, _) = CreateProvider(handler);

        var ex = await Assert.ThrowsAsync<LlmProviderException>(() =>
            provider.CompleteAsync(new ChatRequest([new ChatMessage("user", "x")], Model: "m")));

        Assert.False(ex.IsRetryable);
    }

    [Fact]
    public async Task CompleteAsync_rejects_an_empty_message_list_before_touching_the_network()
    {
        // Validated before the request is built: an empty conversation is a programming
        // error, and sending it would burn a provider round-trip to receive a 400 that
        // tells us less than the exception does.
        var handler = new StubHttpMessageHandler(HttpStatusCode.OK, "{}");
        var (provider, _) = CreateProvider(handler);

        await Assert.ThrowsAsync<ArgumentException>(() =>
            provider.CompleteAsync(new ChatRequest([], Model: "m")));

        Assert.Equal(0, handler.CallCount);
    }

    [Fact]
    public void Constructor_throws_for_an_unconfigured_provider_name()
    {
        // Failing at composition rather than on the first AI call in production. A typo in
        // AI:Providers is the most likely configuration mistake, so the message names both
        // the requested and the configured providers.
        var options = Options.Create(new AiProviderOptions
        {
            DefaultProvider = "missing",
            Providers = { ["maq"] = new AiEndpointOptions { BaseUrl = "https://x/v1", ModelId = "m" } },
        });

        var ex = Assert.Throws<InvalidOperationException>(() => new OpenAiCompatibleLlmProvider(
            new HttpClient(), "missing", options, NullLogger<OpenAiCompatibleLlmProvider>.Instance));

        Assert.Contains("missing", ex.Message);
        Assert.Contains("maq", ex.Message);
    }

    [Fact]
    public void Capabilities_reflect_configuration_not_assumption()
    {
        var (provider, _) = CreateProvider(
            new StubHttpMessageHandler(HttpStatusCode.OK, "{}"),
            supportsJson: false, supportsTools: true);

        Assert.False(provider.Capabilities.SupportsJsonResponseFormat);
        Assert.True(provider.Capabilities.SupportsToolCalling);
    }

    // ---- helpers -------------------------------------------------------------

    private static (OpenAiCompatibleLlmProvider Provider, CapturingHttpClient Client) CreateProvider(
        HttpMessageHandler handler,
        string? apiKey = "sk-test",
        bool supportsJson = true,
        bool supportsTools = false,
        int maxRetries = 0)
    {
        var options = Options.Create(new AiProviderOptions
        {
            DefaultProvider = ProviderName,
            Providers =
            {
                [ProviderName] = new AiEndpointOptions
                {
                    BaseUrl = "https://llm.example.test/v1",
                    ApiKey = apiKey,
                    ModelId = "default-model",
                    MaxRetries = maxRetries,
                    SupportsJsonResponseFormat = supportsJson,
                    SupportsToolCalling = supportsTools,
                    TimeoutSeconds = 5,
                },
            },
        });

        var client = new CapturingHttpClient(handler);

        return (new OpenAiCompatibleLlmProvider(
            client, ProviderName, options, NullLogger<OpenAiCompatibleLlmProvider>.Instance), client);
    }

    /// <summary>
    /// Captures the outgoing request by overriding <see cref="HttpMessageInvoker"/>
    /// behaviour at the client level. Capturing here rather than inside the handler keeps
    /// the handler a pure responder and lets each test assert on the literal bytes sent —
    /// asserting on the payload object instead would only re-assert the serializer and
    /// would miss a wrong property name or a missing snake_case mapping.
    /// </summary>
    private sealed class CapturingHttpClient(HttpMessageHandler handler) : HttpClient(handler)
    {
        public HttpMethod? CapturedMethod { get; private set; }
        public string? CapturedUri { get; private set; }
        public string? CapturedAuthorization { get; private set; }
        public string? CapturedBody { get; private set; }

        public override async Task<HttpResponseMessage> SendAsync(
            HttpRequestMessage request, CancellationToken cancellationToken)
        {
            CapturedMethod = request.Method;
            CapturedUri = request.RequestUri?.ToString();
            CapturedAuthorization = request.Headers.Authorization?.ToString();

            if (request.Content is not null)
            {
                // Read before sending: an HttpContent stream is not reliably re-readable.
                CapturedBody = await request.Content.ReadAsStringAsync(cancellationToken);
            }

            return await base.SendAsync(request, cancellationToken);
        }
    }

    private sealed class StubHttpMessageHandler : HttpMessageHandler
    {
        private const string RecoveredBody =
            """{"choices":[{"message":{"content":"recovered"},"finish_reason":"stop"}]}""";

        private readonly HttpStatusCode _status;
        private readonly string _body;
        private readonly HttpStatusCode? _retryableStatus;
        private readonly int _failFirst;

        public int CallCount { get; private set; }

        public StubHttpMessageHandler(HttpStatusCode status, string body)
        {
            _status = status;
            _body = body;
        }

        public StubHttpMessageHandler(HttpStatusCode retryableStatus, int failFirst)
        {
            _status = HttpStatusCode.OK;
            _body = RecoveredBody;
            _retryableStatus = retryableStatus;
            _failFirst = failFirst;
        }

        protected override Task<HttpResponseMessage> SendAsync(
            HttpRequestMessage request, CancellationToken cancellationToken)
        {
            CallCount++;

            var failing = _retryableStatus is not null && CallCount <= _failFirst;
            var status = failing ? _retryableStatus!.Value : _status;
            var body = failing ? """{"error":{"message":"transient"}}""" : _body;

            return Task.FromResult(new HttpResponseMessage(status)
            {
                Content = new StringContent(body, Encoding.UTF8, "application/json"),
            });
        }
    }
}
