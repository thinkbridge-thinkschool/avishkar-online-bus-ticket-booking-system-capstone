namespace BusBooking.Application.Tenants.Queries.GetTenantById;

public sealed record TenantDto(
    Guid TenantId,
    string Name,
    string Subdomain,
    string AdminEmail,
    string Status,
    DateTime? ApprovedAt,
    bool HasRazorpayCredentials,
    DateTime CreatedAt);
