using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Transport.Migrations
{
    /// <inheritdoc />
    public partial class search : Migration
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
                    ProcessedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    RetryCount = table.Column<int>(type: "integer", nullable: false, defaultValue: 0),
                    LastError = table.Column<string>(type: "text", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_outbox_messages", x => x.Id);
                });

            migrationBuilder.UpdateData(
                table: "Transports",
                keyColumn: "TransportId",
                keyValue: 1,
                columns: new[] { "CreatedAt", "ScheduleDate" },
                values: new object[] { new DateTime(2026, 1, 2, 14, 15, 20, 169, DateTimeKind.Utc).AddTicks(6659), new DateTime(2026, 1, 3, 14, 15, 20, 169, DateTimeKind.Utc).AddTicks(6663) });

            migrationBuilder.UpdateData(
                table: "Transports",
                keyColumn: "TransportId",
                keyValue: 2,
                columns: new[] { "CreatedAt", "ScheduleDate" },
                values: new object[] { new DateTime(2026, 1, 2, 14, 15, 20, 169, DateTimeKind.Utc).AddTicks(6671), new DateTime(2026, 1, 4, 14, 15, 20, 169, DateTimeKind.Utc).AddTicks(6673) });

            migrationBuilder.UpdateData(
                table: "Transports",
                keyColumn: "TransportId",
                keyValue: 3,
                columns: new[] { "CreatedAt", "ScheduleDate" },
                values: new object[] { new DateTime(2026, 1, 2, 14, 15, 20, 169, DateTimeKind.Utc).AddTicks(6675), new DateTime(2026, 1, 1, 14, 15, 20, 169, DateTimeKind.Utc).AddTicks(6677) });

            migrationBuilder.CreateIndex(
                name: "idx_outbox_unprocessed",
                table: "outbox_messages",
                column: "CreatedAt",
                filter: "\"ProcessedAt\" IS NULL");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "outbox_messages");

            migrationBuilder.UpdateData(
                table: "Transports",
                keyColumn: "TransportId",
                keyValue: 1,
                columns: new[] { "CreatedAt", "ScheduleDate" },
                values: new object[] { new DateTime(2026, 1, 2, 10, 55, 23, 643, DateTimeKind.Utc).AddTicks(4548), new DateTime(2026, 1, 3, 10, 55, 23, 643, DateTimeKind.Utc).AddTicks(5706) });

            migrationBuilder.UpdateData(
                table: "Transports",
                keyColumn: "TransportId",
                keyValue: 2,
                columns: new[] { "CreatedAt", "ScheduleDate" },
                values: new object[] { new DateTime(2026, 1, 2, 10, 55, 23, 643, DateTimeKind.Utc).AddTicks(6340), new DateTime(2026, 1, 4, 10, 55, 23, 643, DateTimeKind.Utc).AddTicks(6343) });

            migrationBuilder.UpdateData(
                table: "Transports",
                keyColumn: "TransportId",
                keyValue: 3,
                columns: new[] { "CreatedAt", "ScheduleDate" },
                values: new object[] { new DateTime(2026, 1, 2, 10, 55, 23, 643, DateTimeKind.Utc).AddTicks(6430), new DateTime(2026, 1, 1, 10, 55, 23, 643, DateTimeKind.Utc).AddTicks(6432) });
        }
    }
}
