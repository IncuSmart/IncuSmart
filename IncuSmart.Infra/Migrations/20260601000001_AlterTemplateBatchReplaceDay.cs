using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace IncuSmart.Infra.Migrations
{
    public partial class AlterTemplateBatchReplaceDay : Migration
    {
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.Sql(@"
                ALTER TABLE hatching_season_template_batches
                    ADD COLUMN IF NOT EXISTS number_of_days integer NOT NULL DEFAULT 0;

                ALTER TABLE hatching_season_template_batches
                    DROP COLUMN IF EXISTS day_start;

                ALTER TABLE hatching_season_template_batches
                    DROP COLUMN IF EXISTS day_end;
            ");
        }

        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.Sql(@"
                ALTER TABLE hatching_season_template_batches
                    ADD COLUMN IF NOT EXISTS day_start integer NOT NULL DEFAULT 0;

                ALTER TABLE hatching_season_template_batches
                    ADD COLUMN IF NOT EXISTS day_end integer NOT NULL DEFAULT 0;

                ALTER TABLE hatching_season_template_batches
                    DROP COLUMN IF EXISTS number_of_days;
            ");
        }
    }
}
