using BusBooking.Application.Identity;
using BusBooking.Domain.Identity.Entities;
using BusBooking.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;

namespace BusBooking.Infrastructure.Repositories;

internal sealed class LocalCredentialRepository(BusBookingDbContext db) : ILocalCredentialRepository
{
    public Task<LocalCredential?> GetByAppUserIdAsync(Guid appUserId, CancellationToken ct = default) =>
        db.LocalCredentials.FirstOrDefaultAsync(l => l.AppUserId == appUserId, ct);

    public Task<LocalCredential?> GetByEmailVerificationTokenAsync(
        string tokenHash, CancellationToken ct = default) =>
        db.LocalCredentials
            .Include(l => l.AppUser)
            .FirstOrDefaultAsync(l =>
                l.EmailVerificationTokenHash == tokenHash &&
                l.EmailVerificationTokenExpiry > DateTime.UtcNow, ct);

    public Task<LocalCredential?> GetByPasswordResetTokenAsync(
        string tokenHash, CancellationToken ct = default) =>
        db.LocalCredentials
            .Include(l => l.AppUser)
            .FirstOrDefaultAsync(l =>
                l.PasswordResetTokenHash == tokenHash &&
                l.PasswordResetTokenExpiry > DateTime.UtcNow, ct);

    public async Task AddAsync(LocalCredential credential, CancellationToken ct = default) =>
        await db.LocalCredentials.AddAsync(credential, ct);

    public Task SaveChangesAsync(CancellationToken ct = default) =>
        db.SaveChangesAsync(ct);
}
