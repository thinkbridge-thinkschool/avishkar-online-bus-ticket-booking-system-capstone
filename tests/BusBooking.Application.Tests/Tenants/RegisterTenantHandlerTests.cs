using BusBooking.Application.Common.Exceptions;
using BusBooking.Application.Tenants.Commands.RegisterTenant;
using BusBooking.Application.Tests.Fakes;
using BusBooking.Domain.Tenants.Enums;

namespace BusBooking.Application.Tests.Tenants;

public sealed class RegisterTenantHandlerTests
{
    private static RegisterTenantCommand MakeCommand(
        string subdomain = "acme",
        string oid       = "oid-001") =>
        new("Acme Bus Lines", subdomain, "admin@acme.com", oid);

    [Fact]
    public async Task HandleAsync_ShouldRegisterTenantAndReturnId()
    {
        var repo    = new FakeTenantRepository();
        var handler = new RegisterTenantHandler(repo);

        var id = await handler.HandleAsync(MakeCommand());

        Assert.NotEqual(Guid.Empty, id);
        Assert.Single(repo.All);
        Assert.Equal(TenantStatus.PendingApproval, repo.All[0].Status);
        Assert.Equal("acme", repo.All[0].Subdomain);
    }

    [Fact]
    public async Task HandleAsync_DuplicateSubdomain_ShouldThrow()
    {
        var repo    = new FakeTenantRepository();
        var handler = new RegisterTenantHandler(repo);

        await handler.HandleAsync(MakeCommand(oid: "oid-001"));

        await Assert.ThrowsAsync<InvalidOperationException>(() =>
            handler.HandleAsync(MakeCommand(oid: "oid-002")));
    }

    [Fact]
    public async Task HandleAsync_DuplicateOid_ShouldThrow()
    {
        var repo    = new FakeTenantRepository();
        var handler = new RegisterTenantHandler(repo);

        await handler.HandleAsync(MakeCommand(subdomain: "acme"));

        await Assert.ThrowsAsync<InvalidOperationException>(() =>
            handler.HandleAsync(MakeCommand(subdomain: "acme2")));
    }

    [Fact]
    public async Task HandleAsync_InvalidSubdomain_ShouldThrowArgumentException()
    {
        var repo    = new FakeTenantRepository();
        var handler = new RegisterTenantHandler(repo);

        // Domain rejects subdomains containing uppercase or special chars
        await Assert.ThrowsAsync<ArgumentException>(() =>
            handler.HandleAsync(new RegisterTenantCommand("X", "AB", "a@b.com", "oid-x")));
    }
}
