using BusBooking.Application.Users;
using BusBooking.Domain.Users.Entities;
using BusBooking.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;

namespace BusBooking.Infrastructure.Repositories;

internal sealed class UserProfileRepository(BusBookingDbContext db) : IUserProfileRepository
{
    public Task<UserProfile?> GetByIdAsync(Guid id, CancellationToken ct = default) =>
        db.UserProfiles.FirstOrDefaultAsync(u => u.Id == id, ct);

    public Task<UserProfile?> GetByEntraObjectIdAsync(string entraObjectId, CancellationToken ct = default) =>
        db.UserProfiles.FirstOrDefaultAsync(u => u.EntraObjectId == entraObjectId, ct);

    public Task<UserProfile?> GetByEmailAsync(string email, CancellationToken ct = default) =>
        db.UserProfiles.FirstOrDefaultAsync(u => u.Email == email, ct);

    public async Task<IReadOnlyList<UserProfile>> GetAllAsync(CancellationToken ct = default) =>
        await db.UserProfiles.ToListAsync(ct);

    public async Task AddAsync(UserProfile profile, CancellationToken ct = default) =>
        await db.UserProfiles.AddAsync(profile, ct);

    public Task SaveChangesAsync(CancellationToken ct = default) =>
        db.SaveChangesAsync(ct);
}
