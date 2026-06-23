namespace BusBooking.Application.Tenants.Commands.RegisterTenant;

public sealed record RegisterTenantCommand(
    string Name,
    string Subdomain,
    string AdminEmail,
    string AdminEntraObjectId);
