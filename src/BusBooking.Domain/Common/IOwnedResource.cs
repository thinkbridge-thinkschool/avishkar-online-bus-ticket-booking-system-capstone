namespace BusBooking.Domain.Common;

public interface IOwnedResource
{
    Guid OwnerId { get; }
}
