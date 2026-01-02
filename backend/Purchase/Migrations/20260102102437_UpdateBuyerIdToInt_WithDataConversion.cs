using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Purchase.Migrations
{
    /// <inheritdoc />
    public partial class UpdateBuyerIdToInt_WithDataConversion : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            // Step 1: Add a temporary integer column
            migrationBuilder.AddColumn<int>(
                name: "BuyerIdTemp",
                table: "Purchases",
                type: "integer",
                nullable: true);

            // Step 2: Convert existing string data to integers
            migrationBuilder.Sql(@"
                UPDATE ""Purchases"" 
                SET ""BuyerIdTemp"" = CASE 
                    WHEN ""BuyerId"" = 'BUYER001' THEN 1001
                    WHEN ""BuyerId"" = 'BUYER002' THEN 1002
                    ELSE 1000 + CAST(SUBSTRING(""BuyerId"" FROM 6) AS INTEGER)
                END;
            ");

            // Step 3: Drop the old column
            migrationBuilder.DropColumn(
                name: "BuyerId",
                table: "Purchases");

            // Step 4: Rename the temp column to the original name
            migrationBuilder.RenameColumn(
                name: "BuyerIdTemp",
                table: "Purchases",
                newName: "BuyerId");

            // Step 5: Make the column NOT NULL
            migrationBuilder.AlterColumn<int>(
                name: "BuyerId",
                table: "Purchases",
                type: "integer",
                nullable: false,
                oldClrType: typeof(int),
                oldType: "integer",
                oldNullable: true);

            // Step 6: Update seed data timestamps
            migrationBuilder.UpdateData(
                table: "Purchases",
                keyColumn: "Id",
                keyValue: 1,
                columns: new[] { "CreatedAt", "PurchaseDate" },
                values: new object[] { new DateTime(2026, 1, 2, 10, 24, 36, 932, DateTimeKind.Utc).AddTicks(9650), new DateTime(2026, 1, 2, 10, 24, 36, 932, DateTimeKind.Utc).AddTicks(9651) });

            migrationBuilder.UpdateData(
                table: "Purchases",
                keyColumn: "Id",
                keyValue: 2,
                columns: new[] { "CreatedAt", "PurchaseDate" },
                values: new object[] { new DateTime(2026, 1, 2, 10, 24, 36, 932, DateTimeKind.Utc).AddTicks(9656), new DateTime(2026, 1, 2, 10, 24, 36, 932, DateTimeKind.Utc).AddTicks(9656) });

            migrationBuilder.UpdateData(
                table: "Purchases",
                keyColumn: "Id",
                keyValue: 3,
                columns: new[] { "CreatedAt", "PurchaseDate" },
                values: new object[] { new DateTime(2026, 1, 2, 10, 24, 36, 932, DateTimeKind.Utc).AddTicks(9658), new DateTime(2026, 1, 2, 10, 24, 36, 932, DateTimeKind.Utc).AddTicks(9659) });
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            // Step 1: Add a temporary string column
            migrationBuilder.AddColumn<string>(
                name: "BuyerIdTemp",
                table: "Purchases",
                type: "character varying(100)",
                maxLength: 100,
                nullable: true);

            // Step 2: Convert existing integer data back to strings
            migrationBuilder.Sql(@"
                UPDATE ""Purchases"" 
                SET ""BuyerIdTemp"" = CASE 
                    WHEN ""BuyerId"" = 1001 THEN 'BUYER001'
                    WHEN ""BuyerId"" = 1002 THEN 'BUYER002'
                    ELSE 'BUYER' || LPAD((""BuyerId"" - 1000)::text, 3, '0')
                END;
            ");

            // Step 3: Drop the old integer column
            migrationBuilder.DropColumn(
                name: "BuyerId",
                table: "Purchases");

            // Step 4: Rename the temp column to the original name
            migrationBuilder.RenameColumn(
                name: "BuyerIdTemp",
                table: "Purchases",
                newName: "BuyerId");

            // Step 5: Make the column NOT NULL
            migrationBuilder.AlterColumn<string>(
                name: "BuyerId",
                table: "Purchases",
                type: "character varying(100)",
                maxLength: 100,
                nullable: false,
                oldClrType: typeof(string),
                oldType: "character varying(100)",
                oldMaxLength: 100,
                oldNullable: true);

            // Step 6: Update seed data timestamps
            migrationBuilder.UpdateData(
                table: "Purchases",
                keyColumn: "Id",
                keyValue: 1,
                columns: new[] { "CreatedAt", "PurchaseDate" },
                values: new object[] { new DateTime(2026, 1, 2, 9, 58, 41, 163, DateTimeKind.Utc).AddTicks(8183), new DateTime(2026, 1, 2, 9, 58, 41, 163, DateTimeKind.Utc).AddTicks(8185) });

            migrationBuilder.UpdateData(
                table: "Purchases",
                keyColumn: "Id",
                keyValue: 2,
                columns: new[] { "CreatedAt", "PurchaseDate" },
                values: new object[] { new DateTime(2026, 1, 2, 9, 58, 41, 163, DateTimeKind.Utc).AddTicks(8188), new DateTime(2026, 1, 2, 9, 58, 41, 163, DateTimeKind.Utc).AddTicks(8189) });

            migrationBuilder.UpdateData(
                table: "Purchases",
                keyColumn: "Id",
                keyValue: 3,
                columns: new[] { "CreatedAt", "PurchaseDate" },
                values: new object[] { new DateTime(2026, 1, 2, 9, 58, 41, 163, DateTimeKind.Utc).AddTicks(8190), new DateTime(2026, 1, 2, 9, 58, 41, 163, DateTimeKind.Utc).AddTicks(8191) });
        }
    }
}
