using BusBooking.Application.Common.Exceptions;

namespace BusBooking.Application.Tenants.Commands.SetRazorpayCredentials;

public sealed class SetRazorpayCredentialsHandler(ITenantRepository tenantRepo)
{
    public async Task HandleAsync(SetRazorpayCredentialsCommand command, CancellationToken ct = default)
    {
        var tenant = await tenantRepo.GetByIdAsync(command.TenantId, ct)
            ?? throw new NotFoundException("Tenant", command.TenantId);

        tenant.SetRazorpayCredentials(command.KeyId, command.KeySecret);

        await tenantRepo.SaveChangesAsync(ct);
    }
}
