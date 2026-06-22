namespace BusBooking.Domain.Tenants.Enums;

public enum TenantStatus
{
    PendingApproval = 0,
    Active          = 1,
    Suspended       = 2,
    Rejected        = 3,
    Deactivated     = 4,
}
