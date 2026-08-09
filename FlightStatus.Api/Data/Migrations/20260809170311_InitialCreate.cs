using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace FlightStatus.Api.Data.Migrations
{
    /// <inheritdoc />
    public partial class InitialCreate : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "FlightCatalog",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    FlightNumber = table.Column<string>(type: "nvarchar(10)", maxLength: 10, nullable: false),
                    Route = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: false),
                    Origin = table.Column<string>(type: "nvarchar(5)", maxLength: 5, nullable: false),
                    Destination = table.Column<string>(type: "nvarchar(5)", maxLength: 5, nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_FlightCatalog", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "FlightProviderData",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    FlightNumber = table.Column<string>(type: "nvarchar(10)", maxLength: 10, nullable: false),
                    ProviderName = table.Column<string>(type: "nvarchar(20)", maxLength: 20, nullable: false),
                    RawStatus = table.Column<string>(type: "nvarchar(30)", maxLength: 30, nullable: false),
                    ScheduledDeparture = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: true),
                    ActualDeparture = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: true),
                    ScheduledArrival = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: true),
                    ActualArrival = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: true),
                    Terminal = table.Column<string>(type: "nvarchar(10)", maxLength: 10, nullable: true),
                    Gate = table.Column<string>(type: "nvarchar(10)", maxLength: 10, nullable: true),
                    DelayReason = table.Column<string>(type: "nvarchar(200)", maxLength: 200, nullable: true),
                    LastUpdatedUtc = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_FlightProviderData", x => x.Id);
                });

            migrationBuilder.CreateIndex(
                name: "IX_FlightCatalog_FlightNumber",
                table: "FlightCatalog",
                column: "FlightNumber",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_FlightProviderData_FlightNumber_ProviderName",
                table: "FlightProviderData",
                columns: new[] { "FlightNumber", "ProviderName" },
                unique: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "FlightCatalog");

            migrationBuilder.DropTable(
                name: "FlightProviderData");
        }
    }
}
