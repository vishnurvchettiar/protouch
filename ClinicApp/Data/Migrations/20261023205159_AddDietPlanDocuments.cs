using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace ClinicApp.Data.Migrations
{
    /// <inheritdoc />
    public partial class AddDietPlanDocuments : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "DietPlanDocuments",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "TEXT", nullable: false),
                    ClientUserId = table.Column<string>(type: "TEXT", nullable: false),
                    StoredFileName = table.Column<string>(type: "TEXT", maxLength: 100, nullable: false),
                    OriginalFileName = table.Column<string>(type: "TEXT", maxLength: 180, nullable: false),
                    UploadedAtUtc = table.Column<DateTime>(type: "TEXT", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_DietPlanDocuments", x => x.Id);
                    table.ForeignKey(
                        name: "FK_DietPlanDocuments_AspNetUsers_ClientUserId",
                        column: x => x.ClientUserId,
                        principalTable: "AspNetUsers",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "IX_DietPlanDocuments_ClientUserId",
                table: "DietPlanDocuments",
                column: "ClientUserId",
                unique: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "DietPlanDocuments");
        }
    }
}
