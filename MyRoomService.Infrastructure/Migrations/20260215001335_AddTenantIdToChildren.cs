using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace MyRoomService.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class AddTenantIdToChildren : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<Guid>(
                name: "TenantId",
                table: "InvoiceItems",
                type: "uuid",
                nullable: false,
                defaultValue: new Guid("00000000-0000-0000-0000-000000000000"));

            migrationBuilder.AddColumn<Guid>(
                name: "TenantId",
                table: "ContractAddOns",
                type: "uuid",
                nullable: false,
                defaultValue: new Guid("00000000-0000-0000-0000-000000000000"));
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "TenantId",
                table: "InvoiceItems");

            migrationBuilder.DropColumn(
                name: "TenantId",
                table: "ContractAddOns");
        }
    }
}
