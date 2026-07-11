using System.Net.Http.Headers;
using System.Net.Http.Json;
using System.Text.Json;
using System.Text.Json.Nodes;
using BusBooking.Application.Assistant;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using Polly.CircuitBreaker;
using Polly.Timeout;

namespace BusBooking.Infrastructure.Assistant;

// Calls Groq's OpenAI-compatible Chat Completions API directly over HttpClient — same
// "raw HTTP + System.Text.Json, no SDK" style as GeminiChatProvider, so the two providers are
// easy to compare side by side. Selected instead of Gemini via AiAssistant:Provider = "Groq"
// (see InfrastructureServiceExtensions) — added as a second option because Gemini's free tier
// has shown high, variable latency; nothing about IAiChatProvider, AssistantChatHandler, the
// tool set, or the /api/v1/assistant/chat endpoint changes to support this.
//
// Two format differences from Gemini this class has to bridge on its own, without touching the
// shared tool definitions or handler:
//   1. AssistantTools.cs writes JSON Schema with Gemini's UPPERCASE type keywords ("OBJECT",
//      "STRING") — valid JSON Schema requires lowercase ("object", "string"), which OpenAI-
//      compatible APIs enforce. LowercaseSchemaTypes() converts on the way out.
//   2. OpenAI-style tool results correlate by a "tool_call_id" that must be echoed back on the
//      matching "tool" role message. IAiChatProvider's AiFunctionResponse has no id field
//      (Gemini doesn't need one — it correlates by name only), so this class tracks the most
//      recent call id per tool name as it replays History, rather than requiring a contract
//      change that would also affect Gemini.
//
// NOTE: model name and API base are configurable (AiAssistant:Groq:Model / :BaseUrl) — verify
// the current model catalog at https://console.groq.com/docs/models when you set this up.
internal sealed class GroqChatProvider(HttpClient http, IConfiguration config, ILogger<GroqChatProvider> logger)
    : IAiChatProvider
{
    public async Task<AiCompletionResult> CompleteAsync(AiCompletionRequest request, CancellationToken ct = default)
    {
        var apiKey = config["AiAssistant:Groq:ApiKey"];
        if (string.IsNullOrWhiteSpace(apiKey))
            throw new AiProviderException("AiAssistant:Groq:ApiKey is not configured.");

        var baseUrl = config["AiAssistant:Groq:BaseUrl"] ?? "https://api.groq.com/openai/v1";
        var model = config["AiAssistant:Groq:Model"] ?? "llama-3.3-70b-versatile";

        var body = BuildRequestBody(request, model);

        using var httpRequest = new HttpRequestMessage(HttpMethod.Post, $"{baseUrl}/chat/completions")
        {
            Content = JsonContent.Create(body),
        };
        httpRequest.Headers.Authorization = new AuthenticationHeaderValue("Bearer", apiKey);

        HttpResponseMessage response;
        try
        {
            response = await http.SendAsync(httpRequest, ct);
        }
        catch (Exception ex) when (ex is HttpRequestException or TaskCanceledException
                                       or BrokenCircuitException or TimeoutRejectedException)
        {
            logger.LogWarning(ex, "Groq request failed (network/timeout/resilience).");
            throw new AiProviderException("Could not reach the AI provider.", ex);
        }

        if (!response.IsSuccessStatusCode)
        {
            var errorBody = await response.Content.ReadAsStringAsync(ct);
            logger.LogWarning("Groq returned {Status}: {Body}", response.StatusCode, Truncate(errorBody, 500));
            throw new AiProviderException($"AI provider returned {(int)response.StatusCode}.");
        }

        JsonNode? json;
        try
        {
            json = JsonNode.Parse(await response.Content.ReadAsStringAsync(ct));
        }
        catch (JsonException ex)
        {
            throw new AiProviderException("AI provider returned malformed JSON.", ex);
        }

        var message = json?["choices"]?[0]?["message"];
        if (message is null)
            throw new AiProviderException("AI provider returned no content.");

        var toolCalls = message["tool_calls"] as JsonArray;
        if (toolCalls is { Count: > 0 } && toolCalls[0] is { } call)
        {
            var id = call["id"]?.GetValue<string>();
            var name = call["function"]?["name"]?.GetValue<string>() ?? "";
            // OpenAI-style arguments are already a JSON-encoded string, unlike Gemini's nested object.
            var argsJson = call["function"]?["arguments"]?.GetValue<string>() ?? "{}";
            return new AiCompletionResult(null, new AiFunctionCall(name, argsJson, id));
        }

        var text = message["content"]?.GetValue<string>();
        return new AiCompletionResult(text, null);
    }

    private static object BuildRequestBody(AiCompletionRequest request, string model)
    {
        var messages = new JsonArray
        {
            new JsonObject { ["role"] = "system", ["content"] = request.SystemInstruction },
        };

        // Most recent tool_call id per function name, so a FunctionResponse (which carries no id
        // of its own — see class remarks) can be matched to the "tool_calls" entry that requested it.
        var pendingCallIds = new Dictionary<string, string>();

        foreach (var message in request.History)
            messages.Add(ToMessage(message, pendingCallIds));

        var body = new JsonObject
        {
            ["model"] = model,
            ["messages"] = messages,
            ["temperature"] = 0.3,
            ["max_tokens"] = 800,
        };

        if (request.Tools.Count > 0)
        {
            var tools = new JsonArray();
            foreach (var tool in request.Tools)
            {
                tools.Add(new JsonObject
                {
                    ["type"] = "function",
                    ["function"] = new JsonObject
                    {
                        ["name"] = tool.Name,
                        ["description"] = tool.Description,
                        ["parameters"] = LowercaseSchemaTypes(JsonNode.Parse(tool.ParametersSchemaJson)),
                    },
                });
            }
            body["tools"] = tools;
            body["parallel_tool_calls"] = false; // the handler executes one tool call per round-trip
        }

        return body;
    }

    private static JsonObject ToMessage(AiMessage message, Dictionary<string, string> pendingCallIds)
    {
        if (message.FunctionCall is { } call)
        {
            var id = call.ProviderToken ?? $"call_{Guid.NewGuid():N}";
            pendingCallIds[call.Name] = id;

            return new JsonObject
            {
                ["role"] = "assistant",
                ["tool_calls"] = new JsonArray(new JsonObject
                {
                    ["id"] = id,
                    ["type"] = "function",
                    ["function"] = new JsonObject { ["name"] = call.Name, ["arguments"] = call.ArgumentsJson },
                }),
            };
        }

        if (message.FunctionResponse is { } response)
        {
            var id = pendingCallIds.TryGetValue(response.Name, out var v) ? v : $"call_{Guid.NewGuid():N}";
            return new JsonObject { ["role"] = "tool", ["tool_call_id"] = id, ["content"] = response.ResultJson };
        }

        return new JsonObject
        {
            ["role"] = message.Role == AiRole.Model ? "assistant" : "user",
            ["content"] = message.Text ?? "",
        };
    }

    // AssistantTools.cs writes schemas with Gemini's UPPERCASE type keywords; valid JSON Schema
    // (and OpenAI-compatible function calling) requires lowercase. Recurses through
    // properties/items so nested schemas are covered too, even though today's tools are flat.
    private static JsonNode? LowercaseSchemaTypes(JsonNode? schema)
    {
        if (schema is JsonObject obj)
        {
            if (obj["type"] is JsonValue typeValue && typeValue.TryGetValue<string>(out var typeName))
                obj["type"] = typeName.ToLowerInvariant();

            foreach (var (_, child) in obj.ToList())
                LowercaseSchemaTypes(child);
        }
        else if (schema is JsonArray arr)
        {
            foreach (var item in arr)
                LowercaseSchemaTypes(item);
        }

        return schema;
    }

    private static string Truncate(string value, int max) => value.Length <= max ? value : value[..max] + "…";
}
