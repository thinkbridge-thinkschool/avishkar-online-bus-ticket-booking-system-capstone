using BusBooking.Domain.Users.Entities;

namespace BusBooking.Application.Users.Commands.CreateUserProfile;

public sealed class CreateUserProfileHandler(IUserProfileRepository userRepo)
{
    public async Task<Guid> HandleAsync(CreateUserProfileCommand command, CancellationToken ct = default)
    {
        var existing = await userRepo.GetByEntraObjectIdAsync(command.EntraObjectId, ct);
        if (existing is not null)
            throw new InvalidOperationException("A profile already exists for this account.");

        var profile = UserProfile.Create(command.EntraObjectId, command.FirstName, command.LastName, command.Email);
        await userRepo.AddAsync(profile, ct);
        await userRepo.SaveChangesAsync(ct);
        return profile.Id;
    }
}
