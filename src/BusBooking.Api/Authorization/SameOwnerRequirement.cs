using Microsoft.AspNetCore.Authorization;

namespace BusBooking.Api.Authorization;

public sealed class SameOwnerRequirement : IAuthorizationRequirement
{
    public IReadOnlyCollection<string> ElevatedRoles { get; } = ["BusBooking.SuperAdmin"];
}
