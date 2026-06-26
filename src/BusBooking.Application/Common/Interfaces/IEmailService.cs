namespace BusBooking.Application.Common.Interfaces;

public interface IEmailService
{
    Task SendEmailVerificationAsync(string toEmail, string displayName, string verificationUrl, CancellationToken ct = default);
    Task SendPasswordResetAsync(string toEmail, string displayName, string resetUrl, CancellationToken ct = default);
}
