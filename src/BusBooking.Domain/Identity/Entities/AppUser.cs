using BusBooking.Domain.Common;

namespace BusBooking.Domain.Identity.Entities;

public sealed class AppUser : BaseEntity
{
    public string Email { get; private set; } = default!;
    public string DisplayName { get; private set; } = default!;
    public bool EmailVerified { get; private set; }

    public ICollection<ExternalLogin> ExternalLogins { get; private set; } = new List<ExternalLogin>();
    public LocalCredential? LocalCredential { get; private set; }
    public ICollection<AppUserRole> Roles { get; private set; } = new List<AppUserRole>();
    public ICollection<RefreshToken> RefreshTokens { get; private set; } = new List<RefreshToken>();

    private AppUser() { }

    public static AppUser Create(Guid id, string email, string displayName, bool emailVerified = false)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(email);
        ArgumentException.ThrowIfNullOrWhiteSpace(displayName);
        return new AppUser
        {
            Id = id,
            Email = email,
            DisplayName = displayName,
            EmailVerified = emailVerified
        };
    }

    public void VerifyEmail()
    {
        EmailVerified = true;
        UpdatedAt = DateTime.UtcNow;
    }

    public void UpdateDisplayName(string displayName)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(displayName);
        DisplayName = displayName;
        UpdatedAt = DateTime.UtcNow;
    }
}
