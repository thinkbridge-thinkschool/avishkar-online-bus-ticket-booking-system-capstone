using System.Net;
using System.Net.Mail;
using BusBooking.Application.Common.Interfaces;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;

namespace BusBooking.Infrastructure.Email;

internal sealed class SmtpEmailService(
    IConfiguration config,
    ILogger<SmtpEmailService> logger) : IEmailService
{
    private readonly string _host      = config["Smtp:Host"]!;
    private readonly int    _port      = config.GetValue("Smtp:Port", 587);
    private readonly string _username  = config["Smtp:Username"]!;
    private readonly string _password  = config["Smtp:Password"]!;
    private readonly string _fromAddr  = config["Smtp:FromAddress"] ?? config["Smtp:Username"]!;
    private readonly string _fromName  = config["Smtp:FromName"] ?? "BusBooking";

    public Task SendEmailVerificationAsync(
        string toEmail, string displayName, string verificationUrl, CancellationToken ct = default)
    {
        const string subject = "Verify your BusBooking email address";
        var body = $"""
            <!DOCTYPE html>
            <html lang="en">
            <body style="margin:0;padding:0;background:#f0f2f5;font-family:Arial,sans-serif;">
              <table width="100%" cellpadding="0" cellspacing="0" style="padding:40px 16px;">
                <tr><td align="center">
                  <table width="520" cellpadding="0" cellspacing="0"
                         style="background:#ffffff;border-radius:12px;overflow:hidden;
                                box-shadow:0 4px 16px rgba(0,0,0,0.08);">
                    <!-- Header -->
                    <tr>
                      <td style="background:linear-gradient(135deg,#667eea 0%,#764ba2 100%);
                                 padding:28px 32px;text-align:center;">
                        <p style="margin:0;font-size:2rem;line-height:1;">🚌</p>
                        <p style="margin:6px 0 0;color:#fff;font-size:1.3rem;font-weight:800;
                                  letter-spacing:-0.02em;">BusBooking</p>
                      </td>
                    </tr>
                    <!-- Body -->
                    <tr>
                      <td style="padding:36px 32px;">
                        <h1 style="margin:0 0 8px;color:#1a1a2e;font-size:1.3rem;font-weight:700;">
                          Welcome, {displayName}!
                        </h1>
                        <p style="margin:0 0 24px;color:#555;font-size:0.95rem;line-height:1.6;">
                          Thanks for signing up. Click the button below to verify your email
                          address and activate your account.
                        </p>
                        <div style="text-align:center;margin:0 0 28px;">
                          <a href="{verificationUrl}"
                             style="display:inline-block;background:linear-gradient(135deg,#667eea,#764ba2);
                                    color:#ffffff;text-decoration:none;padding:14px 36px;
                                    border-radius:8px;font-weight:700;font-size:1rem;">
                            Verify Email Address
                          </a>
                        </div>
                        <p style="margin:0;color:#888;font-size:0.82rem;line-height:1.6;">
                          This link expires in <strong>24 hours</strong>. If you didn't create
                          an account with BusBooking, you can safely ignore this email.
                        </p>
                      </td>
                    </tr>
                    <!-- Footer -->
                    <tr>
                      <td style="background:#f8f8fb;padding:16px 32px;text-align:center;
                                 border-top:1px solid #eee;">
                        <p style="margin:0;color:#bbb;font-size:0.75rem;">
                          © 2026 BusBooking · All rights reserved
                        </p>
                      </td>
                    </tr>
                  </table>
                </td></tr>
              </table>
            </body>
            </html>
            """;

        return SendAsync(toEmail, subject, body, ct);
    }

    public Task SendPasswordResetAsync(
        string toEmail, string displayName, string resetUrl, CancellationToken ct = default)
    {
        const string subject = "Reset your BusBooking password";
        var body = $"""
            <!DOCTYPE html>
            <html lang="en">
            <body style="margin:0;padding:0;background:#f0f2f5;font-family:Arial,sans-serif;">
              <table width="100%" cellpadding="0" cellspacing="0" style="padding:40px 16px;">
                <tr><td align="center">
                  <table width="520" cellpadding="0" cellspacing="0"
                         style="background:#ffffff;border-radius:12px;overflow:hidden;
                                box-shadow:0 4px 16px rgba(0,0,0,0.08);">
                    <!-- Header -->
                    <tr>
                      <td style="background:linear-gradient(135deg,#667eea 0%,#764ba2 100%);
                                 padding:28px 32px;text-align:center;">
                        <p style="margin:0;font-size:2rem;line-height:1;">🚌</p>
                        <p style="margin:6px 0 0;color:#fff;font-size:1.3rem;font-weight:800;
                                  letter-spacing:-0.02em;">BusBooking</p>
                      </td>
                    </tr>
                    <!-- Body -->
                    <tr>
                      <td style="padding:36px 32px;">
                        <h1 style="margin:0 0 8px;color:#1a1a2e;font-size:1.3rem;font-weight:700;">
                          Password reset request
                        </h1>
                        <p style="margin:0 0 24px;color:#555;font-size:0.95rem;line-height:1.6;">
                          Hi {displayName}, we received a request to reset your BusBooking password.
                          Click below to choose a new one.
                        </p>
                        <div style="text-align:center;margin:0 0 28px;">
                          <a href="{resetUrl}"
                             style="display:inline-block;background:linear-gradient(135deg,#667eea,#764ba2);
                                    color:#ffffff;text-decoration:none;padding:14px 36px;
                                    border-radius:8px;font-weight:700;font-size:1rem;">
                            Reset Password
                          </a>
                        </div>
                        <p style="margin:0;color:#888;font-size:0.82rem;line-height:1.6;">
                          This link expires in <strong>1 hour</strong>. If you didn't request a
                          password reset, you can safely ignore this email — your password will
                          not be changed.
                        </p>
                      </td>
                    </tr>
                    <!-- Footer -->
                    <tr>
                      <td style="background:#f8f8fb;padding:16px 32px;text-align:center;
                                 border-top:1px solid #eee;">
                        <p style="margin:0;color:#bbb;font-size:0.75rem;">
                          © 2026 BusBooking · All rights reserved
                        </p>
                      </td>
                    </tr>
                  </table>
                </td></tr>
              </table>
            </body>
            </html>
            """;

        return SendAsync(toEmail, subject, body, ct);
    }

    public Task SendBookingConfirmationAsync(
        string toEmail, string userName, Guid bookingId, IReadOnlyList<int> seatNumbers, decimal totalAmount, CancellationToken ct = default)
    {
        const string subject = "Your BusBooking reservation is confirmed";
        var seats = string.Join(", ", seatNumbers);
        var body = $"""
            <!DOCTYPE html>
            <html lang="en">
            <body style="margin:0;padding:0;background:#f0f2f5;font-family:Arial,sans-serif;">
              <table width="100%" cellpadding="0" cellspacing="0" style="padding:40px 16px;">
                <tr><td align="center">
                  <table width="520" cellpadding="0" cellspacing="0"
                         style="background:#ffffff;border-radius:12px;overflow:hidden;
                                box-shadow:0 4px 16px rgba(0,0,0,0.08);">
                    <tr>
                      <td style="background:linear-gradient(135deg,#667eea 0%,#764ba2 100%);
                                 padding:28px 32px;text-align:center;">
                        <p style="margin:0;font-size:2rem;line-height:1;">🚌</p>
                        <p style="margin:6px 0 0;color:#fff;font-size:1.3rem;font-weight:800;
                                  letter-spacing:-0.02em;">BusBooking</p>
                      </td>
                    </tr>
                    <tr>
                      <td style="padding:36px 32px;">
                        <h1 style="margin:0 0 8px;color:#1a1a2e;font-size:1.3rem;font-weight:700;">
                          Booking confirmed, {userName}!
                        </h1>
                        <p style="margin:0 0 24px;color:#555;font-size:0.95rem;line-height:1.6;">
                          Seat(s) <strong>{seats}</strong> are booked. Total paid: <strong>₹{totalAmount}</strong>.
                          Booking reference: <strong>{bookingId}</strong>.
                        </p>
                      </td>
                    </tr>
                    <tr>
                      <td style="background:#f8f8fb;padding:16px 32px;text-align:center;
                                 border-top:1px solid #eee;">
                        <p style="margin:0;color:#bbb;font-size:0.75rem;">
                          © 2026 BusBooking · All rights reserved
                        </p>
                      </td>
                    </tr>
                  </table>
                </td></tr>
              </table>
            </body>
            </html>
            """;

        return SendAsync(toEmail, subject, body, ct);
    }

    public Task SendBookingCancellationAsync(
        string toEmail, Guid bookingId, IReadOnlyList<int> seatNumbers, CancellationToken ct = default)
    {
        const string subject = "Your BusBooking reservation was cancelled";
        var seats = string.Join(", ", seatNumbers);
        var body = $"""
            <!DOCTYPE html>
            <html lang="en">
            <body style="margin:0;padding:0;background:#f0f2f5;font-family:Arial,sans-serif;">
              <table width="100%" cellpadding="0" cellspacing="0" style="padding:40px 16px;">
                <tr><td align="center">
                  <table width="520" cellpadding="0" cellspacing="0"
                         style="background:#ffffff;border-radius:12px;overflow:hidden;
                                box-shadow:0 4px 16px rgba(0,0,0,0.08);">
                    <tr>
                      <td style="background:linear-gradient(135deg,#667eea 0%,#764ba2 100%);
                                 padding:28px 32px;text-align:center;">
                        <p style="margin:0;font-size:2rem;line-height:1;">🚌</p>
                        <p style="margin:6px 0 0;color:#fff;font-size:1.3rem;font-weight:800;
                                  letter-spacing:-0.02em;">BusBooking</p>
                      </td>
                    </tr>
                    <tr>
                      <td style="padding:36px 32px;">
                        <h1 style="margin:0 0 8px;color:#1a1a2e;font-size:1.3rem;font-weight:700;">
                          Booking cancelled
                        </h1>
                        <p style="margin:0 0 24px;color:#555;font-size:0.95rem;line-height:1.6;">
                          Seat(s) <strong>{seats}</strong> for booking <strong>{bookingId}</strong> have been
                          released. If this wasn't you, contact support right away.
                        </p>
                      </td>
                    </tr>
                    <tr>
                      <td style="background:#f8f8fb;padding:16px 32px;text-align:center;
                                 border-top:1px solid #eee;">
                        <p style="margin:0;color:#bbb;font-size:0.75rem;">
                          © 2026 BusBooking · All rights reserved
                        </p>
                      </td>
                    </tr>
                  </table>
                </td></tr>
              </table>
            </body>
            </html>
            """;

        return SendAsync(toEmail, subject, body, ct);
    }

    private async Task SendAsync(string toEmail, string subject, string htmlBody, CancellationToken ct)
    {
        try
        {
            using var message = new MailMessage(
                new MailAddress(_fromAddr, _fromName),
                new MailAddress(toEmail))
            {
                Subject    = subject,
                Body       = htmlBody,
                IsBodyHtml = true,
            };

            using var client = new SmtpClient(_host, _port)
            {
                EnableSsl             = true,
                UseDefaultCredentials = false,
                Credentials           = new NetworkCredential(_username, _password),
            };

            await client.SendMailAsync(message, ct);
            logger.LogInformation("Email sent to {Email}: {Subject}", toEmail, subject);
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Failed to send email to {Email}: {Subject}", toEmail, subject);
            throw;
        }
    }
}
