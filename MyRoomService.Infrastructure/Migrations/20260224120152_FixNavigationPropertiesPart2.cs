using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace MyRoomService.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class FixNavigationPropertiesPart2 : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_ContractIncludedServices_UnitServices_UnitServiceId",
                table: "ContractIncludedServices");

            migrationBuilder.AddForeignKey(
                name: "FK_ContractIncludedServices_UnitServices_UnitServiceId",
                table: "ContractIncludedServices",
                column: "UnitServiceId",
                principalTable: "UnitServices",
                principalColumn: "Id",
                onDelete: ReferentialAction.Restrict);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_ContractIncludedServices_UnitServices_UnitServiceId",
                table: "ContractIncludedServices");

            migrationBuilder.AddForeignKey(
                name: "FK_ContractIncludedServices_UnitServices_UnitServiceId",
                table: "ContractIncludedServices",
                column: "UnitServiceId",
                principalTable: "UnitServices",
                principalColumn: "Id",
                onDelete: ReferentialAction.Cascade);
        }
    }
}
