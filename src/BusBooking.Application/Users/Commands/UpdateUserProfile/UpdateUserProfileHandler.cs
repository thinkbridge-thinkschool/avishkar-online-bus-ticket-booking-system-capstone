using BusBooking.Application.Common.Exceptions;

namespace BusBooking.Application.Users.Commands.UpdateUserProfile;

public sealed class UpdateUserProfileHandler(IUserProfileRepository userRepo)
{
    public async Task HandleAsync(UpdateUserProfileCommand command, CancellationToken ct = default)
    {
        var profile = await userRepo.GetByIdAsync(command.UserId, ct)
            ?? throw new NotFoundException("UserProfile", command.UserId);

        if (profile.EntraObjectId != command.RequestingEntraObjectId)
            throw new UnauthorizedAccessException("You do not own this profile.");

        profile.Update(command.FirstName, command.LastName, command.Phone, command.Address);
        await userRepo.SaveChangesAsync(ct);
    }
}
