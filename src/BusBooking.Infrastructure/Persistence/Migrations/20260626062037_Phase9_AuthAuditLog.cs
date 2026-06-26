using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace BusBooking.Infrastructure.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class Phase9_AuthAuditLog : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "AuthAuditLogs",
                columns: table => new
                {
                    Id = table.Column<long>(type: "bigint", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    AppUserId = table.Column<Guid>(type: "uniqueidentifier", nullable: true),
                    Email = table.Column<string>(type: "nvarchar(256)", maxLength: 256, nullable: false),
                    EventType = table.Column<string>(type: "nvarchar(64)", maxLength: 64, nullable: false),
                    Success = table.Column<bool>(type: "bit", nullable: false),
                    IpAddress = table.Column<string>(type: "nvarchar(45)", maxLength: 45, nullable: true),
                    UserAgent = table.Column<string>(type: "nvarchar(512)", maxLength: 512, nullable: true),
                    CreatedAt = table.Column<DateTime>(type: "datetime2", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_AuthAuditLogs", x => x.Id);
                });

            migrationBuilder.CreateIndex(
                name: "IX_AuthAuditLogs_AppUserId",
                table: "AuthAuditLogs",
                column: "AppUserId");

            migrationBuilder.CreateIndex(
                name: "IX_AuthAuditLogs_CreatedAt",
                table: "AuthAuditLogs",
                column: "CreatedAt");

            migrationBuilder.CreateIndex(
                name: "IX_AuthAuditLogs_EventType",
                table: "AuthAuditLogs",
                column: "EventType");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "AuthAuditLogs");
        }
    }
}
