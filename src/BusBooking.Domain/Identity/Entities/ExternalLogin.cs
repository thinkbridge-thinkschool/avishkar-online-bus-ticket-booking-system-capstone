using BusBooking.Domain.Common;
using BusBooking.Domain.Identity.Enums;

namespace BusBooking.Domain.Identity.Entities;

public sealed class ExternalLogin : BaseEntity
{
    public Guid AppUserId { get; private set; }
    public LoginProvider LoginProvider { get; private set; }

    // OID string for Entra; email for Local
    public string ProviderKey { get; private set; } = default!;

    public AppUser AppUser { get; private set; } = default!;

    private ExternalLogin() { }

    public static ExternalLogin Create(Guid appUserId, LoginProvider provider, string providerKey)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(providerKey);
        return new ExternalLogin
        {
            AppUserId    = appUserId,
            LoginProvider = provider,
            ProviderKey  = providerKey
        };
    }
}
