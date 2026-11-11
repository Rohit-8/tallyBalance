using System.ComponentModel.DataAnnotations;

namespace TallyBalance.Application.AI.Abstractions;

/// <summary>
/// Configuration for one named AI provider. Bound from the <c>AI:Providers</c>
/// configuration section.
///
/// The whole point of this shape is that <em>adding a provider is a configuration
/// change, not a code change</em>. A provider is fully described by a base URL, a key,
/// and a model id — which is exactly what every supported endpoint (MAQ AI, Gemini,
/// NVIDIA NIM, Groq) accepts, because all of them implement the OpenAI
/// chat-completions contract.
/// </summary>
public sealed class AiProviderOptions
{
    public const string SectionName = "AI";

    /// <summary>Key of the provider used when a caller does not name one explicitly.</summary>
    [Required]
    public string DefaultProvider { get; set; } = string.Empty;

    /// <summary>Providers by name, e.g. <c>maq</c>, <c>gemini</c>, <c>nim</c>, <c>groq</c>.</summary>
    [Required]
    public Dictionary<string, AiEndpointOptions> Providers { get; set; } = new();
}

/// <summary>Connection and generation settings for a single OpenAI-compatible endpoint.</summary>
public sealed class AiEndpointOptions
{
    /// <summary>
    /// Root of the OpenAI-compatible API, including the <c>/v1</c> suffix, e.g.
    /// <c>https://llm.maqsoftware.net/v1</c>. The adapter appends <c>/chat/completions</c>.
    /// </summary>
    [Required]
    public string BaseUrl { get; set; } = string.Empty;

    /// <summary>
    /// Bearer token. Comes from user-secrets in development and a secret store in
    /// production — never from a committed file. Left null in tests, where a fake
    /// provider replaces the real one entirely.
    /// </summary>
    public string? ApiKey { get; set; }

    /// <summary>Default model id for this endpoint, e.g. <c>qwen-3.8-27b</c>.</summary>
    [Required]
    public string ModelId { get; set; } = string.Empty;

    /// <summary>Optional default sampling temperature for this endpoint.</summary>
    public double? Temperature { get; set; }

    /// <summary>Default output-token cap. Enforced to keep a runaway generation cheap.</summary>
    public int MaxOutputTokens { get; set; } = 2048;

    /// <summary>Per-request timeout. LLM calls are slow by nature; the HTTP default is far too short.</summary>
    public int TimeoutSeconds { get; set; } = 120;

    /// <summary>Maximum retry attempts for retryable failures (429/5xx). Retries use exponential backoff.</summary>
    public int MaxRetries { get; set; } = 2;

    /// <summary>Whether this endpoint honours <c>response_format</c>. Capability, not preference.</summary>
    public bool SupportsJsonResponseFormat { get; set; }

    /// <summary>Whether this endpoint implements tool/function calling.</summary>
    public bool SupportsToolCalling { get; set; }

    /// <summary>Whether this endpoint reports token usage in responses.</summary>
    public bool SupportsUsageCounts { get; set; } = true;
}
