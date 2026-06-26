using BusBooking.Application.Identity;
using Microsoft.Extensions.Configuration;
using Microsoft.IdentityModel.Tokens;
using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Text;

namespace BusBooking.Infrastructure.Identity;

internal sealed class JwtTokenService : IJwtTokenService
{
    private readonly SymmetricSecurityKey _signingKey;
    private readonly string _issuer;
    private readonly string _audience;

    public int AccessTokenExpiryMinutes { get; }

    public JwtTokenService(IConfiguration config)
    {
        var section    = config.GetSection("LocalJwt");
        var signingKey = section["SigningKey"]
            ?? throw new InvalidOperationException("LocalJwt:SigningKey is not configured.");

        _signingKey          = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(signingKey));
        _issuer              = section["Issuer"]   ?? "https://api.busbooking.com";
        _audience            = section["Audience"] ?? "https://api.busbooking.com";
        AccessTokenExpiryMinutes = int.TryParse(section["AccessTokenExpiryMinutes"], out var m) ? m : 30;
    }

    public string IssueAccessToken(Guid appUserId, string email, string displayName, IEnumerable<string> roles)
    {
        var claims = new List<Claim>
        {
            // Embed app:userId so AppClaimsTransformer finds it and short-circuits
            new("app:userId",                      appUserId.ToString()),
            new(JwtRegisteredClaimNames.Sub,       appUserId.ToString()),
            new(JwtRegisteredClaimNames.Email,     email),
            new("name",                            displayName),
            new(JwtRegisteredClaimNames.Jti,       Guid.NewGuid().ToString()),
        };

        foreach (var role in roles)
            claims.Add(new Claim("roles", role));

        var descriptor = new SecurityTokenDescriptor
        {
            Subject            = new ClaimsIdentity(claims),
            Issuer             = _issuer,
            Audience           = _audience,
            Expires            = DateTime.UtcNow.AddMinutes(AccessTokenExpiryMinutes),
            SigningCredentials = new SigningCredentials(_signingKey, SecurityAlgorithms.HmacSha256),
        };

        var handler = new JwtSecurityTokenHandler();
        return handler.WriteToken(handler.CreateToken(descriptor));
    }
}
