using BusBooking.Domain.Common;
using BusBooking.Domain.Scheduling.Enums;

namespace BusBooking.Domain.Scheduling.Entities;

public sealed class Bus : BaseEntity, ITenantEntity
{
    public string BusNumber { get; private set; } = default!;
    public string BusName { get; private set; } = default!;
    public BusType BusType { get; private set; }
    public int TotalSeats { get; private set; }
    public Guid VendorId { get; private set; }
    public Guid TenantId { get; private set; }
    public bool IsActive { get; private set; } = true;

    private Bus() { }

    public static Bus Create(string busNumber, string busName, BusType busType, int totalSeats, Guid vendorId, Guid tenantId) =>
        new() { BusNumber = busNumber, BusName = busName, BusType = busType, TotalSeats = totalSeats, VendorId = vendorId, TenantId = tenantId };

    public void UpdateDetails(string busName, int totalSeats)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(busName);
        if (totalSeats <= 0) throw new ArgumentException("TotalSeats must be greater than zero.", nameof(totalSeats));
        BusName = busName;
        TotalSeats = totalSeats;
        UpdatedAt = DateTime.UtcNow;
    }

    public void Deactivate()
    {
        IsActive = false;
        UpdatedAt = DateTime.UtcNow;
    }
}
