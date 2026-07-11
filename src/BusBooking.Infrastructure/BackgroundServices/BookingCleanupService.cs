using System.Diagnostics;
using BusBooking.Domain.Booking.Enums;
using BusBooking.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;

namespace BusBooking.Infrastructure.BackgroundServices;

internal sealed class BookingCleanupService(
    IServiceScopeFactory scopeFactory,
    ILogger<BookingCleanupService> logger) : BackgroundService
{
    private static readonly ActivitySource _source = new("BusBooking.Worker");
    private static readonly TimeSpan Interval = TimeSpan.FromMinutes(5);
    private static readonly TimeSpan PaymentTimeout = TimeSpan.FromMinutes(30);

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        while (!stoppingToken.IsCancellationRequested)
        {
            await Task.Delay(Interval, stoppingToken);
            try
            {
                await CancelExpiredPaymentPendingBookingsAsync(stoppingToken);
            }
            catch (OperationCanceledException) when (stoppingToken.IsCancellationRequested)
            {
                break;
            }
            catch (Exception ex)
            {
                logger.LogWarning(ex,
                    "BookingCleanupService: failed to cancel expired bookings; will retry in {Interval}", Interval);
            }
        }
    }

    // Internal (not private) so tests can invoke a single cleanup cycle directly.
    internal async Task CancelExpiredPaymentPendingBookingsAsync(CancellationToken ct)
    {
        using var activity = _source.StartActivity("BookingCleanupService.CancelExpiredBookings");

        using var scope = scopeFactory.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<BusBookingDbContext>();

        var cutoff = DateTime.UtcNow - PaymentTimeout;

        var expiredBookings = await db.Bookings
            .Where(b => b.Status == BookingStatus.PaymentPending && b.BookedAt < cutoff)
            .ToListAsync(ct);

        if (expiredBookings.Count == 0) return;

        var scheduleIds = expiredBookings.Select(b => b.ScheduleId).Distinct().ToList();
        var schedules = await db.Schedules
            .Include(s => s.Seats)
            .Where(s => scheduleIds.Contains(s.Id))
            .ToListAsync(ct);

        var scheduleMap = schedules.ToDictionary(s => s.Id);

        foreach (var booking in expiredBookings)
        {
            booking.Cancel();
            if (scheduleMap.TryGetValue(booking.ScheduleId, out var schedule))
                schedule.ReleaseSeats(booking.Seats.Select(s => s.SeatNumber).ToList());
        }

        // BookingCancelledEvent for each booking is turned into an Outbox row by
        // OutboxSavingChangesInterceptor as part of this save — previously this loop cleared
        // domain events itself without ever publishing them, silently dropping the event.
        await db.SaveChangesAsync(ct);

        activity?.SetTag("bookings.cancelled", expiredBookings.Count);
        logger.LogInformation(
            "BookingCleanupService: cancelled {Count} expired payment-pending bookings",
            expiredBookings.Count);
    }
}
