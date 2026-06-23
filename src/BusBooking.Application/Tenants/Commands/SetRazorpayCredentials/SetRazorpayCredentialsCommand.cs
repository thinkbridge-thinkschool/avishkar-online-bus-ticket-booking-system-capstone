namespace BusBooking.Application.Tenants.Commands.SetRazorpayCredentials;

public sealed record SetRazorpayCredentialsCommand(Guid TenantId, string KeyId, string KeySecret);
