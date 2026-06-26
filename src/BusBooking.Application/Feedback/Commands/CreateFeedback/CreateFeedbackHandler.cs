using BusBooking.Application.Booking.Repositories;
using BusBooking.Application.Common.Exceptions;
using BusBooking.Domain.Feedback.Entities;

namespace BusBooking.Application.Feedback.Commands.CreateFeedback;

public sealed class CreateFeedbackHandler(IFeedbackRepository feedbackRepo, IBookingRepository bookingRepo)
{
    public async Task<Guid> HandleAsync(CreateFeedbackCommand command, CancellationToken ct = default)
    {
        var booking = await bookingRepo.GetByIdAsync(command.BookingId, ct)
            ?? throw new NotFoundException("Booking", command.BookingId);

        if (booking.UserId != command.UserId)
            throw new UnauthorizedAccessException("You do not own this booking.");

        var existing = await feedbackRepo.GetByBookingIdAsync(command.BookingId, ct);
        if (existing is not null)
            throw new InvalidOperationException("Feedback has already been submitted for this booking.");

        // FeedbackEntry inherits tenant from the booking it reviews.
        var entry = FeedbackEntry.Create(command.UserId, command.BookingId, command.ScheduleId,
                                         command.Rating, command.Comment, command.Category, booking.TenantId);
        await feedbackRepo.AddAsync(entry, ct);
        await feedbackRepo.SaveChangesAsync(ct);
        return entry.Id;
    }
}
