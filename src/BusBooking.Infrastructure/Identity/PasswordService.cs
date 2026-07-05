using BusBooking.Application.Identity;

namespace BusBooking.Infrastructure.Identity;

// BCrypt work factor 12 — OWASP recommended minimum as of 2024.
// Balances brute-force resistance against server CPU cost (~250 ms per hash at factor 12).
internal sealed class PasswordService : IPasswordService
{
    private const int WorkFactor = 12; // 2^12 iterations 

    public string Hash(string plaintext) =>
        BCrypt.Net.BCrypt.HashPassword(plaintext, WorkFactor); // function converts it into something like `$2a$12$eImiTXuWVxfM37uY4JANjQ==` which is a hash of the password. The hash is stored in the database instead of the plaintext password for security reasons. Even if the database is compromised, the attacker cannot easily recover the original password from the hash.

    public bool Verify(string plaintext, string hash) =>
        BCrypt.Net.BCrypt.Verify(plaintext, hash);
}
