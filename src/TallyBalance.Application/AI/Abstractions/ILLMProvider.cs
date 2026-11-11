namespace TallyBalance.Application.AI.Abstractions;

/// <summary>
/// Port for text generation. One implementation talks to any OpenAI-compatible
/// <c>/v1/chat/completions</c> endpoint; a deterministic fake serves all tests.
///
/// The interface is intentionally small. It returns text, not domain objects: turning a
/// model's prose into a typed result is the job of a higher-level service that parses
/// and validates, which keeps prompt concerns out of the transport and keeps the
/// transport swappable.
/// </summary>
public interface ILLMProvider
{
    /// <summary>Identifier of the configured provider, for logging and telemetry. Never a secret.</summary>
    string ProviderName { get; }

    /// <summary>
    /// Capabilities of the configured endpoint. Callers consult this instead of
    /// discovering limitations by catching exceptions at runtime.
    /// </summary>
    LlmCapabilities Capabilities { get; }

    /// <summary>Send a chat completion request and return the assistant reply.</summary>
    /// <exception cref="LlmProviderException">The call failed; see the exception for retryability.</exception>
    Task<ChatResult> CompleteAsync(ChatRequest request);
}

/// <summary>
/// Optional features an endpoint may or may not implement. Being OpenAI-compatible
/// guarantees the request/response shape, not the whole feature surface — several
/// self-hosted and gateway endpoints omit parts of it.
/// </summary>
/// <param name="SupportsJsonResponseFormat">Whether <c>response_format</c> is honoured.</param>
/// <param name="SupportsToolCalling">Whether function/tool calling is implemented.</param>
/// <param name="SupportsUsageCounts">Whether token usage is reported in responses.</param>
public sealed record LlmCapabilities(
    bool SupportsJsonResponseFormat,
    bool SupportsToolCalling,
    bool SupportsUsageCounts);

/// <summary>
/// Raised for any provider failure. Carries <see cref="IsRetryable"/> so callers can
/// distinguish a transient 429/5xx — worth one bounded retry with backoff — from a
/// 400 or a 401, which will fail identically forever and must surface immediately.
/// </summary>
public sealed class LlmProviderException : Exception
{
    public LlmProviderException(
        string providerName,
        string message,
        bool isRetryable,
        int? statusCode = null,
        Exception? inner = null)
        : base($"[{providerName}] {message}", inner)
    {
        ProviderName = providerName;
        IsRetryable = isRetryable;
        StatusCode = statusCode;
    }

    public string ProviderName { get; }

    /// <summary>True when retrying with backoff could plausibly succeed.</summary>
    public bool IsRetryable { get; }

    /// <summary>HTTP status when the failure came from the endpoint itself.</summary>
    public int? StatusCode { get; }
}
