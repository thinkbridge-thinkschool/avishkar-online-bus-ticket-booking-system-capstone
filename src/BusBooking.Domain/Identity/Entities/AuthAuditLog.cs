namespace BusBooking.Domain.Identity.Entities;

public sealed class AuthAuditLog
{
    public long Id { get; private set; }
    public Guid? AppUserId { get; private set; }
    public string Email { get; private set; } = default!;
    public string EventType { get; private set; } = default!;
    public bool Success { get; private set; }
    public string? IpAddress { get; private set; }
    public string? UserAgent { get; private set; }
    public DateTime CreatedAt { get; private set; }

    private AuthAuditLog() { }

    public static AuthAuditLog Create(
        string eventType,
        bool success,
        string email,
        Guid? appUserId = null,
        string? ipAddress = null,
        string? userAgent = null)
    {
        return new AuthAuditLog
        {
            EventType  = eventType,
            Success    = success,
            Email      = email,
            AppUserId  = appUserId,
            IpAddress  = ipAddress,
            UserAgent  = userAgent,
            CreatedAt  = DateTime.UtcNow,
        };
    }

    // ── Event type constants ─────────────────────────────────────────────────
    public static class Events
    {
        public const string Register      = "Register";
        public const string LoginSuccess  = "Login.Success";
        public const string LoginFailure  = "Login.Failure";
        public const string LoginLocked   = "Login.Locked";
        public const string EmailVerified = "EmailVerified";
        public const string ForgotPassword = "ForgotPassword";
        public const string PasswordReset = "PasswordReset";
        public const string TokenReuse    = "TokenReuse";   // security alert
        public const string Logout        = "Logout";
        public const string ProviderLinked   = "Provider.Linked";
        public const string ProviderUnlinked = "Provider.Unlinked";
    }
}
