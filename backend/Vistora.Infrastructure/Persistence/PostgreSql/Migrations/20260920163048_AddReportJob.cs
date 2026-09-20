using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Vistora.Infrastructure.Persistence.PostgreSql.Migrations
{
    /// <inheritdoc />
    public partial class AddReportJob : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "report_jobs",
                schema: "vistora",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    organization_id = table.Column<Guid>(type: "uuid", nullable: false),
                    InspectionId = table.Column<Guid>(type: "uuid", nullable: false),
                    IdempotencyKey = table.Column<string>(type: "character varying(256)", maxLength: 256, nullable: false),
                    Status = table.Column<string>(type: "character varying(32)", maxLength: 32, nullable: false),
                    Attempts = table.Column<int>(type: "integer", nullable: false),
                    MaxAttempts = table.Column<int>(type: "integer", nullable: false),
                    ErrorMessage = table.Column<string>(type: "text", nullable: true),
                    ResultReportId = table.Column<Guid>(type: "uuid", nullable: true),
                    CreatedAtUtc = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    StartedAtUtc = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true),
                    CompletedAtUtc = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_report_jobs", x => x.Id);
                    table.ForeignKey(
                        name: "FK_report_jobs_inspections_InspectionId",
                        column: x => x.InspectionId,
                        principalSchema: "vistora",
                        principalTable: "inspections",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_report_jobs_reports_ResultReportId",
                        column: x => x.ResultReportId,
                        principalSchema: "vistora",
                        principalTable: "reports",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateIndex(
                name: "IX_report_jobs_InspectionId",
                schema: "vistora",
                table: "report_jobs",
                column: "InspectionId");

            migrationBuilder.CreateIndex(
                name: "IX_report_jobs_organization_id",
                schema: "vistora",
                table: "report_jobs",
                column: "organization_id");

            migrationBuilder.CreateIndex(
                name: "IX_report_jobs_organization_id_IdempotencyKey",
                schema: "vistora",
                table: "report_jobs",
                columns: new[] { "organization_id", "IdempotencyKey" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_report_jobs_organization_id_InspectionId",
                schema: "vistora",
                table: "report_jobs",
                columns: new[] { "organization_id", "InspectionId" },
                unique: true,
                filter: "\"Status\" IN ('Pending', 'Processing')");

            migrationBuilder.CreateIndex(
                name: "IX_report_jobs_ResultReportId",
                schema: "vistora",
                table: "report_jobs",
                column: "ResultReportId");

            migrationBuilder.Sql("""
                ALTER TABLE vistora.report_jobs ENABLE ROW LEVEL SECURITY;
                ALTER TABLE vistora.report_jobs FORCE ROW LEVEL SECURITY;
                CREATE POLICY tenant_isolation ON vistora.report_jobs USING (organization_id = vistora.current_organization_id()) WITH CHECK (organization_id = vistora.current_organization_id());
                """);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "report_jobs",
                schema: "vistora");
        }
    }
}
