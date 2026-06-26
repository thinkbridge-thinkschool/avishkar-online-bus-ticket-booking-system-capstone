using BusBooking.Domain.Common;

namespace BusBooking.Domain.Identity.Entities;

public sealed class AppUserRole : BaseEntity
{
    public Guid AppUserId { get; private set; }

    // Matches Entra app-role names: BusBooking.Vendor, BusBooking.Admin, BusBooking.SuperAdmin
    public string RoleName { get; private set; } = default!;
    public DateTime GrantedAt { get; private set; }

    public AppUser AppUser { get; private set; } = default!;

    private AppUserRole() { }

    public static AppUserRole Create(Guid appUserId, string roleName)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(roleName);
        return new AppUserRole
        {
            AppUserId = appUserId,
            RoleName  = roleName,
            GrantedAt = DateTime.UtcNow
        };
    }
}
