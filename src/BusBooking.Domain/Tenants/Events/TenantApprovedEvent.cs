using BusBooking.Domain.Common;

namespace BusBooking.Domain.Tenants.Events;

public sealed record TenantApprovedEvent(Guid TenantId, string TenantName, string AdminEmail, DateTime ApprovedAt) : IDomainEvent;
