namespace BusBooking.Application.Users;
using BusBooking.Domain.Users.Entities;

public interface IUserProfileRepository
{
    Task<UserProfile?> GetByIdAsync(Guid id, CancellationToken ct = default);
    Task<UserProfile?> GetByEntraObjectIdAsync(string entraObjectId, CancellationToken ct = default);
    Task<UserProfile?> GetByEmailAsync(string email, CancellationToken ct = default);
    Task<IReadOnlyList<UserProfile>> GetAllAsync(CancellationToken ct = default);
    Task AddAsync(UserProfile profile, CancellationToken ct = default);
    Task SaveChangesAsync(CancellationToken ct = default);
}
