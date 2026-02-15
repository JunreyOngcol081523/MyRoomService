using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace MyRoomService.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class ChangeUnitStatusToEnum : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.Sql(@"
        ALTER TABLE ""Units"" 
        ALTER COLUMN ""Status"" TYPE integer 
        USING (
            CASE ""Status"" 
                WHEN 'AVAILABLE' THEN 0 
                WHEN 'OCCUPIED' THEN 1 
                WHEN 'MAINTENANCE' THEN 2 
                WHEN 'RESERVED' THEN 3 
                ELSE 0 
            END
        );");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AlterColumn<string>(
                name: "Status",
                table: "Units",
                type: "text",
                nullable: false,
                oldClrType: typeof(int),
                oldType: "integer");
        }
    }
}
