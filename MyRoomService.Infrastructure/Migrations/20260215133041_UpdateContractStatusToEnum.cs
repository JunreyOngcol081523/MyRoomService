using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace MyRoomService.Infrastructure.Migrations
{
    public partial class UpdateContractStatusToEnum : Migration
    {
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            // 1. Manually cast the Status column using the CASE statement
            migrationBuilder.Sql(@"
                ALTER TABLE ""Contracts"" 
                ALTER COLUMN ""Status"" TYPE integer 
                USING (
                    CASE ""Status"" 
                        WHEN 'ACTIVE'     THEN 0 
                        WHEN 'ENDED'      THEN 1 
                        WHEN 'RESERVED'   THEN 2 
                        WHEN 'TERMINATED' THEN 3
                        ELSE 0 
                    END
                );");

            // 2. Finalize the column definition as a non-nullable integer
            migrationBuilder.AlterColumn<int>(
                name: "Status",
                table: "Contracts",
                type: "integer",
                nullable: false,
                defaultValue: 0,
                oldClrType: typeof(string),
                oldType: "text");

            // NOTE: I am NOT adding 'ContractId' here because a contract 
            // shouldn't reference itself unless you are doing sub-contracts.
        }

        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AlterColumn<string>(
                name: "Status",
                table: "Contracts",
                type: "text",
                nullable: false,
                oldClrType: typeof(int),
                oldType: "integer");
        }
    }
}