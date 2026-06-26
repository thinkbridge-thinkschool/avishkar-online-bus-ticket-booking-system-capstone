using BusBooking.Infrastructure.Identity;

namespace BusBooking.Api.IntegrationTests.Identity;

public sealed class PasswordServiceTests
{
    private readonly PasswordService _sut = new();

    [Fact]
    public void Hash_ReturnsNonEmptyString()
    {
        var hash = _sut.Hash("TestPassword1!");
        Assert.NotEmpty(hash);
    }

    [Fact]
    public void Hash_DiffersFromPlaintext()
    {
        var hash = _sut.Hash("TestPassword1!");
        Assert.NotEqual("TestPassword1!", hash);
    }

    [Fact]
    public void Hash_ProducesUniqueSaltedHashes()
    {
        var hash1 = _sut.Hash("TestPassword1!");
        var hash2 = _sut.Hash("TestPassword1!");
        // BCrypt embeds a random salt so two hashes of the same password differ
        Assert.NotEqual(hash1, hash2);
    }

    [Fact]
    public void Verify_ReturnsTrueForCorrectPassword()
    {
        var hash = _sut.Hash("CorrectHorseBatteryStaple");
        Assert.True(_sut.Verify("CorrectHorseBatteryStaple", hash));
    }

    [Fact]
    public void Verify_ReturnsFalseForWrongPassword()
    {
        var hash = _sut.Hash("RightPassword");
        Assert.False(_sut.Verify("WrongPassword", hash));
    }

    [Fact]
    public void Verify_ReturnsFalseForEmptyPassword()
    {
        var hash = _sut.Hash("SomePassword");
        Assert.False(_sut.Verify("", hash));
    }
}
