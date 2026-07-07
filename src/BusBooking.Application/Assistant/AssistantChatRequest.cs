namespace BusBooking.Application.Assistant;

public sealed record AssistantChatRequest(
    Guid UserId,
    AssistantRole Role,
    string Message,
    IReadOnlyList<AiMessage> History);

// Kind is a discriminator the Angular chat UI switches on to render a structured card
// ("schedules" | "bookings" | "booking" | "cancel-suggestion" | "vendor-buses" | "vendor-schedules")
// instead of dumping raw JSON as prose. DataJson is the same payload the model itself received.
public sealed record AssistantToolResult(string Kind, string DataJson);

public sealed record AssistantChatResponse(string Reply, IReadOnlyList<AssistantToolResult> ToolResults);
