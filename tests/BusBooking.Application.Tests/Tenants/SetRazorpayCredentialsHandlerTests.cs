using BusBooking.Application.Common.Exceptions;
using BusBooking.Application.Tenants.Commands.RegisterTenant;
using BusBooking.Application.Tenants.Commands.SetRazorpayCredentials;
using BusBooking.Application.Tests.Fakes;

namespace BusBooking.Application.Tests.Tenants;

public sealed class SetRazorpayCredentialsHandlerTests
{
    [Fact]
    public async Task HandleAsync_ShouldPersistCredentials()
    {
        var repo = new FakeTenantRepository();
        var id   = await new RegisterTenantHandler(repo)
            .HandleAsync(new RegisterTenantCommand("Acme Bus", "acme", "a@acme.com", "oid-001"));
        var handler = new SetRazorpayCredentialsHandler(repo);

        await handler.HandleAsync(new SetRazorpayCredentialsCommand(id, "rzp_live_KEY", "secret123"));

        var tenant = repo.All.Single();
        Assert.Equal("rzp_live_KEY", tenant.RazorpayKeyId);
        Assert.NotNull(tenant.RazorpayKeySecret);
    }

    [Fact]
    public async Task HandleAsync_TenantNotFound_ShouldThrow()
    {
        var repo    = new FakeTenantRepository();
        var handler = new SetRazorpayCredentialsHandler(repo);

        await Assert.ThrowsAsync<NotFoundException>(() =>
            handler.HandleAsync(new SetRazorpayCredentialsCommand(Guid.NewGuid(), "key", "secret")));
    }
}
