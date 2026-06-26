using BusBooking.Domain.Common;

namespace BusBooking.Domain.Identity.Entities;

public sealed class LocalCredential : BaseEntity
{
    public Guid AppUserId { get; private set; }
    public string PasswordHash { get; private set; } = default!;
    public DateTime LastChangedAt { get; private set; }
    public int FailedLoginAttempts { get; private set; }
    public DateTime? LockedUntil { get; private set; }

    // One-time tokens are stored as hashes; raw values are sent only via email
    public string? EmailVerificationTokenHash { get; private set; }
    public DateTime? EmailVerificationTokenExpiry { get; private set; }
    public string? PasswordResetTokenHash { get; private set; }
    public DateTime? PasswordResetTokenExpiry { get; private set; }

    public AppUser AppUser { get; private set; } = default!;

    private LocalCredential() { }

    public static LocalCredential Create(Guid appUserId, string passwordHash)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(passwordHash);
        return new LocalCredential
        {
            AppUserId     = appUserId,
            PasswordHash  = passwordHash,
            LastChangedAt = DateTime.UtcNow
        };
    }

    public void UpdatePasswordHash(string newHash)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(newHash);
        PasswordHash  = newHash;
        LastChangedAt = DateTime.UtcNow;
        UpdatedAt     = DateTime.UtcNow;
        FailedLoginAttempts = 0;
        LockedUntil   = null;
    }

    public void RecordFailedLogin(int maxAttempts, TimeSpan lockDuration)
    {
        FailedLoginAttempts++;
        if (FailedLoginAttempts >= maxAttempts)
            LockedUntil = DateTime.UtcNow.Add(lockDuration);
        UpdatedAt = DateTime.UtcNow;
    }

    public void RecordSuccessfulLogin()
    {
        FailedLoginAttempts = 0;
        LockedUntil         = null;
        UpdatedAt           = DateTime.UtcNow;
    }

    public bool IsLocked() => LockedUntil.HasValue && LockedUntil.Value > DateTime.UtcNow;

    public void SetEmailVerificationToken(string tokenHash, DateTime expiry)
    {
        EmailVerificationTokenHash   = tokenHash;
        EmailVerificationTokenExpiry = expiry;
        UpdatedAt = DateTime.UtcNow;
    }

    public void ClearEmailVerificationToken()
    {
        EmailVerificationTokenHash   = null;
        EmailVerificationTokenExpiry = null;
        UpdatedAt = DateTime.UtcNow;
    }

    public void SetPasswordResetToken(string tokenHash, DateTime expiry)
    {
        PasswordResetTokenHash   = tokenHash;
        PasswordResetTokenExpiry = expiry;
        UpdatedAt = DateTime.UtcNow;
    }

    public void ClearPasswordResetToken()
    {
        PasswordResetTokenHash   = null;
        PasswordResetTokenExpiry = null;
        UpdatedAt = DateTime.UtcNow;
    }
}
