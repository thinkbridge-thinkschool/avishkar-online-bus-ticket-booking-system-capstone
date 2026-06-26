using BusBooking.Domain.Identity.Entities;

namespace BusBooking.Application.Identity;

public interface IAuthAuditLogRepository
{
    Task AddAsync(AuthAuditLog entry, CancellationToken ct = default);
    Task SaveChangesAsync(CancellationToken ct = default);
}
