using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Vistora.Infrastructure.Persistence.PostgreSql.Migrations
{
    /// <inheritdoc />
    public partial class AddDurableOperations : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "idempotency_records",
                schema: "vistora",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    organization_id = table.Column<Guid>(type: "uuid", nullable: false),
                    KeyHash = table.Column<string>(type: "character varying(64)", maxLength: 64, nullable: false),
                    RequestHash = table.Column<string>(type: "character varying(64)", maxLength: 64, nullable: false),
                    ResultJson = table.Column<string>(type: "text", nullable: false),
                    CreatedAtUtc = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_idempotency_records", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "outbox_messages",
                schema: "vistora",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    organization_id = table.Column<Guid>(type: "uuid", nullable: false),
                    Type = table.Column<string>(type: "character varying(128)", maxLength: 128, nullable: false),
                    Payload = table.Column<string>(type: "text", nullable: false),
                    CreatedAtUtc = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    PublishedAtUtc = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_outbox_messages", x => x.Id);
                });

            migrationBuilder.CreateIndex(
                name: "IX_idempotency_records_organization_id",
                schema: "vistora",
                table: "idempotency_records",
                column: "organization_id");

            migrationBuilder.CreateIndex(
                name: "IX_idempotency_records_organization_id_KeyHash",
                schema: "vistora",
                table: "idempotency_records",
                columns: new[] { "organization_id", "KeyHash" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_outbox_messages_organization_id",
                schema: "vistora",
                table: "outbox_messages",
                column: "organization_id");

            migrationBuilder.CreateIndex(
                name: "IX_outbox_messages_organization_id_PublishedAtUtc_CreatedAtUtc",
                schema: "vistora",
                table: "outbox_messages",
                columns: new[] { "organization_id", "PublishedAtUtc", "CreatedAtUtc" });
            migrationBuilder.Sql("""
                ALTER TABLE vistora.idempotency_records ENABLE ROW LEVEL SECURITY;
                ALTER TABLE vistora.idempotency_records FORCE ROW LEVEL SECURITY;
                CREATE POLICY tenant_isolation ON vistora.idempotency_records
                    USING (organization_id = vistora.current_organization_id())
                    WITH CHECK (organization_id = vistora.current_organization_id());
                ALTER TABLE vistora.outbox_messages ENABLE ROW LEVEL SECURITY;
                ALTER TABLE vistora.outbox_messages FORCE ROW LEVEL SECURITY;
                CREATE POLICY tenant_isolation ON vistora.outbox_messages
                    USING (organization_id = vistora.current_organization_id())
                    WITH CHECK (organization_id = vistora.current_organization_id());
                GRANT SELECT, INSERT, UPDATE, DELETE ON vistora.idempotency_records, vistora.outbox_messages TO vistora_app;
                """);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "idempotency_records",
                schema: "vistora");

            migrationBuilder.DropTable(
                name: "outbox_messages",
                schema: "vistora");
        }
    }
}
