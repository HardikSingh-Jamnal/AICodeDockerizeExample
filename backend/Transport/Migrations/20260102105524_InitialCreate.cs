using System;
using Microsoft.EntityFrameworkCore.Migrations;
using Npgsql.EntityFrameworkCore.PostgreSQL.Metadata;

#nullable disable

#pragma warning disable CA1814 // Prefer jagged arrays over multidimensional

namespace Transport.Migrations
{
    /// <inheritdoc />
    public partial class InitialCreate : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "Transports",
                columns: table => new
                {
                    TransportId = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    CarrierId = table.Column<int>(type: "integer", nullable: false),
                    PurchaseId = table.Column<int>(type: "integer", nullable: false),
                    PickupStreet = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: false),
                    PickupCity = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: false),
                    PickupStateCode = table.Column<string>(type: "character varying(10)", maxLength: 10, nullable: false),
                    PickupCountry = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: false),
                    PickupZipCode = table.Column<string>(type: "character varying(20)", maxLength: 20, nullable: false),
                    DeliveryStreet = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: false),
                    DeliveryCity = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: false),
                    DeliveryStateCode = table.Column<string>(type: "character varying(10)", maxLength: 10, nullable: false),
                    DeliveryCountry = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: false),
                    DeliveryZipCode = table.Column<string>(type: "character varying(20)", maxLength: 20, nullable: false),
                    ScheduleDate = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    Status = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false, defaultValueSql: "now() at time zone 'utc'"),
                    UpdatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    Notes = table.Column<string>(type: "character varying(1000)", maxLength: 1000, nullable: false),
                    EstimatedCost = table.Column<decimal>(type: "numeric(18,2)", precision: 18, scale: 2, nullable: true),
                    ActualCost = table.Column<decimal>(type: "numeric(18,2)", precision: 18, scale: 2, nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Transports", x => x.TransportId);
                });

            migrationBuilder.InsertData(
                table: "Transports",
                columns: new[] { "TransportId", "ActualCost", "CarrierId", "CreatedAt", "DeliveryCity", "DeliveryCountry", "DeliveryStateCode", "DeliveryStreet", "DeliveryZipCode", "EstimatedCost", "Notes", "PickupCity", "PickupCountry", "PickupStateCode", "PickupStreet", "PickupZipCode", "PurchaseId", "ScheduleDate", "Status", "UpdatedAt" },
                values: new object[,]
                {
                    { 1, null, 101, new DateTime(2026, 1, 2, 10, 55, 23, 643, DateTimeKind.Utc).AddTicks(4548), "Los Angeles", "USA", "CA", "456 Oak Ave", "90001", 150.00m, "Handle with care - fragile items", "New York", "USA", "NY", "123 Main St, Warehouse A", "10001", 1001, new DateTime(2026, 1, 3, 10, 55, 23, 643, DateTimeKind.Utc).AddTicks(5706), "Pending", null },
                    { 2, 215.75m, 102, new DateTime(2026, 1, 2, 10, 55, 23, 643, DateTimeKind.Utc).AddTicks(6340), "Houston", "USA", "TX", "321 Commerce St, Retail Store", "77001", 220.50m, "Express delivery required", "Chicago", "USA", "IL", "789 Industrial Blvd, Distribution Center B", "60601", 1002, new DateTime(2026, 1, 4, 10, 55, 23, 643, DateTimeKind.Utc).AddTicks(6343), "InTransit", null },
                    { 3, 175.25m, 103, new DateTime(2026, 1, 2, 10, 55, 23, 643, DateTimeKind.Utc).AddTicks(6430), "Seattle", "USA", "WA", "888 Business Park Dr, Corporate Office", "98101", 180.00m, "Delivery completed successfully", "Detroit", "USA", "MI", "555 Factory Rd, Manufacturing Plant", "48201", 1003, new DateTime(2026, 1, 1, 10, 55, 23, 643, DateTimeKind.Utc).AddTicks(6432), "Delivered", null }
                });

            migrationBuilder.CreateIndex(
                name: "IX_Transports_CarrierId",
                table: "Transports",
                column: "CarrierId");

            migrationBuilder.CreateIndex(
                name: "IX_Transports_DeliveryCity",
                table: "Transports",
                column: "DeliveryCity");

            migrationBuilder.CreateIndex(
                name: "IX_Transports_PickupCity",
                table: "Transports",
                column: "PickupCity");

            migrationBuilder.CreateIndex(
                name: "IX_Transports_PurchaseId",
                table: "Transports",
                column: "PurchaseId");

            migrationBuilder.CreateIndex(
                name: "IX_Transports_ScheduleDate",
                table: "Transports",
                column: "ScheduleDate");

            migrationBuilder.CreateIndex(
                name: "IX_Transports_Status",
                table: "Transports",
                column: "Status");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "Transports");
        }
    }
}
