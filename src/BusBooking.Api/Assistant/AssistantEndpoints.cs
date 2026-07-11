using System.Security.Claims;
using BusBooking.Application.Assistant;
using BusBooking.Application.Booking.Repositories;
using BusBooking.Application.Buses;
using BusBooking.Application.Common;
using BusBooking.Application.Routes;
using BusBooking.Application.Scheduling.Repositories;
using BusBooking.Application.Vendors;

namespace BusBooking.Api.Assistant;

public static class AssistantEndpoints
{
    private const int MaxMessageLength = 2000;
    private const int MaxHistoryTurns = 20;

    public static void MapAssistantEndpoints(this WebApplication app)
    {
        app.MapGroup("/api/v1/assistant")
           .WithTags("Assistant")
           .RequireAuthorization()
           .RequireRateLimiting("assistant")
           .MapPost("/chat", Chat);
    }

    private static async Task<IResult> Chat(     // Sends a user message to the AI assistant and returns its role-aware response.
        AssistantChatBody body,
        ClaimsPrincipal principal,
        IAiChatProvider provider,
        IScheduleRepository scheduleRepo,
        IBookingRepository bookingRepo,
        IVendorRepository vendorRepo,
        IBusRepository busRepo,
        IRouteRepository routeRepo,
        ICacheService cache,
        CancellationToken ct)
    {
        var oidValue = principal.FindFirst("app:userId")?.Value;
        if (!Guid.TryParse(oidValue, out var userId))
            return Results.Unauthorized();

        if (string.IsNullOrWhiteSpace(body.Message))
            return Results.BadRequest("Message is required.");
        if (body.Message.Length > MaxMessageLength)
            return Results.BadRequest($"Message must be {MaxMessageLength} characters or fewer.");

        var role = principal.IsInRole("BusBooking.Vendor") ? AssistantRole.Vendor
                 : principal.IsInRole("BusBooking.SuperAdmin") ? AssistantRole.Admin
                 : AssistantRole.Customer;

        var history = (body.History ?? [])
            .TakeLast(MaxHistoryTurns)
            .Select(m => new AiMessage
            {
                Role = string.Equals(m.Role, "model", StringComparison.OrdinalIgnoreCase) ? AiRole.Model : AiRole.User,
                Text = m.Text,
            })
            .ToList();

        var handler = new AssistantChatHandler(provider, scheduleRepo, bookingRepo, vendorRepo, busRepo, routeRepo, cache);
        try
        {
            var response = await handler.HandleAsync(
                new AssistantChatRequest(userId, role, body.Message.Trim(), history), ct);
            return Results.Ok(response);
        }
        catch (AiProviderException)
        {
            return Results.Problem(
                detail: "The assistant is temporarily unavailable. Please try again in a moment, or check the FAQ section below.",
                statusCode: StatusCodes.Status503ServiceUnavailable);
        }
    }
}

public sealed record AssistantChatBody(string Message, IReadOnlyList<AssistantHistoryMessageBody>? History);

public sealed record AssistantHistoryMessageBody(string Role, string Text);
