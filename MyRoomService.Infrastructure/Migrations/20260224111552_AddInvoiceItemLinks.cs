using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace MyRoomService.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class AddInvoiceItemLinks : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateIndex(
                name: "IX_InvoiceItems_ContractAddOnId",
                table: "InvoiceItems",
                column: "ContractAddOnId");

            migrationBuilder.CreateIndex(
                name: "IX_InvoiceItems_UnitServiceId",
                table: "InvoiceItems",
                column: "UnitServiceId");

            migrationBuilder.AddForeignKey(
                name: "FK_InvoiceItems_ContractAddOns_ContractAddOnId",
                table: "InvoiceItems",
                column: "ContractAddOnId",
                principalTable: "ContractAddOns",
                principalColumn: "Id",
                onDelete: ReferentialAction.SetNull);

            migrationBuilder.AddForeignKey(
                name: "FK_InvoiceItems_UnitServices_UnitServiceId",
                table: "InvoiceItems",
                column: "UnitServiceId",
                principalTable: "UnitServices",
                principalColumn: "Id",
                onDelete: ReferentialAction.SetNull);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_InvoiceItems_ContractAddOns_ContractAddOnId",
                table: "InvoiceItems");

            migrationBuilder.DropForeignKey(
                name: "FK_InvoiceItems_UnitServices_UnitServiceId",
                table: "InvoiceItems");

            migrationBuilder.DropIndex(
                name: "IX_InvoiceItems_ContractAddOnId",
                table: "InvoiceItems");

            migrationBuilder.DropIndex(
                name: "IX_InvoiceItems_UnitServiceId",
                table: "InvoiceItems");
        }
    }
}
