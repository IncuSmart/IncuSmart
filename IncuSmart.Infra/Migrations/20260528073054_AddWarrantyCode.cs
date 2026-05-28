using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace IncuSmart.Infra.Migrations
{
    /// <inheritdoc />
    public partial class AddWarrantyCode : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "warranty_code",
                table: "warranties",
                type: "text",
                nullable: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "warranty_code",
                table: "warranties");
        }
    }
}
