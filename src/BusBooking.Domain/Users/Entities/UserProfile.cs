using BusBooking.Domain.Common;

namespace BusBooking.Domain.Users.Entities;

public sealed class UserProfile : BaseEntity
{
    public string EntraObjectId { get; private set; } = default!;
    public string FirstName { get; private set; } = default!;
    public string LastName { get; private set; } = default!;
    public string Email { get; private set; } = default!;
    public string? Phone { get; private set; }
    public string? Address { get; private set; }
    public bool IsActive { get; private set; } = true;

    private UserProfile() { }

    public static UserProfile Create(string entraObjectId, string firstName, string lastName, string email)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(entraObjectId);
        ArgumentException.ThrowIfNullOrWhiteSpace(firstName);
        ArgumentException.ThrowIfNullOrWhiteSpace(lastName);
        ArgumentException.ThrowIfNullOrWhiteSpace(email);

        return new UserProfile
        {
            EntraObjectId = entraObjectId,
            FirstName = firstName,
            LastName = lastName,
            Email = email,
            IsActive = true
        };
    }

    public void Update(string firstName, string lastName, string? phone, string? address)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(firstName);
        ArgumentException.ThrowIfNullOrWhiteSpace(lastName);

        FirstName = firstName;
        LastName = lastName;
        Phone = phone;
        Address = address;
        UpdatedAt = DateTime.UtcNow;
    }

    public void Deactivate()
    {
        IsActive = false;
        UpdatedAt = DateTime.UtcNow;
    }
}
