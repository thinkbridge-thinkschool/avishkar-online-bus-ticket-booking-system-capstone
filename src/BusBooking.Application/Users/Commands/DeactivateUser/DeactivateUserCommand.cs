namespace BusBooking.Application.Users.Commands.DeactivateUser;

public sealed record DeactivateUserCommand(Guid UserId, string RequestingEntraObjectId);
