using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Vistora.Infrastructure.Persistence.PostgreSql.Migrations
{
    /// <inheritdoc />
    public partial class AddInspectionTypeRelatedAndRowVersion : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "IX_inspections_organization_id_UnitId_Status",
                schema: "vistora",
                table: "inspections");

            migrationBuilder.AddColumn<Guid>(
                name: "RelatedInspectionId",
                schema: "vistora",
                table: "inspections",
                type: "uuid",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "Type",
                schema: "vistora",
                table: "inspections",
                type: "character varying(32)",
                maxLength: 32,
                nullable: false,
                defaultValue: "");

            migrationBuilder.CreateIndex(
                name: "IX_inspections_organization_id_UnitId_Type",
                schema: "vistora",
                table: "inspections",
                columns: new[] { "organization_id", "UnitId", "Type" },
                unique: true,
                filter: "\"Status\" <> 'Approved'");

            migrationBuilder.CreateIndex(
                name: "IX_inspections_RelatedInspectionId",
                schema: "vistora",
                table: "inspections",
                column: "RelatedInspectionId");

            migrationBuilder.AddForeignKey(
                name: "FK_inspections_inspections_RelatedInspectionId",
                schema: "vistora",
                table: "inspections",
                column: "RelatedInspectionId",
                principalSchema: "vistora",
                principalTable: "inspections",
                principalColumn: "Id",
                onDelete: ReferentialAction.Restrict);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_inspections_inspections_RelatedInspectionId",
                schema: "vistora",
                table: "inspections");

            migrationBuilder.DropIndex(
                name: "IX_inspections_organization_id_UnitId_Type",
                schema: "vistora",
                table: "inspections");

            migrationBuilder.DropIndex(
                name: "IX_inspections_RelatedInspectionId",
                schema: "vistora",
                table: "inspections");

            migrationBuilder.DropColumn(
                name: "RelatedInspectionId",
                schema: "vistora",
                table: "inspections");

            migrationBuilder.DropColumn(
                name: "Type",
                schema: "vistora",
                table: "inspections");

            migrationBuilder.CreateIndex(
                name: "IX_inspections_organization_id_UnitId_Status",
                schema: "vistora",
                table: "inspections",
                columns: new[] { "organization_id", "UnitId", "Status" });
        }
    }
}
