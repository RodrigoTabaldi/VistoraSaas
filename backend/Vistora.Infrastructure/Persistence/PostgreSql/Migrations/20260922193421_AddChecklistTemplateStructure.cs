using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Vistora.Infrastructure.Persistence.PostgreSql.Migrations
{
    /// <inheritdoc />
    public partial class AddChecklistTemplateStructure : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "checklist_template_rooms",
                schema: "vistora",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    organization_id = table.Column<Guid>(type: "uuid", nullable: false),
                    ChecklistTemplateId = table.Column<Guid>(type: "uuid", nullable: false),
                    Name = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: false),
                    Position = table.Column<int>(type: "integer", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_checklist_template_rooms", x => x.Id);
                    table.ForeignKey(
                        name: "FK_checklist_template_rooms_checklist_templates_ChecklistTempl~",
                        column: x => x.ChecklistTemplateId,
                        principalSchema: "vistora",
                        principalTable: "checklist_templates",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "checklist_template_items",
                schema: "vistora",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    organization_id = table.Column<Guid>(type: "uuid", nullable: false),
                    ChecklistTemplateRoomId = table.Column<Guid>(type: "uuid", nullable: false),
                    Description = table.Column<string>(type: "character varying(1000)", maxLength: 1000, nullable: false),
                    Position = table.Column<int>(type: "integer", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_checklist_template_items", x => x.Id);
                    table.ForeignKey(
                        name: "FK_checklist_template_items_checklist_template_rooms_Checklist~",
                        column: x => x.ChecklistTemplateRoomId,
                        principalSchema: "vistora",
                        principalTable: "checklist_template_rooms",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "IX_checklist_template_items_ChecklistTemplateRoomId",
                schema: "vistora",
                table: "checklist_template_items",
                column: "ChecklistTemplateRoomId");

            migrationBuilder.CreateIndex(
                name: "IX_checklist_template_items_organization_id",
                schema: "vistora",
                table: "checklist_template_items",
                column: "organization_id");

            migrationBuilder.CreateIndex(
                name: "IX_checklist_template_items_organization_id_ChecklistTemplateR~",
                schema: "vistora",
                table: "checklist_template_items",
                columns: new[] { "organization_id", "ChecklistTemplateRoomId", "Position" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_checklist_template_rooms_ChecklistTemplateId",
                schema: "vistora",
                table: "checklist_template_rooms",
                column: "ChecklistTemplateId");

            migrationBuilder.CreateIndex(
                name: "IX_checklist_template_rooms_organization_id",
                schema: "vistora",
                table: "checklist_template_rooms",
                column: "organization_id");

            migrationBuilder.CreateIndex(
                name: "IX_checklist_template_rooms_organization_id_ChecklistTemplateI~",
                schema: "vistora",
                table: "checklist_template_rooms",
                columns: new[] { "organization_id", "ChecklistTemplateId", "Position" },
                unique: true);

            migrationBuilder.Sql("""
                ALTER TABLE vistora.checklist_template_rooms ENABLE ROW LEVEL SECURITY;
                ALTER TABLE vistora.checklist_template_rooms FORCE ROW LEVEL SECURITY;
                CREATE POLICY tenant_isolation ON vistora.checklist_template_rooms USING (organization_id = vistora.current_organization_id()) WITH CHECK (organization_id = vistora.current_organization_id());

                ALTER TABLE vistora.checklist_template_items ENABLE ROW LEVEL SECURITY;
                ALTER TABLE vistora.checklist_template_items FORCE ROW LEVEL SECURITY;
                CREATE POLICY tenant_isolation ON vistora.checklist_template_items USING (organization_id = vistora.current_organization_id()) WITH CHECK (organization_id = vistora.current_organization_id());
                """);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "checklist_template_items",
                schema: "vistora");

            migrationBuilder.DropTable(
                name: "checklist_template_rooms",
                schema: "vistora");
        }
    }
}
