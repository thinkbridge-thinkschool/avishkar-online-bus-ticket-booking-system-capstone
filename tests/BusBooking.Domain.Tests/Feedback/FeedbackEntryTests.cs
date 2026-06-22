using BusBooking.Domain.Feedback.Entities;
using BusBooking.Domain.Feedback.Enums;

namespace BusBooking.Domain.Tests.Feedback;

public sealed class FeedbackEntryTests
{
    [Fact]
    public void Create_ShouldSetAllFields()
    {
        var userId     = Guid.NewGuid();
        var bookingId  = Guid.NewGuid();
        var scheduleId = Guid.NewGuid();
        var tenantId   = Guid.NewGuid();

        var entry = FeedbackEntry.Create(userId, bookingId, scheduleId, 4, "Good service.", FeedbackCategory.Service, tenantId);

        Assert.Equal(userId, entry.UserId);
        Assert.Equal(bookingId, entry.BookingId);
        Assert.Equal(scheduleId, entry.ScheduleId);
        Assert.Equal(tenantId, entry.TenantId);
        Assert.Equal(4, entry.Rating);
        Assert.Equal("Good service.", entry.Comment);
        Assert.Equal(FeedbackCategory.Service, entry.Category);
    }

    [Fact]
    public void Create_WithRatingBelow1_ShouldThrow()
    {
        Assert.Throws<ArgumentOutOfRangeException>(() =>
            FeedbackEntry.Create(Guid.NewGuid(), Guid.NewGuid(), Guid.NewGuid(), 0, "Bad", FeedbackCategory.Service, Guid.NewGuid()));
    }

    [Fact]
    public void Create_WithRatingAbove5_ShouldThrow()
    {
        Assert.Throws<ArgumentOutOfRangeException>(() =>
            FeedbackEntry.Create(Guid.NewGuid(), Guid.NewGuid(), Guid.NewGuid(), 6, "Too good", FeedbackCategory.General, Guid.NewGuid()));
    }

    [Fact]
    public void Create_WithEmptyComment_ShouldThrow()
    {
        Assert.Throws<ArgumentException>(() =>
            FeedbackEntry.Create(Guid.NewGuid(), Guid.NewGuid(), Guid.NewGuid(), 3, "", FeedbackCategory.General, Guid.NewGuid()));
    }

    [Fact]
    public void Create_WithCommentOver1000Chars_ShouldThrow()
    {
        var longComment = new string('x', 1001);
        Assert.Throws<ArgumentException>(() =>
            FeedbackEntry.Create(Guid.NewGuid(), Guid.NewGuid(), Guid.NewGuid(), 3, longComment, FeedbackCategory.General, Guid.NewGuid()));
    }

    [Fact]
    public void Create_WithEmptyUserId_ShouldThrow()
    {
        Assert.Throws<ArgumentException>(() =>
            FeedbackEntry.Create(Guid.Empty, Guid.NewGuid(), Guid.NewGuid(), 3, "ok", FeedbackCategory.General, Guid.NewGuid()));
    }

    [Fact]
    public void Update_ShouldMutateFields()
    {
        var entry = FeedbackEntry.Create(Guid.NewGuid(), Guid.NewGuid(), Guid.NewGuid(), 3, "OK", FeedbackCategory.Service, Guid.NewGuid());
        entry.Update(5, "Excellent!", FeedbackCategory.Cleanliness);

        Assert.Equal(5, entry.Rating);
        Assert.Equal("Excellent!", entry.Comment);
        Assert.Equal(FeedbackCategory.Cleanliness, entry.Category);
    }

    [Fact]
    public void Update_WithInvalidRating_ShouldThrow()
    {
        var entry = FeedbackEntry.Create(Guid.NewGuid(), Guid.NewGuid(), Guid.NewGuid(), 3, "OK", FeedbackCategory.Service, Guid.NewGuid());

        Assert.Throws<ArgumentOutOfRangeException>(() => entry.Update(0, "bad", FeedbackCategory.Service));
    }
}
