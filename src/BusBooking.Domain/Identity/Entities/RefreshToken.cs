using BusBooking.Domain.Common;

namespace BusBooking.Domain.Identity.Entities;

public sealed class RefreshToken : BaseEntity
{
    public Guid AppUserId { get; private set; }

    // SHA-256 hash of the raw token sent to the client — never store the raw value
    public string TokenHash { get; private set; } = default!;
    public DateTime IssuedAt { get; private set; }
    public DateTime ExpiresAt { get; private set; }
    public DateTime? RevokedAt { get; private set; }

    // Forms a rotation chain so family invalidation is possible on reuse detection
    public Guid? ReplacedByTokenId { get; private set; }

    public AppUser AppUser { get; private set; } = default!;

    private RefreshToken() { }

    public static RefreshToken Create(Guid appUserId, string tokenHash, DateTime expiresAt)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(tokenHash);
        return new RefreshToken
        {
            AppUserId = appUserId,
            TokenHash = tokenHash,
            IssuedAt  = DateTime.UtcNow,
            ExpiresAt = expiresAt
        };
    }

    public bool IsActive => RevokedAt is null && DateTime.UtcNow < ExpiresAt;

    public void Revoke(Guid? replacedByTokenId = null)
    {
        RevokedAt          = DateTime.UtcNow;
        ReplacedByTokenId  = replacedByTokenId;
        UpdatedAt          = DateTime.UtcNow;
    }
}
