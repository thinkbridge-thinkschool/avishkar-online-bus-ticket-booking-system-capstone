using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Design;

namespace BusBooking.Infrastructure.Persistence;

internal sealed class DesignTimeBusBookingDbContextFactory : IDesignTimeDbContextFactory<BusBookingDbContext>
{
    public BusBookingDbContext CreateDbContext(string[] args)
    {
        var connectionString =
            Environment.GetEnvironmentVariable("ConnectionStrings__DefaultConnection")
            ?? "Server=(localdb)\\mssqllocaldb;Database=BusBookingDesignTime;Trusted_Connection=True;";

        var opts = new DbContextOptionsBuilder<BusBookingDbContext>()
            .UseSqlServer(connectionString)
            .Options;

        return new BusBookingDbContext(opts);
    }
}
