using BusBooking.Domain.Identity.Entities;

namespace BusBooking.Application.Identity;

public interface IRefreshTokenRepository
{
    Task<RefreshToken?> GetByTokenHashAsync(string tokenHash, CancellationToken ct = default);
    Task AddAsync(RefreshToken token, CancellationToken ct = default);
    Task RevokeAllForUserAsync(Guid appUserId, CancellationToken ct = default);
    Task SaveChangesAsync(CancellationToken ct = default);
}
