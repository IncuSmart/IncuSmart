using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace IncuSmart.Infra.Migrations
{
    /// <inheritdoc />
    public partial class AddMaintenanceConfigItemsAndPayment : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            // Add payment columns to maintenance_tickets
            migrationBuilder.AddColumn<long>(
                name: "total_amount",
                table: "maintenance_tickets",
                type: "bigint",
                nullable: false,
                defaultValue: 0L);

            migrationBuilder.AddColumn<string>(
                name: "payment_status",
                table: "maintenance_tickets",
                type: "text",
                nullable: true);

            migrationBuilder.AddColumn<long>(
                name: "payment_order_code",
                table: "maintenance_tickets",
                type: "bigint",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "payment_link_id",
                table: "maintenance_tickets",
                type: "text",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "qr_code",
                table: "maintenance_tickets",
                type: "text",
                nullable: true);

            migrationBuilder.AddColumn<DateTime>(
                name: "payment_link_created_at",
                table: "maintenance_tickets",
                type: "timestamp with time zone",
                nullable: true);

            migrationBuilder.AddColumn<DateTime>(
                name: "payment_link_expired_at",
                table: "maintenance_tickets",
                type: "timestamp with time zone",
                nullable: true);

            migrationBuilder.AddColumn<DateTime>(
                name: "paid_at",
                table: "maintenance_tickets",
                type: "timestamp with time zone",
                nullable: true);

            // Create maintenance_ticket_config_items table
            migrationBuilder.CreateTable(
                name: "maintenance_ticket_config_items",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    ticket_id = table.Column<Guid>(type: "uuid", nullable: false),
                    config_id = table.Column<Guid>(type: "uuid", nullable: false),
                    condition = table.Column<string>(type: "text", nullable: false),
                    market_price = table.Column<long>(type: "bigint", nullable: false),
                    final_price = table.Column<long>(type: "bigint", nullable: false),
                    note = table.Column<string>(type: "text", nullable: true),
                    status = table.Column<string>(type: "text", nullable: false),
                    created_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    created_by = table.Column<string>(type: "text", nullable: true),
                    updated_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    updated_by = table.Column<string>(type: "text", nullable: true),
                    deleted_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    deleted_by = table.Column<string>(type: "text", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_maintenance_ticket_config_items", x => x.id);
                    table.ForeignKey(
                        name: "fk_maintenance_ticket_config_items_configs_config_id",
                        column: x => x.config_id,
                        principalTable: "configs",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "fk_maintenance_ticket_config_items_maintenance_tickets_ticket_",
                        column: x => x.ticket_id,
                        principalTable: "maintenance_tickets",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "ix_maintenance_ticket_config_items_config_id",
                table: "maintenance_ticket_config_items",
                column: "config_id");

            migrationBuilder.CreateIndex(
                name: "ix_maintenance_ticket_config_items_ticket_id",
                table: "maintenance_ticket_config_items",
                column: "ticket_id");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "maintenance_ticket_config_items");

            migrationBuilder.DropColumn(
                name: "total_amount",
                table: "maintenance_tickets");

            migrationBuilder.DropColumn(
                name: "payment_status",
                table: "maintenance_tickets");

            migrationBuilder.DropColumn(
                name: "payment_order_code",
                table: "maintenance_tickets");

            migrationBuilder.DropColumn(
                name: "payment_link_id",
                table: "maintenance_tickets");

            migrationBuilder.DropColumn(
                name: "qr_code",
                table: "maintenance_tickets");

            migrationBuilder.DropColumn(
                name: "payment_link_created_at",
                table: "maintenance_tickets");

            migrationBuilder.DropColumn(
                name: "payment_link_expired_at",
                table: "maintenance_tickets");

            migrationBuilder.DropColumn(
                name: "paid_at",
                table: "maintenance_tickets");
        }
    }
}
