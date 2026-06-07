using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace IncuSmart.Infra.Migrations
{
    public partial class NormalizeEggTypeToEnum : Migration
    {
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            // Convert Vietnamese/free-text values to enum names
            var tables = new[] { "hatching_seasons", "hatching_season_templates" };
            var mappings = new[]
            {
                ("Gà",       "CHICKEN"),
                ("gà",       "CHICKEN"),
                ("chicken",  "CHICKEN"),
                ("Vịt",      "DUCK"),
                ("vịt",      "DUCK"),
                ("duck",     "DUCK"),
                ("Chim cút", "QUAIL"),
                ("chim cút", "QUAIL"),
                ("quail",    "QUAIL"),
                ("Bồ câu",   "PIGEON"),
                ("bồ câu",   "PIGEON"),
                ("pigeon",   "PIGEON"),
            };

            foreach (var table in tables)
            {
                foreach (var (from, to) in mappings)
                {
                    migrationBuilder.Sql($"UPDATE {table} SET egg_type = '{to}' WHERE egg_type = '{from}';");
                }

                // Null out any remaining unrecognized values
                migrationBuilder.Sql($@"
                    UPDATE {table}
                    SET egg_type = NULL
                    WHERE egg_type IS NOT NULL
                      AND egg_type NOT IN ('CHICKEN', 'DUCK', 'QUAIL', 'PIGEON');
                ");
            }
        }

        protected override void Down(MigrationBuilder migrationBuilder)
        {
            // No-op: cannot recover original free-text values
        }
    }
}
