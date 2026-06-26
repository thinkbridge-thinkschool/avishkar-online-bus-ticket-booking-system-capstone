using BusBooking.Domain.Identity.Entities;

namespace BusBooking.Application.Identity;

public interface ILocalCredentialRepository
{
    Task<LocalCredential?> GetByAppUserIdAsync(Guid appUserId, CancellationToken ct = default);
    Task<LocalCredential?> GetByEmailVerificationTokenAsync(string tokenHash, CancellationToken ct = default);
    Task<LocalCredential?> GetByPasswordResetTokenAsync(string tokenHash, CancellationToken ct = default);
    Task AddAsync(LocalCredential credential, CancellationToken ct = default);
    Task SaveChangesAsync(CancellationToken ct = default);
}
