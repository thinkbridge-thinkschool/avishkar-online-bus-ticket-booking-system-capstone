namespace BusBooking.Application.Users.Commands.UpdateUserProfile;

public sealed record UpdateUserProfileCommand(
    Guid UserId,
    string RequestingEntraObjectId,
    string FirstName,
    string LastName,
    string? Phone,
    string? Address);
