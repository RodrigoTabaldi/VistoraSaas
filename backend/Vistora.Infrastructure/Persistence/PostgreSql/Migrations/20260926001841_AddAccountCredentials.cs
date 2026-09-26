using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Vistora.Infrastructure.Persistence.PostgreSql.Migrations
{
    /// <inheritdoc />
    public partial class AddAccountCredentials : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddUniqueConstraint(
                name: "AK_users_organization_id_Id",
                schema: "vistora",
                table: "users",
                columns: new[] { "organization_id", "Id" });

            migrationBuilder.CreateTable(
                name: "accounts",
                schema: "vistora",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    organization_id = table.Column<Guid>(type: "uuid", nullable: false),
                    Name = table.Column<string>(type: "character varying(150)", maxLength: 150, nullable: false),
                    Email = table.Column<string>(type: "character varying(320)", maxLength: 320, nullable: false),
                    NormalizedEmail = table.Column<string>(type: "character varying(320)", maxLength: 320, nullable: false),
                    PasswordHash = table.Column<string>(type: "character varying(512)", maxLength: 512, nullable: false),
                    Role = table.Column<string>(type: "character varying(64)", maxLength: 64, nullable: false),
                    CreatedAtUtc = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_accounts", x => x.Id);
                    table.ForeignKey(
                        name: "FK_accounts_users_organization_id_Id",
                        columns: x => new { x.organization_id, x.Id },
                        principalSchema: "vistora",
                        principalTable: "users",
                        principalColumns: new[] { "organization_id", "Id" },
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "IX_accounts_NormalizedEmail",
                schema: "vistora",
                table: "accounts",
                column: "NormalizedEmail",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_accounts_organization_id_Id",
                schema: "vistora",
                table: "accounts",
                columns: new[] { "organization_id", "Id" },
                unique: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "accounts",
                schema: "vistora");

            migrationBuilder.DropUniqueConstraint(
                name: "AK_users_organization_id_Id",
                schema: "vistora",
                table: "users");
        }
    }
}
