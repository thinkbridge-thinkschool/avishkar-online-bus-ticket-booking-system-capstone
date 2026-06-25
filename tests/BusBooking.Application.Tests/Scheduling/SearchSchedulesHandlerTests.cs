using BusBooking.Application.Scheduling.Queries.SearchSchedules;
using BusBooking.Application.Tests.Fakes;
using BusBooking.Domain.Scheduling.Enums;

namespace BusBooking.Application.Tests.Scheduling;

public sealed class SearchSchedulesHandlerTests
{
    [Fact]
    public async Task HandleAsync_ShouldReturnRepositoryResultUnchanged()
    {
        var expected = new List<ScheduleSummaryDto>
        {
            new(
                Guid.NewGuid(),
                "Express",
                "MH-12-AB-1234",
                "Mumbai",
                "Pune",
                new DateOnly(2026, 6, 24),
                new TimeOnly(8, 0),
                new TimeOnly(12, 0),
                10,
                399m,
                BusType.Sleeper),
        };

        var repo = new FakeScheduleRepository { SearchResults = expected };
        var handler = new SearchSchedulesHandler(repo);

        var result = await handler.HandleAsync(new SearchSchedulesQuery("Mumbai", "Pune", new DateOnly(2026, 6, 24)));

        Assert.Same(expected, result);
    }
}
