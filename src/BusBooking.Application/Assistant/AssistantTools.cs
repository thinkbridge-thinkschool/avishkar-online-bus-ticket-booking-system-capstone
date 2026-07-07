namespace BusBooking.Application.Assistant;

public enum AssistantRole { Customer, Vendor, Admin }

public static class AssistantTools
{
    public const string SearchSchedules = "search_schedules";
    public const string GetMyBookings = "get_my_bookings";
    public const string GetBookingById = "get_booking_by_id";
    public const string SuggestCancelBooking = "suggest_cancel_booking";
    public const string GetVendorBuses = "get_vendor_buses";
    public const string GetVendorSchedules = "get_vendor_schedules";

    // Every customer-facing tool is read-only except suggest_cancel_booking, which itself performs
    // no mutation — it only validates and hands the UI a booking ID to confirm against the real
    // cancel endpoint. Vendor tools are only included in the list sent to the model for Vendor role.
    public static IReadOnlyList<AiToolDefinition> ForRole(AssistantRole role)
    {
        var tools = new List<AiToolDefinition>
        {
            new(SearchSchedules,
                "Search available bus schedules between two cities on a date.",
                """
                {"type":"OBJECT","properties":{
                  "fromCity":{"type":"STRING","description":"Origin city name, e.g. Mumbai"},
                  "toCity":{"type":"STRING","description":"Destination city name, e.g. Pune"},
                  "travelDate":{"type":"STRING","description":"Travel date as YYYY-MM-DD"}
                },"required":["fromCity","toCity","travelDate"]}
                """),
            new(GetMyBookings,
                "Get the current user's own bookings (no parameters).",
                """{"type":"OBJECT","properties":{}}"""),
            new(GetBookingById,
                "Get one of the current user's own bookings by its booking ID.",
                """
                {"type":"OBJECT","properties":{
                  "bookingId":{"type":"STRING","description":"The booking's GUID"}
                },"required":["bookingId"]}
                """),
            new(SuggestCancelBooking,
                "Validate that a specific booking (owned by the current user) can be cancelled, " +
                "and hand off to the application to show a real cancel-confirmation control. " +
                "This does NOT cancel the booking.",
                """
                {"type":"OBJECT","properties":{
                  "bookingId":{"type":"STRING","description":"The booking's GUID"}
                },"required":["bookingId"]}
                """),
        };

        if (role == AssistantRole.Vendor)
        {
            tools.Add(new(GetVendorBuses,
                "Get the current vendor's own active buses (no parameters).",
                """{"type":"OBJECT","properties":{}}"""));
            tools.Add(new(GetVendorSchedules,
                "Get the current vendor's own active schedules (no parameters).",
                """{"type":"OBJECT","properties":{}}"""));
        }

        return tools;
    }
}
