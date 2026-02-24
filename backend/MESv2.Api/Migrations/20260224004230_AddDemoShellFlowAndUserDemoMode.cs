using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace MESv2.Api.Migrations
{
    /// <inheritdoc />
    public partial class AddDemoShellFlowAndUserDemoMode : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<bool>(
                name: "DemoMode",
                table: "Users",
                type: "bit",
                nullable: false,
                defaultValue: false);

            migrationBuilder.CreateTable(
                name: "DemoShellFlows",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    PlantId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    ShellNumber = table.Column<int>(type: "int", nullable: false),
                    SerialNumber = table.Column<string>(type: "nvarchar(16)", maxLength: 16, nullable: false),
                    CurrentStage = table.Column<string>(type: "nvarchar(32)", maxLength: 32, nullable: false),
                    CreatedByUserId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    CreatedAtUtc = table.Column<DateTime>(type: "datetime2", nullable: false),
                    StageEnteredAtUtc = table.Column<DateTime>(type: "datetime2", nullable: false),
                    CompletedAtUtc = table.Column<DateTime>(type: "datetime2", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_DemoShellFlows", x => x.Id);
                    table.ForeignKey(
                        name: "FK_DemoShellFlows_Plants_PlantId",
                        column: x => x.PlantId,
                        principalTable: "Plants",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_DemoShellFlows_Users_CreatedByUserId",
                        column: x => x.CreatedByUserId,
                        principalTable: "Users",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateIndex(
                name: "IX_DemoShellFlows_CreatedByUserId",
                table: "DemoShellFlows",
                column: "CreatedByUserId");

            migrationBuilder.CreateIndex(
                name: "IX_DemoShellFlows_PlantId_CurrentStage_ShellNumber",
                table: "DemoShellFlows",
                columns: new[] { "PlantId", "CurrentStage", "ShellNumber" });

            migrationBuilder.CreateIndex(
                name: "IX_DemoShellFlows_PlantId_ShellNumber",
                table: "DemoShellFlows",
                columns: new[] { "PlantId", "ShellNumber" },
                unique: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "DemoShellFlows");

            migrationBuilder.DropColumn(
                name: "DemoMode",
                table: "Users");
        }
    }
}
