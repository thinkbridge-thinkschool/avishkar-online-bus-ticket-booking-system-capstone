using BusBooking.Application.Common.Interfaces;
using Microsoft.Extensions.Logging;

namespace BusBooking.Infrastructure.Email;

// Development-only email sink. Writes email content to the application log so the
// developer can copy the verification/reset link from the console without a real SMTP server.
internal sealed class LogEmailService(ILogger<LogEmailService> logger) : IEmailService
{
    public Task SendEmailVerificationAsync(
        string toEmail, string displayName, string verificationUrl, CancellationToken ct = default)
    {
        logger.LogInformation(
            "[DEV EMAIL] Email Verification for {DisplayName} <{Email}>\nClick to verify: {Url}",
            displayName, toEmail, verificationUrl);
        return Task.CompletedTask;
    }

    public Task SendPasswordResetAsync(
        string toEmail, string displayName, string resetUrl, CancellationToken ct = default)
    {
        logger.LogInformation(
            "[DEV EMAIL] Password Reset for {DisplayName} <{Email}>\nClick to reset: {Url}",
            displayName, toEmail, resetUrl);
        return Task.CompletedTask;
    }

    public Task SendBookingConfirmationAsync(
        string toEmail, string userName, Guid bookingId, IReadOnlyList<int> seatNumbers, decimal totalAmount, CancellationToken ct = default)
    {
        logger.LogInformation(
            "[DEV EMAIL] Booking Confirmation for {UserName} <{Email}>\nBooking {BookingId}, seats {Seats}, total {Total}",
            userName, toEmail, bookingId, string.Join(",", seatNumbers), totalAmount);
        return Task.CompletedTask;
    }

    public Task SendBookingCancellationAsync(
        string toEmail, Guid bookingId, IReadOnlyList<int> seatNumbers, CancellationToken ct = default)
    {
        logger.LogInformation(
            "[DEV EMAIL] Booking Cancellation for <{Email}>\nBooking {BookingId}, released seats {Seats}",
            toEmail, bookingId, string.Join(",", seatNumbers));
        return Task.CompletedTask;
    }
}
