using BusBooking.Application.Identity;
using BusBooking.Domain.Identity.Entities;
using BusBooking.Domain.Identity.Enums;
using BusBooking.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;

namespace BusBooking.Infrastructure.Repositories;

internal sealed class AppUserRepository(BusBookingDbContext db) : IAppUserRepository
{
    public Task<AppUser?> GetByIdAsync(Guid id, CancellationToken ct = default) =>
        db.AppUsers
            .Include(u => u.ExternalLogins)
            .Include(u => u.Roles)
            .FirstOrDefaultAsync(u => u.Id == id, ct);

    public Task<AppUser?> GetByProviderKeyAsync(
        LoginProvider provider, string providerKey, CancellationToken ct = default) =>
        db.AppUsers
            .Include(u => u.ExternalLogins)
            .Include(u => u.Roles)
            .FirstOrDefaultAsync(
                u => u.ExternalLogins.Any(l =>
                    l.LoginProvider == provider && l.ProviderKey == providerKey), ct);

    public Task<AppUser?> GetByEmailAsync(string email, CancellationToken ct = default) =>
        db.AppUsers
            .Include(u => u.ExternalLogins)
            .Include(u => u.Roles)
            .FirstOrDefaultAsync(u => u.Email == email, ct);

    public async Task AddAsync(AppUser user, CancellationToken ct = default) =>
        await db.AppUsers.AddAsync(user, ct);

    public async Task AddExternalLoginAsync(ExternalLogin login, CancellationToken ct = default) =>
        await db.ExternalLogins.AddAsync(login, ct);

    public Task SaveChangesAsync(CancellationToken ct = default) =>
        db.SaveChangesAsync(ct);
}
