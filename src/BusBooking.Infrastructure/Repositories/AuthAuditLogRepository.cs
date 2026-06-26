using BusBooking.Application.Identity;
using BusBooking.Domain.Identity.Entities;
using BusBooking.Infrastructure.Persistence;

namespace BusBooking.Infrastructure.Repositories;

internal sealed class AuthAuditLogRepository(BusBookingDbContext db) : IAuthAuditLogRepository
{
    public async Task AddAsync(AuthAuditLog entry, CancellationToken ct = default) =>
        await db.AuthAuditLogs.AddAsync(entry, ct);

    public Task SaveChangesAsync(CancellationToken ct = default) =>
        db.SaveChangesAsync(ct);
}
