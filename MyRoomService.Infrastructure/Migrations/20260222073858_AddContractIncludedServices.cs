using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace MyRoomService.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class AddContractIncludedServices : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            //migrationBuilder.DropForeignKey(
            //    name: "FK_UnitServices_ChargeDefinitions_ChargeDefinitionId",
            //    table: "UnitServices");

            //migrationBuilder.DropIndex(
            //    name: "IX_UnitServices_ChargeDefinitionId",
            //    table: "UnitServices");

            //migrationBuilder.DropColumn(
            //    name: "ChargeDefinitionId",
            //    table: "UnitServices");

            migrationBuilder.CreateTable(
                name: "ContractIncludedService",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    TenantId = table.Column<Guid>(type: "uuid", nullable: false),
                    ContractId = table.Column<Guid>(type: "uuid", nullable: false),
                    Name = table.Column<string>(type: "text", nullable: false),
                    Amount = table.Column<decimal>(type: "numeric(10,2)", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_ContractIncludedService", x => x.Id);
                    table.ForeignKey(
                        name: "FK_ContractIncludedService_Contracts_ContractId",
                        column: x => x.ContractId,
                        principalTable: "Contracts",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "IX_ContractIncludedService_ContractId",
                table: "ContractIncludedService",
                column: "ContractId");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "ContractIncludedService");

            migrationBuilder.AddColumn<Guid>(
                name: "ChargeDefinitionId",
                table: "UnitServices",
                type: "uuid",
                nullable: false,
                defaultValue: new Guid("00000000-0000-0000-0000-000000000000"));

            migrationBuilder.CreateIndex(
                name: "IX_UnitServices_ChargeDefinitionId",
                table: "UnitServices",
                column: "ChargeDefinitionId");

            migrationBuilder.AddForeignKey(
                name: "FK_UnitServices_ChargeDefinitions_ChargeDefinitionId",
                table: "UnitServices",
                column: "ChargeDefinitionId",
                principalTable: "ChargeDefinitions",
                principalColumn: "Id",
                onDelete: ReferentialAction.Cascade);
        }
    }
}
