namespace BusBooking.Application.Identity;

public interface IPasswordService
{
    string Hash(string plaintext);
    bool Verify(string plaintext, string hash);
}
