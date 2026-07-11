using System.Text.Json;
using BusBooking.Application.Booking.Queries.GetUserBookings;
using BusBooking.Application.Booking.Repositories;
using BusBooking.Application.Buses;
using BusBooking.Application.Common;
using BusBooking.Application.Routes;
using BusBooking.Application.Scheduling.Queries.GetVendorSchedules;
using BusBooking.Application.Scheduling.Queries.SearchSchedules;
using BusBooking.Application.Scheduling.Repositories;
using BusBooking.Application.Vendors;
using BusBooking.Domain.Booking.Enums;

namespace BusBooking.Application.Assistant;

// Orchestrates one assistant turn: sends the conversation + available tools to the provider, and
// whenever it asks for a tool call, executes that call against the SAME Application handlers the
// REST endpoints use (no business logic is duplicated), feeds the result back, and repeats until
// the model produces a final answer or the round-trip cap is hit. Mutating actions are never
// exposed as tools — see suggest_cancel_booking, which only validates and defers to the real,
// user-confirmed cancel endpoint.
public sealed class AssistantChatHandler(
    IAiChatProvider provider,
    IScheduleRepository scheduleRepo,
    IBookingRepository bookingRepo,
    IVendorRepository vendorRepo,
    IBusRepository busRepo,
    IRouteRepository routeRepo,
    ICacheService cache)
{
    private const int MaxToolRoundTrips = 4;
    private static readonly JsonSerializerOptions JsonOptions = new(JsonSerializerDefaults.Web);

    public async Task<AssistantChatResponse> HandleAsync(AssistantChatRequest request, CancellationToken ct = default)
    {
        var tools = AssistantTools.ForRole(request.Role);
        var systemInstruction = BuildSystemInstruction(request.Role);

        var history = request.History.ToList();
        history.Add(new AiMessage { Role = AiRole.User, Text = request.Message });

        var toolResults = new List<AssistantToolResult>();

        for (var i = 0; i < MaxToolRoundTrips; i++)
        {
            var result = await provider.CompleteAsync(new AiCompletionRequest(systemInstruction, history, tools), ct);

            if (result.FunctionCall is null)
            {
                var reply = string.IsNullOrWhiteSpace(result.Text)
                    ? "Sorry, I wasn't able to come up with a response for that."
                    : result.Text;
                return new AssistantChatResponse(reply, toolResults);
            }

            history.Add(new AiMessage { Role = AiRole.Model, FunctionCall = result.FunctionCall });

            var (kind, resultJson) = await ExecuteToolAsync(result.FunctionCall, request, ct);
            toolResults.Add(new AssistantToolResult(kind, resultJson));

            history.Add(new AiMessage
            {
                Role = AiRole.User,
                FunctionResponse = new AiFunctionResponse(result.FunctionCall.Name, resultJson),
            });
        }

        return new AssistantChatResponse(
            "I wasn't able to finish that request — please try rephrasing, or check My Bookings directly.",
            toolResults);
    }

    private async Task<(string Kind, string ResultJson)> ExecuteToolAsync(
        AiFunctionCall call, AssistantChatRequest request, CancellationToken ct)
    {
        using var args = JsonDocument.Parse(string.IsNullOrWhiteSpace(call.ArgumentsJson) ? "{}" : call.ArgumentsJson);
        var root = args.RootElement;

        switch (call.Name)
        {
            case AssistantTools.SearchSchedules:
            {
                var fromCity = root.TryGetProperty("fromCity", out var f) ? f.GetString() ?? "" : "";
                var toCity = root.TryGetProperty("toCity", out var t) ? t.GetString() ?? "" : "";
                var travelDateText = root.TryGetProperty("travelDate", out var d) ? d.GetString() : null;

                if (string.IsNullOrWhiteSpace(fromCity) || string.IsNullOrWhiteSpace(toCity) ||
                    !DateOnly.TryParse(travelDateText, out var travelDate))
                {
                    return ("error", Serialize(new { error = "fromCity, toCity, and a valid travelDate (YYYY-MM-DD) are required." }));
                }

                var results = await new SearchSchedulesHandler(scheduleRepo, cache)
                    .HandleAsync(new SearchSchedulesQuery(fromCity.Trim(), toCity.Trim(), travelDate), ct);
                return ("schedules", Serialize(results.Take(10)));
            }

            case AssistantTools.GetMyBookings:
            {
                var results = await new GetUserBookingsHandler(bookingRepo)
                    .HandleAsync(new GetUserBookingsQuery(request.UserId), ct);
                return ("bookings", Serialize(results));
            }

            case AssistantTools.GetBookingById:
            {
                if (!TryGetGuid(root, "bookingId", out var bookingId))
                    return ("error", Serialize(new { error = "A valid bookingId is required." }));

                var booking = await bookingRepo.GetByIdReadOnlyAsync(bookingId, ct);
                if (booking is null || booking.UserId != request.UserId)
                    return ("error", Serialize(new { error = "No booking with that ID was found for this user." }));

                var dto = await BookingDtoFactory.CreateAsync(booking, scheduleRepo, busRepo, routeRepo, ct);
                return ("booking", Serialize(dto));
            }

            case AssistantTools.SuggestCancelBooking:
            {
                if (!TryGetGuid(root, "bookingId", out var bookingId))
                    return ("error", Serialize(new { error = "A valid bookingId is required." }));

                var booking = await bookingRepo.GetByIdReadOnlyAsync(bookingId, ct);
                if (booking is null || booking.UserId != request.UserId)
                    return ("error", Serialize(new { error = "No booking with that ID was found for this user." }));

                var cancellable = booking.Status is BookingStatus.Pending or BookingStatus.PaymentPending;
                if (!cancellable)
                {
                    return ("error", Serialize(new
                    {
                        error = $"This booking is {booking.Status} and can't be self-cancelled from here. " +
                                "Contact support for a Confirmed booking.",
                    }));
                }

                return ("cancel-suggestion", Serialize(new { bookingId = booking.Id, status = booking.Status.ToString() }));
            }

            case AssistantTools.GetVendorBuses:
            {
                var vendor = await vendorRepo.GetByEntraObjectIdAsync(request.UserId.ToString(), ct);
                if (vendor is null)
                    return ("error", Serialize(new { error = "No vendor profile found for this account." }));

                var buses = await busRepo.GetByVendorIdAsync(vendor.Id, ct);
                return ("vendor-buses", Serialize(buses.Select(b => new
                {
                    b.Id, b.BusNumber, b.BusName, BusType = b.BusType.ToString(), b.TotalSeats,
                })));
            }

            case AssistantTools.GetVendorSchedules:
            {
                var vendor = await vendorRepo.GetByEntraObjectIdAsync(request.UserId.ToString(), ct);
                if (vendor is null)
                    return ("error", Serialize(new { error = "No vendor profile found for this account." }));

                var results = await new GetVendorSchedulesHandler(scheduleRepo, busRepo, routeRepo)
                    .HandleAsync(new GetVendorSchedulesQuery(vendor.Id), ct);
                return ("vendor-schedules", Serialize(results));
            }

            default:
                return ("error", Serialize(new { error = $"Unknown tool '{call.Name}'." }));
        }
    }

    private static bool TryGetGuid(JsonElement root, string propertyName, out Guid value)
    {
        value = Guid.Empty;
        return root.TryGetProperty(propertyName, out var el) &&
               Guid.TryParse(el.GetString(), out value);
    }

    private static string Serialize<T>(T value) => JsonSerializer.Serialize(value, JsonOptions);

    private static string BuildSystemInstruction(AssistantRole role)
    {
        var parts = new List<string> { AssistantKnowledgeBase.AssistantIdentityAndRules, AssistantKnowledgeBase.FaqAndPolicies };
        if (role == AssistantRole.Vendor)
            parts.Add(AssistantKnowledgeBase.VendorGuidance);
        return string.Join("\n\n", parts);
    }
}
