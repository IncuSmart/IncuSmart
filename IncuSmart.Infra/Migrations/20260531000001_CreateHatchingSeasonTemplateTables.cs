using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace IncuSmart.Infra.Migrations
{
    /// <inheritdoc />
    public partial class CreateHatchingSeasonTemplateTables : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.Sql(@"
                CREATE TABLE IF NOT EXISTS hatching_season_templates (
                    id uuid NOT NULL,
                    customer_id uuid,
                    name text NOT NULL,
                    description text,
                    total_days integer NOT NULL,
                    egg_type text,
                    is_active boolean NOT NULL,
                    created_by_type text NOT NULL,
                    status text NOT NULL,
                    created_at timestamp with time zone NOT NULL,
                    created_by text,
                    updated_at timestamp with time zone,
                    updated_by text,
                    deleted_at timestamp with time zone,
                    deleted_by text,
                    CONSTRAINT pk_hatching_season_templates PRIMARY KEY (id)
                );

                CREATE TABLE IF NOT EXISTS hatching_season_template_batches (
                    id uuid NOT NULL,
                    template_id uuid NOT NULL,
                    batch_index integer NOT NULL,
                    name text,
                    day_start integer NOT NULL,
                    day_end integer NOT NULL,
                    notes text,
                    status text NOT NULL,
                    created_at timestamp with time zone NOT NULL,
                    created_by text,
                    updated_at timestamp with time zone,
                    updated_by text,
                    deleted_at timestamp with time zone,
                    deleted_by text,
                    CONSTRAINT pk_hatching_season_template_batches PRIMARY KEY (id)
                );

                CREATE TABLE IF NOT EXISTS hatching_season_template_batch_configs (
                    id uuid NOT NULL,
                    template_batch_id uuid NOT NULL,
                    config_id uuid NOT NULL,
                    target_value numeric,
                    min_value numeric,
                    max_value numeric,
                    status text NOT NULL,
                    created_at timestamp with time zone NOT NULL,
                    created_by text,
                    updated_at timestamp with time zone,
                    updated_by text,
                    deleted_at timestamp with time zone,
                    deleted_by text,
                    CONSTRAINT pk_hatching_season_template_batch_configs PRIMARY KEY (id)
                );
            ");

            migrationBuilder.Sql(@"
                DO $$ BEGIN
                    IF NOT EXISTS (
                        SELECT 1 FROM pg_constraint
                        WHERE conname = 'fk_hatching_season_template_batches_hatching_season_templates_template_id'
                    ) THEN
                        ALTER TABLE hatching_season_template_batches
                            ADD CONSTRAINT fk_hatching_season_template_batches_hatching_season_templates_template_id
                            FOREIGN KEY (template_id)
                            REFERENCES hatching_season_templates(id)
                            ON DELETE CASCADE
                            NOT VALID;
                    END IF;
                END $$;

                DO $$ BEGIN
                    IF NOT EXISTS (
                        SELECT 1 FROM pg_constraint
                        WHERE conname = 'fk_hatching_season_template_batch_configs_hatching_season_template_batches_template_batch_id'
                    ) THEN
                        ALTER TABLE hatching_season_template_batch_configs
                            ADD CONSTRAINT fk_hatching_season_template_batch_configs_hatching_season_template_batches_template_batch_id
                            FOREIGN KEY (template_batch_id)
                            REFERENCES hatching_season_template_batches(id)
                            ON DELETE CASCADE
                            NOT VALID;
                    END IF;
                END $$;
            ");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.Sql(@"
                ALTER TABLE IF EXISTS hatching_season_template_batch_configs
                    DROP CONSTRAINT IF EXISTS fk_hatching_season_template_batch_configs_hatching_season_template_batches_template_batch_id;

                ALTER TABLE IF EXISTS hatching_season_template_batches
                    DROP CONSTRAINT IF EXISTS fk_hatching_season_template_batches_hatching_season_templates_template_id;

                DROP TABLE IF EXISTS hatching_season_template_batch_configs;
                DROP TABLE IF EXISTS hatching_season_template_batches;
                DROP TABLE IF EXISTS hatching_season_templates;
            ");
        }
    }
}
