using BusBooking.Application.Identity;
using BusBooking.Domain.Identity.Entities;
using BusBooking.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;

namespace BusBooking.Infrastructure.Repositories;

internal sealed class RefreshTokenRepository(BusBookingDbContext db) : IRefreshTokenRepository
{
    public Task<RefreshToken?> GetByTokenHashAsync(string tokenHash, CancellationToken ct = default) =>
        db.RefreshTokens.FirstOrDefaultAsync(t => t.TokenHash == tokenHash, ct);

    public async Task AddAsync(RefreshToken token, CancellationToken ct = default) =>
        await db.RefreshTokens.AddAsync(token, ct);

    public async Task RevokeAllForUserAsync(Guid appUserId, CancellationToken ct = default)
    {
        var active = await db.RefreshTokens
            .Where(t => t.AppUserId == appUserId && t.RevokedAt == null)
            .ToListAsync(ct);

        foreach (var token in active)
            token.Revoke();
    }

    public Task SaveChangesAsync(CancellationToken ct = default) =>
        db.SaveChangesAsync(ct);
}
