using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace MyRoomService.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class AddIsArchivedToBuilding : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<bool>(
                name: "IsArchived",
                table: "Buildings",
                type: "boolean",
                nullable: false,
                defaultValue: false);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "IsArchived",
                table: "Buildings");
        }
    }
}
