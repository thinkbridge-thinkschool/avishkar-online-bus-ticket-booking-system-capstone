using BusBooking.Application.Common;
using BusBooking.Application.Scheduling.Repositories;

namespace BusBooking.Application.Scheduling.Queries.SearchSchedules;

public sealed class SearchSchedulesHandler(IScheduleRepository scheduleRepo, ICacheService cache)
{
    // Layered under ScheduleEndpoints' own OutputCache: OutputCache is per-instance in-memory,
    // so on a multi-instance deployment a cache miss on one instance still re-hits SQL even
    // when another instance just answered the identical query — HybridCache (Redis-backed) is
    // what actually shares this across instances.
    public Task<IReadOnlyList<ScheduleSummaryDto>> HandleAsync(
        SearchSchedulesQuery query, CancellationToken ct = default) =>
        cache.GetOrCreateAsync(
            $"schedules:search:{query.Source}:{query.Destination}:{query.TravelDate:yyyy-MM-dd}",
            token => scheduleRepo.SearchAsync(query.Source, query.Destination, query.TravelDate, token),
            TimeSpan.FromMinutes(2),
            ["schedules"],
            ct).AsTask();
}
