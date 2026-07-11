using System.Text.Json;
using BusBooking.Application.Common;
using BusBooking.Application.Common.Interfaces;
using BusBooking.Domain.Booking.Events;
using BusBooking.Infrastructure.Messaging;
using BusBooking.Infrastructure.Persistence;
using BusBooking.Infrastructure.Tenancy;
using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging.Abstractions;

namespace BusBooking.Api.IntegrationTests.Messaging;

public sealed class ServiceBusConsumerServiceTests : IDisposable
{
    private readonly SqliteConnection _connection;
    private readonly IServiceProvider _provider;
    private readonly SpyEmailService _emailService = new();

    public ServiceBusConsumerServiceTests()
    {
        _connection = new SqliteConnection("DataSource=:memory:");
        _connection.Open();

        var services = new ServiceCollection();
        services.AddScoped<TenantContext>();
        services.AddScoped<ITenantContext>(sp => sp.GetRequiredService<TenantContext>());
        services.AddDbContext<BusBookingDbContext>(opts => opts.UseSqlite(_connection));
        services.AddSingleton<IEmailService>(_emailService);
        _provider = services.BuildServiceProvider();

        using var scope = _provider.CreateScope();
        scope.ServiceProvider.GetRequiredService<BusBookingDbContext>().Database.EnsureCreated();
    }

    public void Dispose() => _connection.Dispose();

    [Fact]
    public async Task HandleMessageAsync_SameMessageIdTwice_OnlyProcessesOnce()
    {
        // Simulates Service Bus's at-least-once redelivery of a message that was already
        // fully processed (e.g. the CompleteMessageAsync ack was lost) — the Inbox check
        // (ProcessedMessage) must make the second delivery a no-op.
        var consumer = new ServiceBusConsumerService(
            null!, // unused — HandleMessageAsync never touches the ServiceBusClient directly
            new TestScopeFactory(_provider),
            NullLogger<ServiceBusConsumerService>.Instance);

        var evt = new BookingConfirmedEvent(Guid.NewGuid(), "user@example.com", "Test User", Guid.NewGuid(), 500m, [1]);
        var body = JsonSerializer.Serialize(evt);
        var messageId = Guid.NewGuid().ToString();

        await consumer.HandleMessageAsync(
            messageId, nameof(BookingConfirmedEvent), body, ServiceBusConsumerService.SubscriptionConfirmed, CancellationToken.None);
        await consumer.HandleMessageAsync(
            messageId, nameof(BookingConfirmedEvent), body, ServiceBusConsumerService.SubscriptionConfirmed, CancellationToken.None);

        Assert.Equal(1, _emailService.ConfirmationCallCount);

        using var scope = _provider.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<BusBookingDbContext>();
        Assert.Equal(1, await db.ProcessedMessages.CountAsync());
    }

    private sealed class SpyEmailService : IEmailService
    {
        public int ConfirmationCallCount { get; private set; }

        public Task SendEmailVerificationAsync(string toEmail, string displayName, string verificationUrl, CancellationToken ct = default) =>
            Task.CompletedTask;

        public Task SendPasswordResetAsync(string toEmail, string displayName, string resetUrl, CancellationToken ct = default) =>
            Task.CompletedTask;

        public Task SendBookingConfirmationAsync(
            string toEmail, string userName, Guid bookingId, IReadOnlyList<int> seatNumbers, decimal totalAmount, CancellationToken ct = default)
        {
            ConfirmationCallCount++;
            return Task.CompletedTask;
        }

        public Task SendBookingCancellationAsync(string toEmail, Guid bookingId, IReadOnlyList<int> seatNumbers, CancellationToken ct = default) =>
            Task.CompletedTask;
    }

    private sealed class TestScopeFactory(IServiceProvider provider) : IServiceScopeFactory
    {
        public IServiceScope CreateScope() => provider.CreateScope();
    }
}
