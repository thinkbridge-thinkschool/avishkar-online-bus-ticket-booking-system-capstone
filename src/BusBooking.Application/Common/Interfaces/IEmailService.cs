namespace BusBooking.Application.Common.Interfaces;

public interface IEmailService
{
    Task SendEmailVerificationAsync(string toEmail, string displayName, string verificationUrl, CancellationToken ct = default);
    Task SendPasswordResetAsync(string toEmail, string displayName, string resetUrl, CancellationToken ct = default);
    Task SendBookingConfirmationAsync(string toEmail, string userName, Guid bookingId, IReadOnlyList<int> seatNumbers, decimal totalAmount, CancellationToken ct = default);
    Task SendBookingCancellationAsync(string toEmail, Guid bookingId, IReadOnlyList<int> seatNumbers, CancellationToken ct = default);
}
