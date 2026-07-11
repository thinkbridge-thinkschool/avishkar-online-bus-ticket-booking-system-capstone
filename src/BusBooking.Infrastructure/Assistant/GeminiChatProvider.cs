using System.Net.Http.Json;
using System.Text.Json;
using System.Text.Json.Nodes;
using BusBooking.Application.Assistant;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using Polly.CircuitBreaker;
using Polly.Timeout;

namespace BusBooking.Infrastructure.Assistant;

// Calls Google's Generative Language REST API (Gemini, AI Studio free tier) directly over
// HttpClient — no SDK dependency, since the request/response shape is small and stable enough
// to own directly, and it keeps this the same "raw HTTP + System.Text.Json" style as the rest of
// this codebase's external integrations.
//
// NOTE: Model name and API base are configurable (AiAssistant:Model / AiAssistant:BaseUrl)
// specifically so they can be bumped without a code change if Google renames/retires a model.
// Default is "gemini-flash-latest" — an alias Google repoints at its current recommended Flash
// model, rather than a pinned version, since pinned free-tier models get their quota cut over
// time (e.g. gemini-2.0-flash returned "limit: 0" for the free tier as of July 2026).
internal sealed class GeminiChatProvider(HttpClient http, IConfiguration config, ILogger<GeminiChatProvider> logger)
    : IAiChatProvider
{
    public async Task<AiCompletionResult> CompleteAsync(AiCompletionRequest request, CancellationToken ct = default)
    {
        var apiKey = config["AiAssistant:ApiKey"];
        if (string.IsNullOrWhiteSpace(apiKey))
            throw new AiProviderException("AiAssistant:ApiKey is not configured.");

        var baseUrl = config["AiAssistant:BaseUrl"] ?? "https://generativelanguage.googleapis.com/v1beta";
        var model = config["AiAssistant:Model"] ?? "gemini-flash-latest";

        var body = BuildRequestBody(request);

        HttpResponseMessage response;
        try
        {
            response = await http.PostAsJsonAsync(
                $"{baseUrl}/models/{model}:generateContent?key={apiKey}", body, ct);
        }
        catch (Exception ex) when (ex is HttpRequestException or TaskCanceledException
                                       or BrokenCircuitException or TimeoutRejectedException)
        {
            logger.LogWarning(ex, "Gemini request failed (network/timeout/resilience).");
            throw new AiProviderException("Could not reach the AI provider.", ex);
        }

        if (!response.IsSuccessStatusCode)
        {
            var errorBody = await response.Content.ReadAsStringAsync(ct);
            logger.LogWarning("Gemini returned {Status}: {Body}", response.StatusCode, Truncate(errorBody, 500));
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

        var part = json?["candidates"]?[0]?["content"]?["parts"]?[0];
        if (part is null)
            throw new AiProviderException("AI provider returned no content.");

        var functionCall = part["functionCall"];
        if (functionCall is not null)
        {
            var name = functionCall["name"]?.GetValue<string>() ?? "";
            var args = functionCall["args"]?.ToJsonString() ?? "{}";
            // Thinking-enabled Gemini models require this same value echoed back verbatim when
            // the function call is replayed into history, or the next request is rejected with
            // "Function call is missing a thought_signature" (400).
            var thoughtSignature = part["thoughtSignature"]?.GetValue<string>();
            return new AiCompletionResult(null, new AiFunctionCall(name, args, thoughtSignature));
        }

        var text = part["text"]?.GetValue<string>();
        return new AiCompletionResult(text, null);
    }

    private static object BuildRequestBody(AiCompletionRequest request)
    {
        var contents = new JsonArray();
        foreach (var message in request.History)
            contents.Add(ToContent(message));

        var body = new JsonObject
        {
            ["systemInstruction"] = new JsonObject { ["parts"] = new JsonArray(new JsonObject { ["text"] = request.SystemInstruction }) },
            ["contents"] = contents,
            ["generationConfig"] = new JsonObject { ["temperature"] = 0.3, ["maxOutputTokens"] = 800 },
        };

        if (request.Tools.Count > 0)
        {
            var declarations = new JsonArray();
            foreach (var tool in request.Tools)
            {
                declarations.Add(new JsonObject
                {
                    ["name"] = tool.Name,
                    ["description"] = tool.Description,
                    ["parameters"] = JsonNode.Parse(tool.ParametersSchemaJson),
                });
            }
            body["tools"] = new JsonArray(new JsonObject { ["functionDeclarations"] = declarations });
        }

        return body;
    }

    // Gemini's "contents" array has no dedicated tool/function role — a function response is
    // sent back as a "user" turn whose part is a functionResponse object.
    private static JsonObject ToContent(AiMessage message)
    {
        if (message.FunctionCall is { } call)
        {
            var part = new JsonObject
            {
                ["functionCall"] = new JsonObject { ["name"] = call.Name, ["args"] = JsonNode.Parse(call.ArgumentsJson) },
            };
            if (call.ProviderToken is not null)
                part["thoughtSignature"] = call.ProviderToken;

            return new JsonObject { ["role"] = "model", ["parts"] = new JsonArray(part) };
        }

        if (message.FunctionResponse is { } response)
        {
            // Gemini expects functionResponse.response to be a JSON object (a "Struct"); several
            // of our tools return a JSON array, so wrap those under a "result" key.
            var parsed = JsonNode.Parse(response.ResultJson);
            var responseNode = parsed is JsonObject ? parsed : new JsonObject { ["result"] = parsed };

            return new JsonObject
            {
                ["role"] = "user",
                ["parts"] = new JsonArray(new JsonObject
                {
                    ["functionResponse"] = new JsonObject { ["name"] = response.Name, ["response"] = responseNode },
                }),
            };
        }

        return new JsonObject
        {
            ["role"] = message.Role == AiRole.Model ? "model" : "user",
            ["parts"] = new JsonArray(new JsonObject { ["text"] = message.Text ?? "" }),
        };
    }

    private static string Truncate(string value, int max) => value.Length <= max ? value : value[..max] + "…";
}
