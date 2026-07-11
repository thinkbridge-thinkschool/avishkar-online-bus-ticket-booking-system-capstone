using BusBooking.Domain.Common;
using Microsoft.AspNetCore.Authorization;

namespace BusBooking.Api.Authorization;

public sealed class SameOwnerAuthorizationHandler : AuthorizationHandler<SameOwnerRequirement, IOwnedResource>
{
    protected override Task HandleRequirementAsync(
        AuthorizationHandlerContext context, SameOwnerRequirement requirement, IOwnedResource resource)
    {
        var callerId = context.User.FindFirst("app:userId")?.Value;
        if (Guid.TryParse(callerId, out var userId) && userId == resource.OwnerId)
        {
            context.Succeed(requirement);
        }
        else if (requirement.ElevatedRoles.Any(context.User.IsInRole))
        {
            context.Succeed(requirement);
        }

        return Task.CompletedTask;
    }
}
