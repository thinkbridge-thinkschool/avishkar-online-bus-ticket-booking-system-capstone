using BusBooking.Domain.Common;
using BusBooking.Domain.Vendor.Enums;
using BusBooking.Domain.Vendor.Events;

namespace BusBooking.Domain.Vendor.Aggregates;

public sealed class Vendor : BaseEntity
{
    public string EntraObjectId { get; private set; } = default!;
    public string VendorName { get; private set; } = default!;
    public string Email { get; private set; } = default!;
    public string PhoneNumber { get; private set; } = default!;
    public string Address { get; private set; } = default!;
    public string LicenseNumber { get; private set; } = default!;
    public VendorStatus Status { get; private set; } = VendorStatus.Pending;
    public bool IsActive { get; private set; } = true;

    private Vendor() { }

    public static Vendor Register(string entraObjectId, string vendorName, string email, string phone, string address, string licenseNumber)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(entraObjectId);
        ArgumentException.ThrowIfNullOrWhiteSpace(vendorName);
        ArgumentException.ThrowIfNullOrWhiteSpace(email);
        ArgumentException.ThrowIfNullOrWhiteSpace(phone);
        ArgumentException.ThrowIfNullOrWhiteSpace(address);
        ArgumentException.ThrowIfNullOrWhiteSpace(licenseNumber);

        return new Vendor
        {
            EntraObjectId = entraObjectId,
            VendorName = vendorName,
            Email = email,
            PhoneNumber = phone,
            Address = address,
            LicenseNumber = licenseNumber,
            Status = VendorStatus.Pending,
            IsActive = true
        };
    }

    public void Approve()
    {
        if (Status == VendorStatus.Approved)
            throw new InvalidOperationException("Vendor is already approved.");

        Status = VendorStatus.Approved;
        UpdatedAt = DateTime.UtcNow;
        RaiseDomainEvent(new VendorApprovedEvent(Id, VendorName, Email));
    }

    public void Reject(string reason)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(reason);

        if (Status == VendorStatus.Rejected)
            throw new InvalidOperationException("Vendor is already rejected.");

        Status = VendorStatus.Rejected;
        UpdatedAt = DateTime.UtcNow;
        RaiseDomainEvent(new VendorRejectedEvent(Id, VendorName, Email, reason));
    }

    public void UpdateProfile(string vendorName, string phone, string address)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(vendorName);
        ArgumentException.ThrowIfNullOrWhiteSpace(phone);
        ArgumentException.ThrowIfNullOrWhiteSpace(address);

        VendorName = vendorName;
        PhoneNumber = phone;
        Address = address;
        UpdatedAt = DateTime.UtcNow;
    }

    public void Deactivate()
    {
        IsActive = false;
        UpdatedAt = DateTime.UtcNow;
    }
}
