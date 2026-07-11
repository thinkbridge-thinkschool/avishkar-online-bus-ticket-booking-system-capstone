using BusBooking.Domain.Common;
using BusBooking.Domain.Feedback.Enums;

namespace BusBooking.Domain.Feedback.Entities;

public sealed class FeedbackEntry : BaseEntity, ITenantEntity, IOwnedResource
{
    public Guid UserId { get; private set; }
    Guid IOwnedResource.OwnerId => UserId;
    public Guid BookingId { get; private set; }
    public Guid ScheduleId { get; private set; }
    public Guid TenantId { get; private set; }
    public int Rating { get; private set; }
    public string Comment { get; private set; } = default!;
    public FeedbackCategory Category { get; private set; }

    private FeedbackEntry() { }

    public static FeedbackEntry Create(Guid userId, Guid bookingId, Guid scheduleId, int rating, string comment, FeedbackCategory category, Guid tenantId)
    {
        if (userId == Guid.Empty)
            throw new ArgumentException("UserId must not be empty.", nameof(userId));
        if (bookingId == Guid.Empty)
            throw new ArgumentException("BookingId must not be empty.", nameof(bookingId));
        if (scheduleId == Guid.Empty)
            throw new ArgumentException("ScheduleId must not be empty.", nameof(scheduleId));
        if (rating < 1 || rating > 5)
            throw new ArgumentOutOfRangeException(nameof(rating), "Rating must be between 1 and 5.");
        ArgumentException.ThrowIfNullOrWhiteSpace(comment);
        if (comment.Length > 1000)
            throw new ArgumentException("Comment must not exceed 1000 characters.", nameof(comment));

        return new FeedbackEntry
        {
            UserId     = userId,
            BookingId  = bookingId,
            ScheduleId = scheduleId,
            TenantId   = tenantId,
            Rating     = rating,
            Comment    = comment,
            Category   = category
        };
    }

    public void Update(int rating, string comment, FeedbackCategory category)
    {
        if (rating < 1 || rating > 5)
            throw new ArgumentOutOfRangeException(nameof(rating), "Rating must be between 1 and 5.");
        ArgumentException.ThrowIfNullOrWhiteSpace(comment);
        if (comment.Length > 1000)
            throw new ArgumentException("Comment must not exceed 1000 characters.", nameof(comment));

        Rating    = rating;
        Comment   = comment;
        Category  = category;
        UpdatedAt = DateTime.UtcNow;
    }
}
