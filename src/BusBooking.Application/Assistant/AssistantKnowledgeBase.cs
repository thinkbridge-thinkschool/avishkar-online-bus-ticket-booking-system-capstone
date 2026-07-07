namespace BusBooking.Application.Assistant;

// Mirrors the FAQ content in the Angular home page (features/home/home.html) so the assistant
// paraphrases real, reviewed policy text instead of inventing figures. Keep these two in sync
// when either changes. (One correction vs. the current home.html copy: that page still says
// "sign in with your Microsoft account" — stale since local email/password login shipped;
// the assistant states both options correctly.)
public static class AssistantKnowledgeBase
{
    public const string FaqAndPolicies = """
        Frequently asked questions and policies for BusBooking:

        Q: How do I book a bus ticket?
        A: Search for buses by selecting your origin, destination, and travel date. Choose your
        preferred bus, pick your seats, fill in passenger details, and complete the payment. Your
        e-ticket is issued instantly and appears under My Bookings.

        Q: What payment methods are accepted?
        A: All major payment methods via Razorpay — UPI (PhonePe, Google Pay, Paytm), credit and
        debit cards (Visa, Mastercard, RuPay), net banking, and popular digital wallets.

        Q: Can I cancel or modify my booking?
        A: Cancellation policies depend on the bus operator. You can cancel a booking yourself from
        My Bookings while it is still Pending or Payment Pending. For a Confirmed booking, or to
        modify passenger/seat details, contact support at least 2 hours before departure.

        Q: How do I get my ticket after booking?
        A: After successful payment, your e-ticket appears in My Bookings, showing seat number,
        journey details, and booking reference. Present it (digitally or printed) when boarding.

        Q: Do I need an account to book tickets?
        A: Yes. You can sign in either with a Microsoft account or with a local email/password
        account created via Sign Up — either way your bookings are securely linked to your profile
        and accessible from any device.
        """;

    public const string AssistantIdentityAndRules = """
        You are the BusBooking Assistant, a helpful guide for an online bus ticket booking platform.

        Hard rules — follow these exactly:
        - Only state booking, schedule, price, or seat-availability facts that came from a tool
          call in this conversation. Never invent a schedule, price, or booking detail.
        - For policy questions (cancellation, refund, payment), paraphrase the FAQ content you were
          given — never invent numbers, windows, or fees not present in that content.
        - You cannot cancel a booking yourself. If the user wants to cancel one, call
          suggest_cancel_booking with the specific booking's ID — the application will show them a
          real confirmation button. Never claim a booking has been cancelled.
        - You cannot process payments, modify prices, or take any action beyond the tools provided.
        - If a tool call fails or returns no results, say so plainly and suggest a next step (e.g.
          "try a different date" or "check My Bookings directly") rather than guessing.
        - Keep answers concise and conversational. Use plain text, not markdown tables.
        """;

    public const string VendorGuidance = """
        Guidance for vendor users managing their fleet on BusBooking:
        - Add a bus from Vendor Portal → My Buses → Add New Bus (bus number, type, total seats).
        - Create a schedule from Vendor Portal → My Schedules → Add Schedule, choosing one of your
          buses, a route, travel date, times, and price per seat.
        - Deleting a bus or schedule removes it from your active lists; it does not affect bookings
          already made against it.
        - A vendor account needs to be approved by an admin, and needs an active tenant, before
          buses or schedules can be created — if a vendor reports being blocked, that is almost
          always the cause.
        """;
}
