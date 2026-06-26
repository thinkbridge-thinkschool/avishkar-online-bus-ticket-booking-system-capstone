using BusBooking.Application.Common.Exceptions;

namespace BusBooking.Application.Users.Commands.DeactivateUser;

public sealed class DeactivateUserHandler(IUserProfileRepository userRepo)
{
    public async Task HandleAsync(DeactivateUserCommand command, CancellationToken ct = default)
    {
        var profile = await userRepo.GetByIdAsync(command.UserId, ct)
            ?? throw new NotFoundException("UserProfile", command.UserId);

        if (profile.EntraObjectId != command.RequestingEntraObjectId)
            throw new UnauthorizedAccessException("You do not own this profile.");

        profile.Deactivate();
        await userRepo.SaveChangesAsync(ct);
    }
}
