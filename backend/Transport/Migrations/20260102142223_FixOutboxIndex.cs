using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Transport.Migrations
{
    /// <inheritdoc />
    public partial class FixOutboxIndex : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "idx_outbox_unprocessed",
                table: "outbox_messages");

            migrationBuilder.RenameColumn(
                name: "ProcessedAt",
                table: "outbox_messages",
                newName: "processed_at");

            migrationBuilder.UpdateData(
                table: "Transports",
                keyColumn: "TransportId",
                keyValue: 1,
                columns: new[] { "CreatedAt", "ScheduleDate" },
                values: new object[] { new DateTime(2026, 1, 2, 14, 22, 23, 731, DateTimeKind.Utc).AddTicks(9033), new DateTime(2026, 1, 3, 14, 22, 23, 731, DateTimeKind.Utc).AddTicks(9037) });

            migrationBuilder.UpdateData(
                table: "Transports",
                keyColumn: "TransportId",
                keyValue: 2,
                columns: new[] { "CreatedAt", "ScheduleDate" },
                values: new object[] { new DateTime(2026, 1, 2, 14, 22, 23, 731, DateTimeKind.Utc).AddTicks(9046), new DateTime(2026, 1, 4, 14, 22, 23, 731, DateTimeKind.Utc).AddTicks(9048) });

            migrationBuilder.UpdateData(
                table: "Transports",
                keyColumn: "TransportId",
                keyValue: 3,
                columns: new[] { "CreatedAt", "ScheduleDate" },
                values: new object[] { new DateTime(2026, 1, 2, 14, 22, 23, 731, DateTimeKind.Utc).AddTicks(9051), new DateTime(2026, 1, 1, 14, 22, 23, 731, DateTimeKind.Utc).AddTicks(9053) });

            migrationBuilder.CreateIndex(
                name: "idx_outbox_unprocessed",
                table: "outbox_messages",
                column: "CreatedAt",
                filter: "\"processed_at\" IS NULL");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "idx_outbox_unprocessed",
                table: "outbox_messages");

            migrationBuilder.RenameColumn(
                name: "processed_at",
                table: "outbox_messages",
                newName: "ProcessedAt");

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
                filter: "processed_at IS NULL");
        }
    }
}
