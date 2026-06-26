using BusBooking.Infrastructure.Identity;
using Microsoft.Extensions.Configuration;
using System.IdentityModel.Tokens.Jwt;

namespace BusBooking.Api.IntegrationTests.Identity;

public sealed class JwtTokenServiceTests
{
    private const string SigningKey = "unit-test-signing-key-exactly-32-chars!!";
    private const string Issuer     = "BusBooking";
    private const string Audience   = "BusBookingClient";

    private static JwtTokenService BuildSut(int expiryMinutes = 60) =>
        new(new ConfigurationBuilder()
            .AddInMemoryCollection(new Dictionary<string, string?>
            {
                ["LocalJwt:SigningKey"]               = SigningKey,
                ["LocalJwt:Issuer"]                   = Issuer,
                ["LocalJwt:Audience"]                 = Audience,
                ["LocalJwt:AccessTokenExpiryMinutes"] = expiryMinutes.ToString(),
            })
            .Build());

    [Fact]
    public void IssueAccessToken_ReturnsNonEmptyString()
    {
        var token = BuildSut().IssueAccessToken(
            Guid.NewGuid(), "user@example.com", "Test User", []);
        Assert.NotEmpty(token);
    }

    [Fact]
    public void IssueAccessToken_ContainsAppUserId()
    {
        var userId = Guid.NewGuid();
        var token  = BuildSut().IssueAccessToken(userId, "user@example.com", "Test User", []);
        var parsed = new JwtSecurityTokenHandler().ReadJwtToken(token);
        var claim  = parsed.Claims.FirstOrDefault(c => c.Type == "app:userId");
        Assert.NotNull(claim);
        Assert.Equal(userId.ToString(), claim.Value);
    }

    [Fact]
    public void IssueAccessToken_ContainsEmailClaim()
    {
        var token  = BuildSut().IssueAccessToken(
            Guid.NewGuid(), "user@example.com", "Test User", []);
        var parsed = new JwtSecurityTokenHandler().ReadJwtToken(token);
        var email  = parsed.Claims.FirstOrDefault(c => c.Type == "email");
        Assert.NotNull(email);
        Assert.Equal("user@example.com", email.Value);
    }

    [Fact]
    public void IssueAccessToken_EmbedsSingleRole()
    {
        var token  = BuildSut().IssueAccessToken(
            Guid.NewGuid(), "admin@example.com", "Admin", ["BusBooking.Admin"]);
        var parsed = new JwtSecurityTokenHandler().ReadJwtToken(token);
        var role   = parsed.Claims.FirstOrDefault(c => c.Type == "roles");
        Assert.NotNull(role);
        Assert.Equal("BusBooking.Admin", role.Value);
    }

    [Fact]
    public void IssueAccessToken_EmbedsMultipleRoles()
    {
        var roles  = new[] { "BusBooking.Admin", "BusBooking.Vendor" };
        var token  = BuildSut().IssueAccessToken(
            Guid.NewGuid(), "multi@example.com", "Multi", roles);
        var parsed = new JwtSecurityTokenHandler().ReadJwtToken(token);
        var actual = parsed.Claims.Where(c => c.Type == "roles").Select(c => c.Value).ToArray();
        Assert.Equal(roles.Order(), actual.Order());
    }

    [Fact]
    public void IssueAccessToken_ExpiresAfterConfiguredMinutes()
    {
        var expiryMinutes = 15;
        var before = DateTime.UtcNow;
        var token  = BuildSut(expiryMinutes).IssueAccessToken(
            Guid.NewGuid(), "u@example.com", "U", []);
        var parsed = new JwtSecurityTokenHandler().ReadJwtToken(token);
        // Allow a couple of seconds of clock drift
        Assert.True(parsed.ValidTo >= before.AddMinutes(expiryMinutes - 1));
        Assert.True(parsed.ValidTo <= before.AddMinutes(expiryMinutes + 1));
    }

    [Fact]
    public void AccessTokenExpiryMinutes_ReflectsConfiguration()
    {
        Assert.Equal(30, BuildSut(30).AccessTokenExpiryMinutes);
    }
}
