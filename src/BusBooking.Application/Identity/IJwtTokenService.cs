namespace BusBooking.Application.Identity;

public interface IJwtTokenService
{
    string IssueAccessToken(Guid appUserId, string email, string displayName, IEnumerable<string> roles);
    int AccessTokenExpiryMinutes { get; }
}
