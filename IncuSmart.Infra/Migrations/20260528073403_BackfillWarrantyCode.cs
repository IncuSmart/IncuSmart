using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace IncuSmart.Infra.Migrations
{
    /// <inheritdoc />
    public partial class BackfillWarrantyCode : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.Sql(@"
                UPDATE warranties
                SET warranty_code = 'BH-' || TO_CHAR(created_at AT TIME ZONE 'UTC', 'YYYYMMDD') || '-' || LPAD((FLOOR(RANDOM() * 9000) + 1000)::text, 4, '0')
                WHERE warranty_code IS NULL;
            ");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
        }
    }
}
