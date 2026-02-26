using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace MyRoomService.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class AddIsResetToMeterReadings : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<bool>(
                name: "IsReset",
                table: "MeterReadings",
                type: "boolean",
                nullable: false,
                defaultValue: false);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "IsReset",
                table: "MeterReadings");
        }
    }
}
