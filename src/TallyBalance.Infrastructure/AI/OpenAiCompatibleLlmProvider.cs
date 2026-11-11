using System.Diagnostics;
using System.Net;
using System.Net.Http.Headers;
using System.Text;
using System.Text.Json;
using System.Text.Json.Serialization;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using TallyBalance.Application.AI.Abstractions;

namespace TallyBalance.Infrastructure.AI;

/// <summary>
/// The single LLM transport for this system. Talks to any endpoint implementing the
/// OpenAI <c>/v1/chat/completions</c> contract — MAQ AI, Gemini, NVIDIA NIM, Groq, and
/// any future OpenAI-compatible service — purely by changing configuration.
///
/// Deliberately built on <see cref="HttpClient"/> rather than a vendor SDK. That is a
/// considered trade-off, not an oversight:
///
/// - A vendor SDK (e.g. the OpenAI package) is a dependency whose idea of "the API" is
///   one company's server. Pinning to it means a provider that deviates slightly — a
///   gateway that returns an extra field, a self-hosted server that omits
///   <c>usage</c> — becomes a fight with someone else's deserializer.
/// - The contract we must honour is small and stable: POST JSON in, read <c>choices[0]</c>
///   out. Hand-rolling it is roughly a hundred lines and removes a version-coupling risk
///   across four providers.
/// - Capability differences then become explicit configuration
///   (<see cref="AiEndpointOptions.SupportsJsonResponseFormat"/>) instead of hidden
///   behaviour differences between SDK versions.
///
/// If a fifth provider ever needs something genuinely non-standard, this class is the
/// only place that changes — which is the test of whether the abstraction is earning
/// its keep.
/// </summary>
public sealed class OpenAiCompatibleLlmProvider : ILLMProvider
{
    private readonly HttpClient _httpClient;
    private readonly ILogger<OpenAiCompatibleLlmProvider> _logger;
    private readonly AiEndpointOptions _options;
    private readonly string _providerName;

    private static readonly JsonSerializerOptions JsonOptions = new()
    {
        DefaultIgnoreCondition = JsonIgnoreCondition.WhenWritingNull,
        PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
    };

    public OpenAiCompatibleLlmProvider(
        HttpClient httpClient,
        string providerName,
        IOptions<AiProviderOptions> allOptions,
        ILogger<OpenAiCompatibleLlmProvider> logger)
    {
        _httpClient = httpClient;
        _providerName = providerName;
        _logger = logger;

        if (!allOptions.Value.Providers.TryGetValue(providerName, out var endpoint))
        {
            throw new InvalidOperationException(
                $"AI provider '{providerName}' is not configured. " +
                $"Configured providers: {string.Join(", ", allOptions.Value.Providers.Keys.DefaultIfEmpty("none"))}.");
        }

        _options = endpoint;

        if (string.IsNullOrWhiteSpace(_options.BaseUrl))
        {
            throw new InvalidOperationException($"AI provider '{providerName}' has no BaseUrl configured.");
        }

        // BaseUrl must include the /v1 suffix; the endpoint path is appended to it.
        _httpClient.BaseAddress = new Uri(
            _options.BaseUrl.TrimEnd('/') + "/", UriKind.Absolute);
    }

    public string ProviderName => _providerName;

    public LlmCapabilities Capabilities => new(
        SupportsJsonResponseFormat: _options.SupportsJsonResponseFormat,
        SupportsToolCalling: _options.SupportsToolCalling,
        SupportsUsageCounts: _options.SupportsUsageCounts);

    public async Task<ChatResult> CompleteAsync(ChatRequest request)
    {
        ArgumentNullException.ThrowIfNull(request);

        if (request.Messages.Count == 0)
        {
            throw new ArgumentException("A chat request must contain at least one message.", nameof(request));
        }

        var model = string.IsNullOrWhiteSpace(request.Model) ? _options.ModelId : request.Model;

        var payload = new ChatCompletionRequest
        {
            Model = model,
            Messages = request.Messages
                .Select(m => new ChatCompletionMessage { Role = m.Role, Content = m.Content })
                .ToList(),
            Temperature = request.Temperature ?? _options.Temperature,
            MaxTokens = request.MaxOutputTokens ?? _options.MaxOutputTokens,
        };

        // Only ask for JSON when the endpoint is known to honour it. Sending
        // response_format to an endpoint that does not implement it is a coin flip
        // between being ignored and returning a 400 — and a 400 on a request we believe
        // is valid is the kind of failure that gets misdiagnosed as a model problem.
        if (request.ResponseFormatJson && _options.SupportsJsonResponseFormat)
        {
            payload.ResponseFormat = new ResponseFormat { Type = "json_object" };
        }

        Exception? lastFailure = null;
        var attempts = Math.Max(1, _options.MaxRetries + 1);

        for (var attempt = 1; attempt <= attempts; attempt++)
        {
            try
            {
                return await SendAsync(payload, model, request.Cancellation);
            }
            catch (LlmProviderException ex) when (ex.IsRetryable && attempt < attempts)
            {
                lastFailure = ex;

                // Exponential backoff with a ceiling. Jitter is omitted deliberately:
                // with a single caller per request this adds little, and a fixed schedule
                // is easier to reason about in tests. Add jitter before any high-fan-out
                // batch scenario (bulk re-extraction) is introduced.
                var delay = TimeSpan.FromSeconds(Math.Pow(2, attempt - 1));

                _logger.LogWarning(
                    ex, "AI provider {Provider} attempt {Attempt}/{Attempts} failed with a retryable error. Retrying in {Delay}.",
                    _providerName, attempt, attempts, delay);

                await Task.Delay(delay, request.Cancellation);
            }
        }

        throw lastFailure ?? new LlmProviderException(
            _providerName, "All completion attempts failed without a specific error.", isRetryable: false);
    }

    private async Task<ChatResult> SendAsync(
        ChatCompletionRequest payload, string model, CancellationToken cancellation)
    {
        using var activity = StartActivity(payload, model);

        using var httpRequest = new HttpRequestMessage(
            HttpMethod.Post, "chat/completions")
        {
            Content = new StringContent(
                JsonSerializer.Serialize(payload, JsonOptions), Encoding.UTF8, "application/json"),
        };

        if (!string.IsNullOrWhiteSpace(_options.ApiKey))
        {
            httpRequest.Headers.Authorization = new AuthenticationHeaderValue("Bearer", _options.ApiKey);
        }

        HttpResponseMessage response;
        string body;

        try
        {
            response = await _httpClient.SendAsync(httpRequest, cancellation);
            body = await response.Content.ReadAsStringAsync(cancellation);
        }
        catch (TaskCanceledException ex) when (!cancellation.IsCancellationRequested)
        {
            // A timeout, not a caller cancellation. Worth retrying; a genuine
            // cancellation must never be retried, which is why the filter excludes it.
            activity?.SetStatus(ActivityStatusCode.Error, "timeout");
            throw new LlmProviderException(
                _providerName,
                $"Completion timed out after {_options.TimeoutSeconds}s.",
                isRetryable: true,
                inner: ex);
        }
        catch (HttpRequestException ex)
        {
            activity?.SetStatus(ActivityStatusCode.Error, ex.Message);
            throw new LlmProviderException(
                _providerName, $"Transport failure contacting the endpoint: {ex.Message}",
                isRetryable: true, inner: ex);
        }

        using (response)
        {
            if (!response.IsSuccessStatusCode)
            {
                var retryable = IsRetryableStatus(response.StatusCode);

                // Log the status and a truncated body only. Never log request bodies at
                // information level: prompts embed vendor names, invoice numbers, GSTINs
                // and PANs, and document text. The full body is debug-gated and truncated
                // so a misbehaving endpoint cannot flood the log.
                _logger.LogWarning(
                    "AI provider {Provider} returned {Status}. Body (truncated): {Body}",
                    _providerName, (int)response.StatusCode, Truncate(body, 2000));

                activity?.SetStatus(ActivityStatusCode.Error, ((int)response.StatusCode).ToString());

                throw new LlmProviderException(
                    _providerName,
                    $"Endpoint returned {(int)response.StatusCode} {response.StatusCode}.",
                    isRetryable: retryable,
                    statusCode: (int)response.StatusCode);
            }

            ChatCompletionResponse? parsed;
            try
            {
                parsed = JsonSerializer.Deserialize<ChatCompletionResponse>(body, JsonOptions);
            }
            catch (JsonException ex)
            {
                // A 200 whose body is not the documented shape means the endpoint is not
                // actually OpenAI-compatible. That is a configuration error, not a
                // transient one — retrying a malformed body wastes money.
                throw new LlmProviderException(
                    _providerName,
                    "Endpoint returned 200 but the body is not a valid chat completion response.",
                    isRetryable: false,
                    statusCode: 200,
                    inner: ex);
            }

            var choice = parsed?.Choices is { Count: > 0 } choices ? choices[0] : null;

            if (choice is null || choice.Message is null)
            {
                throw new LlmProviderException(
                    _providerName, "Endpoint returned a response containing no choices.",
                    isRetryable: false, statusCode: 200);
            }

            var result = new ChatResult(
                Content: choice.Message.Content ?? string.Empty,
                Model: parsed?.Model ?? model,
                PromptTokens: parsed?.Usage?.PromptTokens,
                CompletionTokens: parsed?.Usage?.CompletionTokens,
                FinishReason: choice.FinishReason);

            if (activity is not null)
            {
                activity.SetTag("llm.completion_tokens", result.CompletionTokens);
                activity.SetTag("llm.prompt_tokens", result.PromptTokens);

                // "length" means the model was cut off mid-sentence. Downstream JSON
                // parsing will fail on truncated output, so flag it here where the cause
                // is still obvious rather than letting a JSON error obscure it later.
                if (string.Equals(result.FinishReason, "length", StringComparison.Ordinal))
                {
                    _logger.LogWarning(
                        "AI provider {Provider} hit the output token cap for model {Model}. " +
                        "Increase MaxOutputTokens or shorten the expected output.",
                        _providerName, result.Model);
                }
            }

            return result;
        }
    }

    private Activity? StartActivity(ChatCompletionRequest payload, string model)
    {
        var activity = ActivitySourceHelper.Source.StartActivity(
            "llm.chat", ActivityKind.Client);

        if (activity is null)
        {
            return null;
        }

        activity.SetTag("llm.provider", _providerName);
        activity.SetTag("llm.model", model);
        activity.SetTag("llm.message_count", payload.Messages.Count);

        // Prompt text is deliberately NOT recorded: it contains vendor and tax
        // identifiers. Tracing the shape of the call is useful; tracing its contents is
        // a data-leak vector through the observability backend.
        return activity;
    }

    private static bool IsRetryableStatus(HttpStatusCode status) => status switch
    {
        // 429: rate limited — backoff is the correct response.
        HttpStatusCode.TooManyRequests => true,
        // 5xx on the provider side; a retry may land on a healthy replica.
        >= HttpStatusCode.InternalServerError => true,
        // 400/401/403/404 and 422 are deterministic: same request, same failure.
        _ => false,
    };

    private static string Truncate(string value, int max) =>
        value.Length <= max ? value : value[..max] + "...[truncated]";

    // ---- Wire format ---------------------------------------------------------
    // Mirrors the OpenAI chat-completions contract. Kept as private nested types so
    // the wire shape cannot leak into application code, which speaks only in
    // ChatRequest / ChatResult.

    private sealed class ChatCompletionRequest
    {
        public string Model { get; set; } = string.Empty;
        public List<ChatCompletionMessage> Messages { get; set; } = [];
        public double? Temperature { get; set; }
        [JsonPropertyName("max_tokens")]
        public int? MaxTokens { get; set; }
        [JsonPropertyName("response_format")]
        public ResponseFormat? ResponseFormat { get; set; }
    }

    private sealed class ChatCompletionMessage
    {
        public string Role { get; set; } = string.Empty;
        public string Content { get; set; } = string.Empty;
    }

    private sealed class ResponseFormat
    {
        public string Type { get; set; } = "json_object";
    }

    private sealed class ChatCompletionResponse
    {
        public string? Model { get; set; }
        public List<Choice>? Choices { get; set; }
        public UsageInfo? Usage { get; set; }
    }

    private sealed class Choice
    {
        public ResponseMessage? Message { get; set; }
        [JsonPropertyName("finish_reason")]
        public string? FinishReason { get; set; }
    }

    private sealed class ResponseMessage
    {
        public string? Content { get; set; }
    }

    private sealed class UsageInfo
    {
        [JsonPropertyName("prompt_tokens")]
        public int? PromptTokens { get; set; }

        [JsonPropertyName("completion_tokens")]
        public int? CompletionTokens { get; set; }
    }
}
