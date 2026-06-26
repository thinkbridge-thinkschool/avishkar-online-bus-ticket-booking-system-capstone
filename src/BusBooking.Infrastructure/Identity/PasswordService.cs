using BusBooking.Application.Identity;

namespace BusBooking.Infrastructure.Identity;

// BCrypt work factor 12 — OWASP recommended minimum as of 2024.
// Balances brute-force resistance against server CPU cost (~250 ms per hash at factor 12).
internal sealed class PasswordService : IPasswordService
{
    private const int WorkFactor = 12;

    public string Hash(string plaintext) =>
        BCrypt.Net.BCrypt.HashPassword(plaintext, WorkFactor);

    public bool Verify(string plaintext, string hash) =>
        BCrypt.Net.BCrypt.Verify(plaintext, hash);
}
