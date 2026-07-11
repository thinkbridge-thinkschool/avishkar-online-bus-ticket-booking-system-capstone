using BusBooking.Application.Identity;
using BusBooking.Domain.Identity.Entities;
using BusBooking.Domain.Identity.Enums;

namespace BusBooking.Application.Tests.Fakes;

public sealed class FakeAppUserRepository : IAppUserRepository
{
    private readonly List<AppUser> _store = [];

    public Task<AppUser?> GetByIdAsync(Guid id, CancellationToken ct = default) =>
        Task.FromResult(_store.FirstOrDefault(u => u.Id == id));

    public Task<AppUser?> GetByProviderKeyAsync(LoginProvider provider, string providerKey, CancellationToken ct = default) =>
        Task.FromResult<AppUser?>(null);

    public Task<AppUser?> GetByEmailAsync(string email, CancellationToken ct = default) =>
        Task.FromResult(_store.FirstOrDefault(u => u.Email == email));

    public Task<IReadOnlyList<AppUser>> GetAllAsync(int skip, int take, CancellationToken ct = default) =>
        Task.FromResult<IReadOnlyList<AppUser>>(_store.Skip(skip).Take(take).ToList());

    public Task AddAsync(AppUser user, CancellationToken ct = default)
    {
        _store.Add(user);
        return Task.CompletedTask;
    }

    public Task AddExternalLoginAsync(ExternalLogin login, CancellationToken ct = default) => Task.CompletedTask;

    public Task RemoveExternalLoginAsync(Guid appUserId, LoginProvider provider, CancellationToken ct = default) => Task.CompletedTask;

    public Task AddRoleAsync(AppUserRole role, CancellationToken ct = default) => Task.CompletedTask;

    public Task RemoveRoleAsync(Guid appUserId, string roleName, CancellationToken ct = default) => Task.CompletedTask;

    public Task SaveChangesAsync(CancellationToken ct = default) => Task.CompletedTask;
}
