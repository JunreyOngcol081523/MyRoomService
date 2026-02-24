using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace MyRoomService.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class AddMeterReadingsAndUnitServiceUpdates : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<bool>(
                name: "IsMetered",
                table: "UnitServices",
                type: "boolean",
                nullable: false,
                defaultValue: false);

            migrationBuilder.AddColumn<string>(
                name: "MeterNumber",
                table: "UnitServices",
                type: "text",
                nullable: true);

            migrationBuilder.CreateTable(
                name: "MeterReadings",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    TenantId = table.Column<Guid>(type: "uuid", nullable: false),
                    UnitServiceId = table.Column<Guid>(type: "uuid", nullable: false),
                    PreviousValue = table.Column<double>(type: "double precision", nullable: false),
                    CurrentValue = table.Column<double>(type: "double precision", nullable: false),
                    ReadingDate = table.Column<DateTime>(type: "timestamp without time zone", nullable: false),
                    IsBilled = table.Column<bool>(type: "boolean", nullable: false),
                    Notes = table.Column<string>(type: "text", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_MeterReadings", x => x.Id);
                    table.ForeignKey(
                        name: "FK_MeterReadings_UnitServices_UnitServiceId",
                        column: x => x.UnitServiceId,
                        principalTable: "UnitServices",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "IX_MeterReadings_UnitServiceId",
                table: "MeterReadings",
                column: "UnitServiceId");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "MeterReadings");

            migrationBuilder.DropColumn(
                name: "IsMetered",
                table: "UnitServices");

            migrationBuilder.DropColumn(
                name: "MeterNumber",
                table: "UnitServices");
        }
    }
}
