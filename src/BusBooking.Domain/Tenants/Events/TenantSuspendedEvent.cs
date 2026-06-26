using BusBooking.Domain.Common;

namespace BusBooking.Domain.Tenants.Events;

public sealed record TenantSuspendedEvent(Guid TenantId, string TenantName, DateTime SuspendedAt) : IDomainEvent;
