using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Vistora.Infrastructure.Persistence.PostgreSql.Migrations
{
    /// <inheritdoc />
    public partial class AddProductWorkflows : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "Document",
                schema: "vistora",
                table: "organizations",
                type: "character varying(32)",
                maxLength: 32,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "OperationalEmail",
                schema: "vistora",
                table: "organizations",
                type: "character varying(320)",
                maxLength: 320,
                nullable: true);

            migrationBuilder.AddColumn<DateTimeOffset>(
                name: "ScheduledAtUtc",
                schema: "vistora",
                table: "inspections",
                type: "timestamp with time zone",
                nullable: true);

            migrationBuilder.CreateTable(
                name: "account_invitations",
                schema: "vistora",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    OrganizationId = table.Column<Guid>(type: "uuid", nullable: false),
                    InvitedByUserId = table.Column<Guid>(type: "uuid", nullable: false),
                    Email = table.Column<string>(type: "character varying(320)", maxLength: 320, nullable: false),
                    NormalizedEmail = table.Column<string>(type: "character varying(320)", maxLength: 320, nullable: false),
                    Role = table.Column<string>(type: "character varying(64)", maxLength: 64, nullable: false),
                    TokenHash = table.Column<string>(type: "character(64)", fixedLength: true, maxLength: 64, nullable: false),
                    CreatedAtUtc = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    ExpiresAtUtc = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    AcceptedAtUtc = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true),
                    RevokedAtUtc = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_account_invitations", x => x.Id);
                    table.ForeignKey(
                        name: "FK_account_invitations_organizations_OrganizationId",
                        column: x => x.OrganizationId,
                        principalSchema: "vistora",
                        principalTable: "organizations",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_account_invitations_users_OrganizationId_InvitedByUserId",
                        columns: x => new { x.OrganizationId, x.InvitedByUserId },
                        principalSchema: "vistora",
                        principalTable: "users",
                        principalColumns: new[] { "organization_id", "Id" },
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "inspection_acceptances",
                schema: "vistora",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    organization_id = table.Column<Guid>(type: "uuid", nullable: false),
                    InspectionId = table.Column<Guid>(type: "uuid", nullable: false),
                    ActorUserId = table.Column<Guid>(type: "uuid", nullable: false),
                    SignerName = table.Column<string>(type: "character varying(150)", maxLength: 150, nullable: false),
                    SignerEmail = table.Column<string>(type: "character varying(320)", maxLength: 320, nullable: false),
                    SignatureObjectKey = table.Column<string>(type: "character varying(1024)", maxLength: 1024, nullable: false),
                    SignatureSha256 = table.Column<string>(type: "character(64)", fixedLength: true, maxLength: 64, nullable: false),
                    TermsVersion = table.Column<string>(type: "character varying(32)", maxLength: 32, nullable: false),
                    AcceptedAtUtc = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_inspection_acceptances", x => x.Id);
                    table.ForeignKey(
                        name: "FK_inspection_acceptances_inspections_InspectionId",
                        column: x => x.InspectionId,
                        principalSchema: "vistora",
                        principalTable: "inspections",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_inspection_acceptances_users_organization_id_ActorUserId",
                        columns: x => new { x.organization_id, x.ActorUserId },
                        principalSchema: "vistora",
                        principalTable: "users",
                        principalColumns: new[] { "organization_id", "Id" },
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.Sql("""
                ALTER TABLE vistora.inspection_acceptances ENABLE ROW LEVEL SECURITY;
                ALTER TABLE vistora.inspection_acceptances FORCE ROW LEVEL SECURITY;
                CREATE POLICY tenant_isolation ON vistora.inspection_acceptances
                    USING (organization_id = vistora.current_organization_id())
                    WITH CHECK (organization_id = vistora.current_organization_id());
                """);

            migrationBuilder.CreateIndex(
                name: "IX_account_invitations_NormalizedEmail",
                schema: "vistora",
                table: "account_invitations",
                column: "NormalizedEmail",
                unique: true,
                filter: "\"AcceptedAtUtc\" IS NULL AND \"RevokedAtUtc\" IS NULL");

            migrationBuilder.CreateIndex(
                name: "IX_account_invitations_OrganizationId_InvitedByUserId",
                schema: "vistora",
                table: "account_invitations",
                columns: new[] { "OrganizationId", "InvitedByUserId" });

            migrationBuilder.CreateIndex(
                name: "IX_account_invitations_TokenHash",
                schema: "vistora",
                table: "account_invitations",
                column: "TokenHash",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_inspection_acceptances_InspectionId",
                schema: "vistora",
                table: "inspection_acceptances",
                column: "InspectionId",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_inspection_acceptances_organization_id",
                schema: "vistora",
                table: "inspection_acceptances",
                column: "organization_id");

            migrationBuilder.CreateIndex(
                name: "IX_inspection_acceptances_organization_id_ActorUserId",
                schema: "vistora",
                table: "inspection_acceptances",
                columns: new[] { "organization_id", "ActorUserId" });

            migrationBuilder.CreateIndex(
                name: "IX_inspection_acceptances_organization_id_InspectionId",
                schema: "vistora",
                table: "inspection_acceptances",
                columns: new[] { "organization_id", "InspectionId" },
                unique: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "account_invitations",
                schema: "vistora");

            migrationBuilder.DropTable(
                name: "inspection_acceptances",
                schema: "vistora");

            migrationBuilder.DropColumn(
                name: "Document",
                schema: "vistora",
                table: "organizations");

            migrationBuilder.DropColumn(
                name: "OperationalEmail",
                schema: "vistora",
                table: "organizations");

            migrationBuilder.DropColumn(
                name: "ScheduledAtUtc",
                schema: "vistora",
                table: "inspections");

        }
    }
}
