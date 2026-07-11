using BusBooking.Domain.Common;

namespace BusBooking.Api.Authorization;

// Wraps a bare userId route parameter so route-only "get by user" endpoints can be
// authorized through the same SameOwner policy as endpoints that already have a
// fetched entity (e.g. Booking) implementing IOwnedResource directly.
public readonly record struct UserIdResource(Guid UserId) : IOwnedResource
{
    public Guid OwnerId => UserId;
}
