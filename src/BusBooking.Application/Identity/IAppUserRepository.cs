using BusBooking.Domain.Identity.Entities;
using BusBooking.Domain.Identity.Enums;

namespace BusBooking.Application.Identity;

public interface IAppUserRepository
{
    Task<AppUser?> GetByIdAsync(Guid id, CancellationToken ct = default);
    Task<AppUser?> GetByProviderKeyAsync(LoginProvider provider, string providerKey, CancellationToken ct = default);
    Task<AppUser?> GetByEmailAsync(string email, CancellationToken ct = default);
    Task AddAsync(AppUser user, CancellationToken ct = default);
    Task AddExternalLoginAsync(ExternalLogin login, CancellationToken ct = default);
    Task SaveChangesAsync(CancellationToken ct = default);
}
