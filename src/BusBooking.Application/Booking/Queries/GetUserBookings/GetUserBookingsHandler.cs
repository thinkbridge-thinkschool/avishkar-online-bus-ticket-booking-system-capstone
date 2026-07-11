using BusBooking.Application.Booking.Repositories;

namespace BusBooking.Application.Booking.Queries.GetUserBookings;

public sealed class GetUserBookingsHandler(IBookingRepository bookingRepo)
{
    public Task<IReadOnlyList<BookingDto>> HandleAsync(
        GetUserBookingsQuery query, CancellationToken ct = default) =>
        bookingRepo.GetByUserIdWithDetailsAsync(query.UserId, ct);
}
