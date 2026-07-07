namespace BusBooking.Application.Assistant;

public enum AiRole { User, Model }

// OpenAPI-style JSON Schema as a raw string (e.g. {"type":"OBJECT","properties":{...}}) —
// passed through verbatim to whichever provider is behind IAiChatProvider.
public sealed record AiToolDefinition(string Name, string Description, string ParametersSchemaJson);

// ProviderToken is an opaque value some providers require echoed back verbatim when this call
// is replayed into history — e.g. Gemini's "thoughtSignature" on thinking-enabled models, or an
// OpenAI-style tool_call_id. Null for providers that don't need one.
public sealed record AiFunctionCall(string Name, string ArgumentsJson, string? ProviderToken = null);

public sealed record AiFunctionResponse(string Name, string ResultJson);

// A single turn in the conversation. Exactly one of Text / FunctionCall / FunctionResponse is set.
public sealed class AiMessage
{
    public required AiRole Role { get; init; }
    public string? Text { get; init; }
    public AiFunctionCall? FunctionCall { get; init; }
    public AiFunctionResponse? FunctionResponse { get; init; }
}

public sealed record AiCompletionRequest(
    string SystemInstruction,
    IReadOnlyList<AiMessage> History,
    IReadOnlyList<AiToolDefinition> Tools);

// Exactly one of Text / FunctionCall is populated, mirroring how Gemini (and OpenAI-compatible
// providers) return either a final answer or a single tool-call request per turn.
public sealed record AiCompletionResult(string? Text, AiFunctionCall? FunctionCall);

// Thrown by provider implementations on network failure, non-success responses, or malformed
// output — callers turn this into a graceful "assistant unavailable" response instead of a 500.
public sealed class AiProviderException(string message, Exception? inner = null) : Exception(message, inner);

public interface IAiChatProvider
{
    Task<AiCompletionResult> CompleteAsync(AiCompletionRequest request, CancellationToken ct = default);
}
