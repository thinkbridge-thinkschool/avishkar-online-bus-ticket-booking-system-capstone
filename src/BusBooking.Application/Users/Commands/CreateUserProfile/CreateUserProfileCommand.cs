namespace BusBooking.Application.Users.Commands.CreateUserProfile;

public sealed record CreateUserProfileCommand(
    string EntraObjectId,
    string FirstName,
    string LastName,
    string Email);
