namespace BusBooking.Application.Buses.Queries.GetVendorBuses;

public sealed class GetVendorBusesHandler(IBusRepository busRepo)
{
    public async Task<IReadOnlyList<BusDto>> HandleAsync(GetVendorBusesQuery query, CancellationToken ct = default)
    {
        var buses = await busRepo.GetByVendorIdAsync(query.VendorId, ct);
        return buses.Select(b => new BusDto(b.Id, b.BusNumber, b.BusName, b.BusType, b.TotalSeats, b.VendorId, b.IsActive))
                    .ToList();
    }
}
