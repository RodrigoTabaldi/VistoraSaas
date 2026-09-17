using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Vistora.Infrastructure.Persistence.PostgreSql.Migrations
{
    /// <inheritdoc />
    public partial class InitialPostgreSql : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.EnsureSchema(
                name: "vistora");

            migrationBuilder.CreateTable(
                name: "audit_events",
                schema: "vistora",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    organization_id = table.Column<Guid>(type: "uuid", nullable: false),
                    ActorUserId = table.Column<Guid>(type: "uuid", nullable: true),
                    EventType = table.Column<string>(type: "character varying(128)", maxLength: 128, nullable: false),
                    EntityType = table.Column<string>(type: "character varying(128)", maxLength: 128, nullable: false),
                    EntityId = table.Column<string>(type: "character varying(128)", maxLength: 128, nullable: false),
                    CorrelationId = table.Column<string>(type: "character varying(128)", maxLength: 128, nullable: true),
                    OccurredAtUtc = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_audit_events", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "checklist_templates",
                schema: "vistora",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    organization_id = table.Column<Guid>(type: "uuid", nullable: false),
                    Name = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: false),
                    IsActive = table.Column<bool>(type: "boolean", nullable: false),
                    CreatedAtUtc = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_checklist_templates", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "organizations",
                schema: "vistora",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    Name = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: false),
                    CreatedAtUtc = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_organizations", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "properties",
                schema: "vistora",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    organization_id = table.Column<Guid>(type: "uuid", nullable: false),
                    Name = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: false),
                    Address = table.Column<string>(type: "character varying(1000)", maxLength: 1000, nullable: false),
                    CreatedAtUtc = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_properties", x => x.Id);
                    table.ForeignKey(
                        name: "FK_properties_organizations_organization_id",
                        column: x => x.organization_id,
                        principalSchema: "vistora",
                        principalTable: "organizations",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "users",
                schema: "vistora",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    organization_id = table.Column<Guid>(type: "uuid", nullable: false),
                    Email = table.Column<string>(type: "character varying(320)", maxLength: 320, nullable: false),
                    Role = table.Column<string>(type: "character varying(64)", maxLength: 64, nullable: false),
                    CreatedAtUtc = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_users", x => x.Id);
                    table.ForeignKey(
                        name: "FK_users_organizations_organization_id",
                        column: x => x.organization_id,
                        principalSchema: "vistora",
                        principalTable: "organizations",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "units",
                schema: "vistora",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    organization_id = table.Column<Guid>(type: "uuid", nullable: false),
                    PropertyId = table.Column<Guid>(type: "uuid", nullable: false),
                    Identifier = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_units", x => x.Id);
                    table.ForeignKey(
                        name: "FK_units_properties_PropertyId",
                        column: x => x.PropertyId,
                        principalSchema: "vistora",
                        principalTable: "properties",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "inspections",
                schema: "vistora",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    organization_id = table.Column<Guid>(type: "uuid", nullable: false),
                    UnitId = table.Column<Guid>(type: "uuid", nullable: false),
                    ChecklistTemplateId = table.Column<Guid>(type: "uuid", nullable: true),
                    Status = table.Column<string>(type: "character varying(32)", maxLength: 32, nullable: false),
                    CreatedAtUtc = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    CompletedAtUtc = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_inspections", x => x.Id);
                    table.ForeignKey(
                        name: "FK_inspections_checklist_templates_ChecklistTemplateId",
                        column: x => x.ChecklistTemplateId,
                        principalSchema: "vistora",
                        principalTable: "checklist_templates",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.SetNull);
                    table.ForeignKey(
                        name: "FK_inspections_units_UnitId",
                        column: x => x.UnitId,
                        principalSchema: "vistora",
                        principalTable: "units",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "inspection_rooms",
                schema: "vistora",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    organization_id = table.Column<Guid>(type: "uuid", nullable: false),
                    InspectionId = table.Column<Guid>(type: "uuid", nullable: false),
                    Name = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: false),
                    Position = table.Column<int>(type: "integer", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_inspection_rooms", x => x.Id);
                    table.ForeignKey(
                        name: "FK_inspection_rooms_inspections_InspectionId",
                        column: x => x.InspectionId,
                        principalSchema: "vistora",
                        principalTable: "inspections",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "reports",
                schema: "vistora",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    organization_id = table.Column<Guid>(type: "uuid", nullable: false),
                    InspectionId = table.Column<Guid>(type: "uuid", nullable: false),
                    Version = table.Column<int>(type: "integer", nullable: false),
                    ObjectKey = table.Column<string>(type: "character varying(1024)", maxLength: 1024, nullable: false),
                    Sha256 = table.Column<string>(type: "character(64)", fixedLength: true, maxLength: 64, nullable: false),
                    CreatedAtUtc = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    ApprovedAtUtc = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_reports", x => x.Id);
                    table.ForeignKey(
                        name: "FK_reports_inspections_InspectionId",
                        column: x => x.InspectionId,
                        principalSchema: "vistora",
                        principalTable: "inspections",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "inspection_items",
                schema: "vistora",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    organization_id = table.Column<Guid>(type: "uuid", nullable: false),
                    InspectionRoomId = table.Column<Guid>(type: "uuid", nullable: false),
                    Description = table.Column<string>(type: "character varying(1000)", maxLength: 1000, nullable: false),
                    Response = table.Column<string>(type: "character varying(2000)", maxLength: 2000, nullable: true),
                    Notes = table.Column<string>(type: "character varying(4000)", maxLength: 4000, nullable: true),
                    Position = table.Column<int>(type: "integer", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_inspection_items", x => x.Id);
                    table.ForeignKey(
                        name: "FK_inspection_items_inspection_rooms_InspectionRoomId",
                        column: x => x.InspectionRoomId,
                        principalSchema: "vistora",
                        principalTable: "inspection_rooms",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "evidence",
                schema: "vistora",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    organization_id = table.Column<Guid>(type: "uuid", nullable: false),
                    InspectionItemId = table.Column<Guid>(type: "uuid", nullable: false),
                    ObjectKey = table.Column<string>(type: "character varying(1024)", maxLength: 1024, nullable: false),
                    FileName = table.Column<string>(type: "character varying(512)", maxLength: 512, nullable: false),
                    ContentType = table.Column<string>(type: "character varying(255)", maxLength: 255, nullable: false),
                    SizeBytes = table.Column<long>(type: "bigint", nullable: false),
                    Sha256 = table.Column<string>(type: "character(64)", fixedLength: true, maxLength: 64, nullable: false),
                    CreatedAtUtc = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_evidence", x => x.Id);
                    table.ForeignKey(
                        name: "FK_evidence_inspection_items_InspectionItemId",
                        column: x => x.InspectionItemId,
                        principalSchema: "vistora",
                        principalTable: "inspection_items",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateIndex(
                name: "IX_audit_events_organization_id",
                schema: "vistora",
                table: "audit_events",
                column: "organization_id");

            migrationBuilder.CreateIndex(
                name: "IX_audit_events_organization_id_OccurredAtUtc",
                schema: "vistora",
                table: "audit_events",
                columns: new[] { "organization_id", "OccurredAtUtc" });

            migrationBuilder.CreateIndex(
                name: "IX_checklist_templates_organization_id",
                schema: "vistora",
                table: "checklist_templates",
                column: "organization_id");

            migrationBuilder.CreateIndex(
                name: "IX_evidence_InspectionItemId",
                schema: "vistora",
                table: "evidence",
                column: "InspectionItemId");

            migrationBuilder.CreateIndex(
                name: "IX_evidence_organization_id",
                schema: "vistora",
                table: "evidence",
                column: "organization_id");

            migrationBuilder.CreateIndex(
                name: "IX_evidence_organization_id_ObjectKey",
                schema: "vistora",
                table: "evidence",
                columns: new[] { "organization_id", "ObjectKey" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_inspection_items_InspectionRoomId",
                schema: "vistora",
                table: "inspection_items",
                column: "InspectionRoomId");

            migrationBuilder.CreateIndex(
                name: "IX_inspection_items_organization_id",
                schema: "vistora",
                table: "inspection_items",
                column: "organization_id");

            migrationBuilder.CreateIndex(
                name: "IX_inspection_items_organization_id_InspectionRoomId_Position",
                schema: "vistora",
                table: "inspection_items",
                columns: new[] { "organization_id", "InspectionRoomId", "Position" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_inspection_rooms_InspectionId",
                schema: "vistora",
                table: "inspection_rooms",
                column: "InspectionId");

            migrationBuilder.CreateIndex(
                name: "IX_inspection_rooms_organization_id",
                schema: "vistora",
                table: "inspection_rooms",
                column: "organization_id");

            migrationBuilder.CreateIndex(
                name: "IX_inspection_rooms_organization_id_InspectionId_Position",
                schema: "vistora",
                table: "inspection_rooms",
                columns: new[] { "organization_id", "InspectionId", "Position" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_inspections_ChecklistTemplateId",
                schema: "vistora",
                table: "inspections",
                column: "ChecklistTemplateId");

            migrationBuilder.CreateIndex(
                name: "IX_inspections_organization_id",
                schema: "vistora",
                table: "inspections",
                column: "organization_id");

            migrationBuilder.CreateIndex(
                name: "IX_inspections_organization_id_UnitId_Status",
                schema: "vistora",
                table: "inspections",
                columns: new[] { "organization_id", "UnitId", "Status" });

            migrationBuilder.CreateIndex(
                name: "IX_inspections_UnitId",
                schema: "vistora",
                table: "inspections",
                column: "UnitId");

            migrationBuilder.CreateIndex(
                name: "IX_properties_organization_id",
                schema: "vistora",
                table: "properties",
                column: "organization_id");

            migrationBuilder.CreateIndex(
                name: "IX_reports_InspectionId",
                schema: "vistora",
                table: "reports",
                column: "InspectionId");

            migrationBuilder.CreateIndex(
                name: "IX_reports_organization_id",
                schema: "vistora",
                table: "reports",
                column: "organization_id");

            migrationBuilder.CreateIndex(
                name: "IX_reports_organization_id_InspectionId_Version",
                schema: "vistora",
                table: "reports",
                columns: new[] { "organization_id", "InspectionId", "Version" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_units_organization_id",
                schema: "vistora",
                table: "units",
                column: "organization_id");

            migrationBuilder.CreateIndex(
                name: "IX_units_organization_id_PropertyId_Identifier",
                schema: "vistora",
                table: "units",
                columns: new[] { "organization_id", "PropertyId", "Identifier" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_units_PropertyId",
                schema: "vistora",
                table: "units",
                column: "PropertyId");

            migrationBuilder.CreateIndex(
                name: "IX_users_organization_id",
                schema: "vistora",
                table: "users",
                column: "organization_id");

            migrationBuilder.CreateIndex(
                name: "IX_users_organization_id_Email",
                schema: "vistora",
                table: "users",
                columns: new[] { "organization_id", "Email" },
                unique: true);

            migrationBuilder.Sql("""
                CREATE FUNCTION vistora.current_organization_id()
                RETURNS uuid
                LANGUAGE sql
                STABLE
                AS $$ SELECT NULLIF(current_setting('app.organization_id', true), '')::uuid; $$;

                DO $$
                DECLARE table_name text;
                BEGIN
                  FOREACH table_name IN ARRAY ARRAY[
                    'audit_events', 'checklist_templates', 'evidence', 'inspection_items',
                    'inspection_rooms', 'inspections', 'properties', 'reports', 'units', 'users'
                  ]
                  LOOP
                    EXECUTE format('ALTER TABLE vistora.%I ENABLE ROW LEVEL SECURITY', table_name);
                    EXECUTE format('ALTER TABLE vistora.%I FORCE ROW LEVEL SECURITY', table_name);
                    EXECUTE format(
                      'CREATE POLICY tenant_isolation ON vistora.%I USING (organization_id = vistora.current_organization_id()) WITH CHECK (organization_id = vistora.current_organization_id())',
                      table_name);
                  END LOOP;
                END $$;

                CREATE FUNCTION vistora.reject_audit_event_mutation()
                RETURNS trigger
                LANGUAGE plpgsql
                AS $$ BEGIN RAISE EXCEPTION 'Audit events are append-only'; END; $$;

                CREATE TRIGGER audit_events_append_only
                BEFORE UPDATE OR DELETE ON vistora.audit_events
                FOR EACH ROW EXECUTE FUNCTION vistora.reject_audit_event_mutation();
                """);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.Sql("""
                DROP TRIGGER IF EXISTS audit_events_append_only ON vistora.audit_events;
                DROP FUNCTION IF EXISTS vistora.reject_audit_event_mutation();
                DROP FUNCTION IF EXISTS vistora.current_organization_id();
                """);

            migrationBuilder.DropTable(
                name: "audit_events",
                schema: "vistora");

            migrationBuilder.DropTable(
                name: "evidence",
                schema: "vistora");

            migrationBuilder.DropTable(
                name: "reports",
                schema: "vistora");

            migrationBuilder.DropTable(
                name: "users",
                schema: "vistora");

            migrationBuilder.DropTable(
                name: "inspection_items",
                schema: "vistora");

            migrationBuilder.DropTable(
                name: "inspection_rooms",
                schema: "vistora");

            migrationBuilder.DropTable(
                name: "inspections",
                schema: "vistora");

            migrationBuilder.DropTable(
                name: "checklist_templates",
                schema: "vistora");

            migrationBuilder.DropTable(
                name: "units",
                schema: "vistora");

            migrationBuilder.DropTable(
                name: "properties",
                schema: "vistora");

            migrationBuilder.DropTable(
                name: "organizations",
                schema: "vistora");
        }
    }
}
