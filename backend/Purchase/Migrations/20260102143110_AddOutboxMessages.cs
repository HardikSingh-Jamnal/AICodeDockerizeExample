using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Purchase.Migrations
{
    /// <inheritdoc />
    public partial class AddOutboxMessages : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "outbox_messages",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false, defaultValueSql: "gen_random_uuid()"),
                    EventType = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: false),
                    Payload = table.Column<string>(type: "text", nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false, defaultValueSql: "now() at time zone 'utc'"),
                    processed_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    RetryCount = table.Column<int>(type: "integer", nullable: false, defaultValue: 0),
                    LastError = table.Column<string>(type: "text", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_outbox_messages", x => x.Id);
                });

            migrationBuilder.UpdateData(
                table: "Purchases",
                keyColumn: "Id",
                keyValue: 1,
                columns: new[] { "CreatedAt", "PurchaseDate" },
                values: new object[] { new DateTime(2026, 1, 2, 14, 31, 10, 422, DateTimeKind.Utc).AddTicks(5388), new DateTime(2026, 1, 2, 14, 31, 10, 422, DateTimeKind.Utc).AddTicks(5390) });

            migrationBuilder.UpdateData(
                table: "Purchases",
                keyColumn: "Id",
                keyValue: 2,
                columns: new[] { "CreatedAt", "PurchaseDate" },
                values: new object[] { new DateTime(2026, 1, 2, 14, 31, 10, 422, DateTimeKind.Utc).AddTicks(5393), new DateTime(2026, 1, 2, 14, 31, 10, 422, DateTimeKind.Utc).AddTicks(5394) });

            migrationBuilder.UpdateData(
                table: "Purchases",
                keyColumn: "Id",
                keyValue: 3,
                columns: new[] { "CreatedAt", "PurchaseDate" },
                values: new object[] { new DateTime(2026, 1, 2, 14, 31, 10, 422, DateTimeKind.Utc).AddTicks(5395), new DateTime(2026, 1, 2, 14, 31, 10, 422, DateTimeKind.Utc).AddTicks(5396) });

            migrationBuilder.CreateIndex(
                name: "idx_outbox_unprocessed",
                table: "outbox_messages",
                column: "CreatedAt",
                filter: "\"processed_at\" IS NULL");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "outbox_messages");

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
    }
}
