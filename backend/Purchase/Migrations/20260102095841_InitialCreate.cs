using System;
using Microsoft.EntityFrameworkCore.Migrations;
using Npgsql.EntityFrameworkCore.PostgreSQL.Metadata;

#nullable disable

#pragma warning disable CA1814 // Prefer jagged arrays over multidimensional

namespace Purchase.Migrations
{
    /// <inheritdoc />
    public partial class InitialCreate : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "Purchases",
                columns: table => new
                {
                    Id = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    BuyerId = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: false),
                    OfferId = table.Column<int>(type: "integer", nullable: false),
                    PurchaseDate = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    Amount = table.Column<decimal>(type: "numeric(18,2)", precision: 18, scale: 2, nullable: false),
                    Status = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: false),
                    IsActive = table.Column<bool>(type: "boolean", nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false, defaultValueSql: "now() at time zone 'utc'"),
                    UpdatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Purchases", x => x.Id);
                });

            migrationBuilder.InsertData(
                table: "Purchases",
                columns: new[] { "Id", "Amount", "BuyerId", "CreatedAt", "IsActive", "OfferId", "PurchaseDate", "Status", "UpdatedAt" },
                values: new object[,]
                {
                    { 1, 25000.00m, "BUYER001", new DateTime(2026, 1, 2, 9, 58, 41, 163, DateTimeKind.Utc).AddTicks(8183), true, 1, new DateTime(2026, 1, 2, 9, 58, 41, 163, DateTimeKind.Utc).AddTicks(8185), "Completed", null },
                    { 2, 32000.00m, "BUYER002", new DateTime(2026, 1, 2, 9, 58, 41, 163, DateTimeKind.Utc).AddTicks(8188), true, 2, new DateTime(2026, 1, 2, 9, 58, 41, 163, DateTimeKind.Utc).AddTicks(8189), "Pending", null },
                    { 3, 18500.00m, "BUYER001", new DateTime(2026, 1, 2, 9, 58, 41, 163, DateTimeKind.Utc).AddTicks(8190), true, 3, new DateTime(2026, 1, 2, 9, 58, 41, 163, DateTimeKind.Utc).AddTicks(8191), "Processing", null }
                });
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "Purchases");
        }
    }
}
