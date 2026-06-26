using BusBooking.Application.Scheduling.Queries.SearchSchedules;
using BusBooking.Application.Scheduling.Repositories;
using BusBooking.Domain.Scheduling.Entities;

namespace BusBooking.Application.Tests.Fakes;

public sealed class FakeScheduleRepository : IScheduleRepository
{
    public Schedule? ScheduleForGetByIdWithSeats { get; set; }
    public IReadOnlyList<ScheduleSummaryDto> SearchResults { get; set; } = [];

    public Task<Schedule?> GetByIdWithSeatsAsync(Guid scheduleId, CancellationToken ct = default) =>
        Task.FromResult(ScheduleForGetByIdWithSeats);

    public Task<IReadOnlyList<ScheduleSummaryDto>> SearchAsync(
        string source,
        string destination,
        DateOnly travelDate,
        CancellationToken ct = default) =>
        Task.FromResult(SearchResults);

    public Task<IReadOnlyList<Schedule>> GetByVendorIdAsync(Guid vendorId, CancellationToken ct = default) =>
        Task.FromResult<IReadOnlyList<Schedule>>([]);

    public Task AddAsync(Schedule schedule, CancellationToken ct = default) => Task.CompletedTask;

    public Task SaveChangesAsync(CancellationToken ct = default) => Task.CompletedTask;
}
