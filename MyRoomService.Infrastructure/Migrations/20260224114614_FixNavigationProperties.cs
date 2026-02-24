using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace MyRoomService.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class FixNavigationProperties : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_ContractAddOns_ChargeDefinitions_ChargeDefinitionId",
                table: "ContractAddOns");

            migrationBuilder.DropForeignKey(
                name: "FK_ContractIncludedService_Contracts_ContractId",
                table: "ContractIncludedService");

            migrationBuilder.DropForeignKey(
                name: "FK_Contracts_Occupants_OccupantId",
                table: "Contracts");

            migrationBuilder.DropForeignKey(
                name: "FK_Contracts_Units_UnitId",
                table: "Contracts");

            migrationBuilder.DropForeignKey(
                name: "FK_Invoices_Contracts_ContractId",
                table: "Invoices");

            migrationBuilder.DropForeignKey(
                name: "FK_Invoices_Occupants_OccupantId",
                table: "Invoices");

            migrationBuilder.DropForeignKey(
                name: "FK_MeterReadings_UnitServices_UnitServiceId",
                table: "MeterReadings");

            migrationBuilder.DropPrimaryKey(
                name: "PK_ContractIncludedService",
                table: "ContractIncludedService");

            migrationBuilder.DropColumn(
                name: "Amount",
                table: "ContractIncludedService");

            migrationBuilder.DropColumn(
                name: "Name",
                table: "ContractIncludedService");

            migrationBuilder.RenameTable(
                name: "ContractIncludedService",
                newName: "ContractIncludedServices");

            migrationBuilder.RenameIndex(
                name: "IX_ContractIncludedService_ContractId",
                table: "ContractIncludedServices",
                newName: "IX_ContractIncludedServices_ContractId");

            migrationBuilder.AddColumn<decimal>(
                name: "OverrideAmount",
                table: "ContractIncludedServices",
                type: "numeric(10,2)",
                nullable: true);

            migrationBuilder.AddColumn<Guid>(
                name: "UnitServiceId",
                table: "ContractIncludedServices",
                type: "uuid",
                nullable: false,
                defaultValue: new Guid("00000000-0000-0000-0000-000000000000"));

            migrationBuilder.AddPrimaryKey(
                name: "PK_ContractIncludedServices",
                table: "ContractIncludedServices",
                column: "Id");

            migrationBuilder.CreateIndex(
                name: "IX_ContractIncludedServices_UnitServiceId",
                table: "ContractIncludedServices",
                column: "UnitServiceId");

            migrationBuilder.AddForeignKey(
                name: "FK_ContractAddOns_ChargeDefinitions_ChargeDefinitionId",
                table: "ContractAddOns",
                column: "ChargeDefinitionId",
                principalTable: "ChargeDefinitions",
                principalColumn: "Id",
                onDelete: ReferentialAction.Restrict);

            migrationBuilder.AddForeignKey(
                name: "FK_ContractIncludedServices_Contracts_ContractId",
                table: "ContractIncludedServices",
                column: "ContractId",
                principalTable: "Contracts",
                principalColumn: "Id",
                onDelete: ReferentialAction.Cascade);

            migrationBuilder.AddForeignKey(
                name: "FK_ContractIncludedServices_UnitServices_UnitServiceId",
                table: "ContractIncludedServices",
                column: "UnitServiceId",
                principalTable: "UnitServices",
                principalColumn: "Id",
                onDelete: ReferentialAction.Cascade);

            migrationBuilder.AddForeignKey(
                name: "FK_Contracts_Occupants_OccupantId",
                table: "Contracts",
                column: "OccupantId",
                principalTable: "Occupants",
                principalColumn: "Id",
                onDelete: ReferentialAction.Restrict);

            migrationBuilder.AddForeignKey(
                name: "FK_Contracts_Units_UnitId",
                table: "Contracts",
                column: "UnitId",
                principalTable: "Units",
                principalColumn: "Id",
                onDelete: ReferentialAction.Restrict);

            migrationBuilder.AddForeignKey(
                name: "FK_Invoices_Contracts_ContractId",
                table: "Invoices",
                column: "ContractId",
                principalTable: "Contracts",
                principalColumn: "Id",
                onDelete: ReferentialAction.Restrict);

            migrationBuilder.AddForeignKey(
                name: "FK_Invoices_Occupants_OccupantId",
                table: "Invoices",
                column: "OccupantId",
                principalTable: "Occupants",
                principalColumn: "Id",
                onDelete: ReferentialAction.Restrict);

            migrationBuilder.AddForeignKey(
                name: "FK_MeterReadings_UnitServices_UnitServiceId",
                table: "MeterReadings",
                column: "UnitServiceId",
                principalTable: "UnitServices",
                principalColumn: "Id",
                onDelete: ReferentialAction.Restrict);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_ContractAddOns_ChargeDefinitions_ChargeDefinitionId",
                table: "ContractAddOns");

            migrationBuilder.DropForeignKey(
                name: "FK_ContractIncludedServices_Contracts_ContractId",
                table: "ContractIncludedServices");

            migrationBuilder.DropForeignKey(
                name: "FK_ContractIncludedServices_UnitServices_UnitServiceId",
                table: "ContractIncludedServices");

            migrationBuilder.DropForeignKey(
                name: "FK_Contracts_Occupants_OccupantId",
                table: "Contracts");

            migrationBuilder.DropForeignKey(
                name: "FK_Contracts_Units_UnitId",
                table: "Contracts");

            migrationBuilder.DropForeignKey(
                name: "FK_Invoices_Contracts_ContractId",
                table: "Invoices");

            migrationBuilder.DropForeignKey(
                name: "FK_Invoices_Occupants_OccupantId",
                table: "Invoices");

            migrationBuilder.DropForeignKey(
                name: "FK_MeterReadings_UnitServices_UnitServiceId",
                table: "MeterReadings");

            migrationBuilder.DropPrimaryKey(
                name: "PK_ContractIncludedServices",
                table: "ContractIncludedServices");

            migrationBuilder.DropIndex(
                name: "IX_ContractIncludedServices_UnitServiceId",
                table: "ContractIncludedServices");

            migrationBuilder.DropColumn(
                name: "OverrideAmount",
                table: "ContractIncludedServices");

            migrationBuilder.DropColumn(
                name: "UnitServiceId",
                table: "ContractIncludedServices");

            migrationBuilder.RenameTable(
                name: "ContractIncludedServices",
                newName: "ContractIncludedService");

            migrationBuilder.RenameIndex(
                name: "IX_ContractIncludedServices_ContractId",
                table: "ContractIncludedService",
                newName: "IX_ContractIncludedService_ContractId");

            migrationBuilder.AddColumn<decimal>(
                name: "Amount",
                table: "ContractIncludedService",
                type: "numeric(10,2)",
                nullable: false,
                defaultValue: 0m);

            migrationBuilder.AddColumn<string>(
                name: "Name",
                table: "ContractIncludedService",
                type: "text",
                nullable: false,
                defaultValue: "");

            migrationBuilder.AddPrimaryKey(
                name: "PK_ContractIncludedService",
                table: "ContractIncludedService",
                column: "Id");

            migrationBuilder.AddForeignKey(
                name: "FK_ContractAddOns_ChargeDefinitions_ChargeDefinitionId",
                table: "ContractAddOns",
                column: "ChargeDefinitionId",
                principalTable: "ChargeDefinitions",
                principalColumn: "Id",
                onDelete: ReferentialAction.Cascade);

            migrationBuilder.AddForeignKey(
                name: "FK_ContractIncludedService_Contracts_ContractId",
                table: "ContractIncludedService",
                column: "ContractId",
                principalTable: "Contracts",
                principalColumn: "Id",
                onDelete: ReferentialAction.Cascade);

            migrationBuilder.AddForeignKey(
                name: "FK_Contracts_Occupants_OccupantId",
                table: "Contracts",
                column: "OccupantId",
                principalTable: "Occupants",
                principalColumn: "Id",
                onDelete: ReferentialAction.Cascade);

            migrationBuilder.AddForeignKey(
                name: "FK_Contracts_Units_UnitId",
                table: "Contracts",
                column: "UnitId",
                principalTable: "Units",
                principalColumn: "Id",
                onDelete: ReferentialAction.Cascade);

            migrationBuilder.AddForeignKey(
                name: "FK_Invoices_Contracts_ContractId",
                table: "Invoices",
                column: "ContractId",
                principalTable: "Contracts",
                principalColumn: "Id",
                onDelete: ReferentialAction.Cascade);

            migrationBuilder.AddForeignKey(
                name: "FK_Invoices_Occupants_OccupantId",
                table: "Invoices",
                column: "OccupantId",
                principalTable: "Occupants",
                principalColumn: "Id",
                onDelete: ReferentialAction.Cascade);

            migrationBuilder.AddForeignKey(
                name: "FK_MeterReadings_UnitServices_UnitServiceId",
                table: "MeterReadings",
                column: "UnitServiceId",
                principalTable: "UnitServices",
                principalColumn: "Id",
                onDelete: ReferentialAction.Cascade);
        }
    }
}
