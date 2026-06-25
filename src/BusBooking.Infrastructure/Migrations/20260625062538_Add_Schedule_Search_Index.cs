using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace BusBooking.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class Add_Schedule_Search_Index : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "IX_Schedules_TenantId_TravelDate_IsActive",
                table: "Schedules");

            migrationBuilder.CreateIndex(
                name: "IX_Schedules_Search",
                table: "Schedules",
                columns: new[] { "TravelDate", "IsActive", "TenantId" })
                .Annotation("SqlServer:Include", new[] { "RouteId", "BusId" });
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "IX_Schedules_Search",
                table: "Schedules");

            migrationBuilder.CreateIndex(
                name: "IX_Schedules_TenantId_TravelDate_IsActive",
                table: "Schedules",
                columns: new[] { "TenantId", "TravelDate", "IsActive" });
        }
    }
}
