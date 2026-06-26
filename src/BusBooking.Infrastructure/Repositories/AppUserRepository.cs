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

    public Task<IReadOnlyList<AppUser>> GetAllAsync(int skip, int take, CancellationToken ct = default) =>
        db.AppUsers
            .Include(u => u.ExternalLogins)
            .Include(u => u.Roles)
            .OrderBy(u => u.Email)
            .Skip(skip)
            .Take(take)
            .ToListAsync(ct)
            .ContinueWith(t => (IReadOnlyList<AppUser>)t.Result, ct);

    public async Task AddAsync(AppUser user, CancellationToken ct = default) =>
        await db.AppUsers.AddAsync(user, ct);

    public async Task AddExternalLoginAsync(ExternalLogin login, CancellationToken ct = default) =>
        await db.ExternalLogins.AddAsync(login, ct);

    public async Task RemoveExternalLoginAsync(
        Guid appUserId, LoginProvider provider, CancellationToken ct = default)
    {
        var login = await db.ExternalLogins
            .FirstOrDefaultAsync(l => l.AppUserId == appUserId && l.LoginProvider == provider, ct);
        if (login is not null)
            db.ExternalLogins.Remove(login);
    }

    public async Task AddRoleAsync(AppUserRole role, CancellationToken ct = default) =>
        await db.AppUserRoles.AddAsync(role, ct);

    public async Task RemoveRoleAsync(Guid appUserId, string roleName, CancellationToken ct = default)
    {
        var role = await db.AppUserRoles
            .FirstOrDefaultAsync(r => r.AppUserId == appUserId && r.RoleName == roleName, ct);
        if (role is not null)
            db.AppUserRoles.Remove(role);
    }

    public Task SaveChangesAsync(CancellationToken ct = default) =>
        db.SaveChangesAsync(ct);
}
