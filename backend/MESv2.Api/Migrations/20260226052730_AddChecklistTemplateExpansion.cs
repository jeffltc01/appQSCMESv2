using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace MESv2.Api.Migrations
{
    /// <inheritdoc />
    public partial class AddChecklistTemplateExpansion : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<Guid>(
                name: "OwnerUserId",
                table: "ChecklistTemplates",
                type: "uniqueidentifier",
                nullable: true);

            migrationBuilder.AddColumn<decimal>(
                name: "DimensionLowerLimit",
                table: "ChecklistTemplateItems",
                type: "decimal(18,2)",
                nullable: true);

            migrationBuilder.AddColumn<decimal>(
                name: "DimensionTarget",
                table: "ChecklistTemplateItems",
                type: "decimal(18,2)",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "DimensionUnitOfMeasure",
                table: "ChecklistTemplateItems",
                type: "nvarchar(32)",
                maxLength: 32,
                nullable: true);

            migrationBuilder.AddColumn<decimal>(
                name: "DimensionUpperLimit",
                table: "ChecklistTemplateItems",
                type: "decimal(18,2)",
                nullable: true);

            migrationBuilder.AddColumn<Guid>(
                name: "ScoreTypeId",
                table: "ChecklistTemplateItems",
                type: "uniqueidentifier",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "Section",
                table: "ChecklistTemplateItems",
                type: "nvarchar(200)",
                maxLength: 200,
                nullable: true);

            migrationBuilder.AlterColumn<string>(
                name: "ResponseValue",
                table: "ChecklistEntryItemResponses",
                type: "nvarchar(4000)",
                maxLength: 4000,
                nullable: false,
                oldClrType: typeof(string),
                oldType: "nvarchar(16)",
                oldMaxLength: 16);

            migrationBuilder.CreateTable(
                name: "ScoreTypes",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    Name = table.Column<string>(type: "nvarchar(120)", maxLength: 120, nullable: false),
                    IsActive = table.Column<bool>(type: "bit", nullable: false),
                    CreatedByUserId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    CreatedAtUtc = table.Column<DateTime>(type: "datetime2", nullable: false),
                    ModifiedByUserId = table.Column<Guid>(type: "uniqueidentifier", nullable: true),
                    ModifiedAtUtc = table.Column<DateTime>(type: "datetime2", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_ScoreTypes", x => x.Id);
                    table.ForeignKey(
                        name: "FK_ScoreTypes_Users_CreatedByUserId",
                        column: x => x.CreatedByUserId,
                        principalTable: "Users",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_ScoreTypes_Users_ModifiedByUserId",
                        column: x => x.ModifiedByUserId,
                        principalTable: "Users",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "ScoreTypeValues",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    ScoreTypeId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    Score = table.Column<decimal>(type: "decimal(18,2)", nullable: false),
                    Description = table.Column<string>(type: "nvarchar(240)", maxLength: 240, nullable: false),
                    SortOrder = table.Column<int>(type: "int", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_ScoreTypeValues", x => x.Id);
                    table.ForeignKey(
                        name: "FK_ScoreTypeValues_ScoreTypes_ScoreTypeId",
                        column: x => x.ScoreTypeId,
                        principalTable: "ScoreTypes",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "IX_ChecklistTemplates_OwnerUserId",
                table: "ChecklistTemplates",
                column: "OwnerUserId");

            migrationBuilder.CreateIndex(
                name: "IX_ChecklistTemplateItems_ScoreTypeId",
                table: "ChecklistTemplateItems",
                column: "ScoreTypeId");

            migrationBuilder.CreateIndex(
                name: "IX_ScoreTypes_CreatedByUserId",
                table: "ScoreTypes",
                column: "CreatedByUserId");

            migrationBuilder.CreateIndex(
                name: "IX_ScoreTypes_ModifiedByUserId",
                table: "ScoreTypes",
                column: "ModifiedByUserId");

            migrationBuilder.CreateIndex(
                name: "IX_ScoreTypes_Name",
                table: "ScoreTypes",
                column: "Name");

            migrationBuilder.CreateIndex(
                name: "IX_ScoreTypeValues_ScoreTypeId_Score_Description",
                table: "ScoreTypeValues",
                columns: new[] { "ScoreTypeId", "Score", "Description" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_ScoreTypeValues_ScoreTypeId_SortOrder",
                table: "ScoreTypeValues",
                columns: new[] { "ScoreTypeId", "SortOrder" });

            migrationBuilder.AddForeignKey(
                name: "FK_ChecklistTemplateItems_ScoreTypes_ScoreTypeId",
                table: "ChecklistTemplateItems",
                column: "ScoreTypeId",
                principalTable: "ScoreTypes",
                principalColumn: "Id",
                onDelete: ReferentialAction.Restrict);

            migrationBuilder.AddForeignKey(
                name: "FK_ChecklistTemplates_Users_OwnerUserId",
                table: "ChecklistTemplates",
                column: "OwnerUserId",
                principalTable: "Users",
                principalColumn: "Id",
                onDelete: ReferentialAction.Restrict);

            migrationBuilder.Sql("""
                UPDATE ChecklistTemplates
                SET OwnerUserId = CreatedByUserId
                WHERE OwnerUserId IS NULL;
                """);

            migrationBuilder.AlterColumn<Guid>(
                name: "OwnerUserId",
                table: "ChecklistTemplates",
                type: "uniqueidentifier",
                nullable: false,
                oldClrType: typeof(Guid),
                oldType: "uniqueidentifier",
                oldNullable: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_ChecklistTemplateItems_ScoreTypes_ScoreTypeId",
                table: "ChecklistTemplateItems");

            migrationBuilder.DropForeignKey(
                name: "FK_ChecklistTemplates_Users_OwnerUserId",
                table: "ChecklistTemplates");

            migrationBuilder.DropTable(
                name: "ScoreTypeValues");

            migrationBuilder.DropTable(
                name: "ScoreTypes");

            migrationBuilder.DropIndex(
                name: "IX_ChecklistTemplates_OwnerUserId",
                table: "ChecklistTemplates");

            migrationBuilder.DropIndex(
                name: "IX_ChecklistTemplateItems_ScoreTypeId",
                table: "ChecklistTemplateItems");

            migrationBuilder.DropColumn(
                name: "OwnerUserId",
                table: "ChecklistTemplates");

            migrationBuilder.DropColumn(
                name: "DimensionLowerLimit",
                table: "ChecklistTemplateItems");

            migrationBuilder.DropColumn(
                name: "DimensionTarget",
                table: "ChecklistTemplateItems");

            migrationBuilder.DropColumn(
                name: "DimensionUnitOfMeasure",
                table: "ChecklistTemplateItems");

            migrationBuilder.DropColumn(
                name: "DimensionUpperLimit",
                table: "ChecklistTemplateItems");

            migrationBuilder.DropColumn(
                name: "ScoreTypeId",
                table: "ChecklistTemplateItems");

            migrationBuilder.DropColumn(
                name: "Section",
                table: "ChecklistTemplateItems");

            migrationBuilder.AlterColumn<string>(
                name: "ResponseValue",
                table: "ChecklistEntryItemResponses",
                type: "nvarchar(16)",
                maxLength: 16,
                nullable: false,
                oldClrType: typeof(string),
                oldType: "nvarchar(4000)",
                oldMaxLength: 4000);
        }
    }
}
