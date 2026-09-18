using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Vistora.Infrastructure.Persistence.PostgreSql.Migrations
{
    /// <inheritdoc />
    public partial class CreateApplicationRole : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.Sql("""
                -- The role every environment's PostgreSQL container starts with (POSTGRES_USER,
                -- e.g. "vistora") is created as a SUPERUSER by the official Postgres Docker image.
                -- PostgreSQL superusers unconditionally bypass row security, including on tables
                -- with FORCE ROW LEVEL SECURITY - there is no policy or grant that changes this.
                -- Running the application itself with that role means the RLS policies created in
                -- the initial migration were never actually being enforced against it.
                --
                -- This role is for the application (API/Worker) at runtime ONLY. Migrations
                -- continue to run as the superuser, since they need DDL privileges (CREATE TABLE,
                -- CREATE POLICY, etc.) that this role deliberately does not have.
                DO $$
                BEGIN
                    IF NOT EXISTS (SELECT 1 FROM pg_roles WHERE rolname = 'vistora_app') THEN
                        CREATE ROLE vistora_app LOGIN PASSWORD 'change-me' NOSUPERUSER NOBYPASSRLS NOCREATEDB NOCREATEROLE;
                    END IF;
                END
                $$;

                GRANT USAGE ON SCHEMA vistora TO vistora_app;

                GRANT SELECT, INSERT, UPDATE, DELETE ON ALL TABLES IN SCHEMA vistora TO vistora_app;

                -- Ensures tables created by future migrations (run as the superuser) are
                -- automatically granted to vistora_app too, so this doesn't need to be repeated
                -- by hand in every subsequent migration that adds a table.
                ALTER DEFAULT PRIVILEGES IN SCHEMA vistora
                    GRANT SELECT, INSERT, UPDATE, DELETE ON TABLES TO vistora_app;

                -- Identity/serial-less here (all PKs are client-generated uuids), but sequences
                -- may exist for other reasons; grant usage defensively so a future one isn't
                -- silently unusable by the app role.
                GRANT USAGE ON ALL SEQUENCES IN SCHEMA vistora TO vistora_app;
                ALTER DEFAULT PRIVILEGES IN SCHEMA vistora
                    GRANT USAGE ON SEQUENCES TO vistora_app;
                """);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.Sql("""
                ALTER DEFAULT PRIVILEGES IN SCHEMA vistora
                    REVOKE USAGE ON SEQUENCES FROM vistora_app;
                ALTER DEFAULT PRIVILEGES IN SCHEMA vistora
                    REVOKE SELECT, INSERT, UPDATE, DELETE ON TABLES FROM vistora_app;

                REVOKE ALL PRIVILEGES ON ALL TABLES IN SCHEMA vistora FROM vistora_app;
                REVOKE ALL PRIVILEGES ON ALL SEQUENCES IN SCHEMA vistora FROM vistora_app;
                REVOKE USAGE ON SCHEMA vistora FROM vistora_app;

                DROP ROLE IF EXISTS vistora_app;
                """);
        }
    }
}