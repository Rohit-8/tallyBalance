namespace TallyBalance.Application.AI.Abstractions;

/// <summary>A single message in a chat-completions conversation.</summary>
/// <param name="Role">One of <c>system</c>, <c>user</c>, <c>assistant</c>, <c>tool</c>.</param>
/// <param name="Content">Message text. For tool results this carries the tool output.</param>
public sealed record ChatMessage(string Role, string Content);

/// <summary>
/// A request to a chat-completions endpoint. Deliberately shaped like the OpenAI
/// contract, because every provider this system must support (MAQ AI, Gemini,
/// NVIDIA NIM, Groq) exposes that contract.
/// </summary>
/// <param name="Messages">Conversation, ordered oldest first.</param>
/// <param name="Model">
/// Model identifier. Supplied per request rather than baked into the provider so a
/// single configured endpoint can serve several models, and so the value can be
/// recorded alongside the result for reproducibility.
/// </param>
/// <param name="Temperature">
/// Sampling temperature. <c>null</c> means "use the provider default" — do not invent a
/// default here, or extraction and summarisation silently change behaviour when a
/// provider is swapped.
/// </param>
/// <param name="MaxOutputTokens">Hard cap on output length; guards against runaway cost.</param>
/// <param name="ResponseFormatJson">
/// Ask the endpoint to constrain output to valid JSON. Not every OpenAI-compatible
/// endpoint supports <c>response_format</c>, so the adapter treats it as an optional
/// capability: when unsupported it is omitted and validity is enforced by parsing
/// instead. Never rely on this flag alone.
/// </param>
/// <param name="Cancellation">Propagated so a cancelled HTTP request stops paying for tokens.</param>
public sealed record ChatRequest(
    IReadOnlyList<ChatMessage> Messages,
    string Model,
    double? Temperature = null,
    int? MaxOutputTokens = null,
    bool ResponseFormatJson = false,
    CancellationToken Cancellation = default);

/// <summary>The assistant's reply from a chat-completions call.</summary>
/// <param name="Content">Generated text.</param>
/// <param name="Model">Model that actually produced the reply, echoed by the endpoint.</param>
/// <param name="PromptTokens">Tokens billed for the prompt, when the endpoint reports it.</param>
/// <param name="CompletionTokens">Tokens billed for the completion, when the endpoint reports it.</param>
/// <param name="FinishReason">Why generation stopped, e.g. <c>stop</c> or <c>length</c>.</param>
public sealed record ChatResult(
    string Content,
    string Model,
    int? PromptTokens,
    int? CompletionTokens,
    string? FinishReason);
