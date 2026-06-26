using BusBooking.Application.Common.Exceptions;

namespace BusBooking.Application.Users.Queries.GetUserProfile;

public sealed class GetUserProfileHandler(IUserProfileRepository userRepo)
{
    public async Task<UserProfileDto> HandleAsync(GetUserProfileQuery query, CancellationToken ct = default)
    {
        var profile = await userRepo.GetByIdAsync(query.UserId, ct)
            ?? throw new NotFoundException("UserProfile", query.UserId);

        return new UserProfileDto(profile.Id, profile.FirstName, profile.LastName,
                                  profile.Email, profile.Phone, profile.Address, profile.IsActive);
    }
}
