namespace BusBooking.Application.Users.Queries.GetUserProfile;

public sealed record UserProfileDto(
    Guid UserId,
    string FirstName,
    string LastName,
    string Email,
    string? Phone,
    string? Address,
    bool IsActive);
