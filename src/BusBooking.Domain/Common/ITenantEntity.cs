namespace BusBooking.Domain.Common;

public interface ITenantEntity
{
    Guid TenantId { get; }
}
