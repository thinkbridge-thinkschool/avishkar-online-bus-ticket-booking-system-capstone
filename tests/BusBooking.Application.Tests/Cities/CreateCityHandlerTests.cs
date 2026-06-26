using BusBooking.Application.Cities.Commands.CreateCity;
using BusBooking.Application.Tests.Fakes;

namespace BusBooking.Application.Tests.Cities;

public sealed class CreateCityHandlerTests
{
    [Fact]
    public async Task HandleAsync_ShouldCreateCityAndReturnId()
    {
        var repo = new FakeCityRepository();
        var handler = new CreateCityHandler(repo);

        var id = await handler.HandleAsync(new CreateCityCommand("Mumbai"));

        Assert.NotEqual(Guid.Empty, id);
        Assert.Single(repo.All);
        Assert.Equal("Mumbai", repo.All[0].CityName);
    }

    [Fact]
    public async Task HandleAsync_DuplicateName_ShouldThrow()
    {
        var repo = new FakeCityRepository();
        var handler = new CreateCityHandler(repo);

        await handler.HandleAsync(new CreateCityCommand("Delhi"));

        await Assert.ThrowsAsync<InvalidOperationException>(() =>
            handler.HandleAsync(new CreateCityCommand("Delhi")));
    }
}
