using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace MESv2.Api.Migrations
{
    /// <inheritdoc />
    public partial class AddChecklistEngine : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "ChecklistTemplates",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    TemplateCode = table.Column<string>(type: "nvarchar(64)", maxLength: 64, nullable: false),
                    Title = table.Column<string>(type: "nvarchar(200)", maxLength: 200, nullable: false),
                    ChecklistType = table.Column<string>(type: "nvarchar(64)", maxLength: 64, nullable: false),
                    ScopeLevel = table.Column<string>(type: "nvarchar(32)", maxLength: 32, nullable: false),
                    SiteId = table.Column<Guid>(type: "uniqueidentifier", nullable: true),
                    WorkCenterId = table.Column<Guid>(type: "uniqueidentifier", nullable: true),
                    ProductionLineId = table.Column<Guid>(type: "uniqueidentifier", nullable: true),
                    VersionNo = table.Column<int>(type: "int", nullable: false),
                    EffectiveFromUtc = table.Column<DateTime>(type: "datetime2", nullable: false),
                    EffectiveToUtc = table.Column<DateTime>(type: "datetime2", nullable: true),
                    IsActive = table.Column<bool>(type: "bit", nullable: false),
                    ResponseMode = table.Column<string>(type: "nvarchar(16)", maxLength: 16, nullable: false),
                    RequireFailNote = table.Column<bool>(type: "bit", nullable: false),
                    IsSafetyProfile = table.Column<bool>(type: "bit", nullable: false),
                    CreatedByUserId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    CreatedAtUtc = table.Column<DateTime>(type: "datetime2", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_ChecklistTemplates", x => x.Id);
                    table.ForeignKey(
                        name: "FK_ChecklistTemplates_Plants_SiteId",
                        column: x => x.SiteId,
                        principalTable: "Plants",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_ChecklistTemplates_ProductionLines_ProductionLineId",
                        column: x => x.ProductionLineId,
                        principalTable: "ProductionLines",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_ChecklistTemplates_Users_CreatedByUserId",
                        column: x => x.CreatedByUserId,
                        principalTable: "Users",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_ChecklistTemplates_WorkCenters_WorkCenterId",
                        column: x => x.WorkCenterId,
                        principalTable: "WorkCenters",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "ChecklistEntries",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    ChecklistTemplateId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    ChecklistType = table.Column<string>(type: "nvarchar(64)", maxLength: 64, nullable: false),
                    SiteId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    WorkCenterId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    ProductionLineId = table.Column<Guid>(type: "uniqueidentifier", nullable: true),
                    OperatorUserId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    Status = table.Column<string>(type: "nvarchar(32)", maxLength: 32, nullable: false),
                    StartedAtUtc = table.Column<DateTime>(type: "datetime2", nullable: false),
                    CompletedAtUtc = table.Column<DateTime>(type: "datetime2", nullable: true),
                    ResolvedFromScope = table.Column<string>(type: "nvarchar(32)", maxLength: 32, nullable: false),
                    ResolvedTemplateCode = table.Column<string>(type: "nvarchar(64)", maxLength: 64, nullable: false),
                    ResolvedTemplateVersionNo = table.Column<int>(type: "int", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_ChecklistEntries", x => x.Id);
                    table.ForeignKey(
                        name: "FK_ChecklistEntries_ChecklistTemplates_ChecklistTemplateId",
                        column: x => x.ChecklistTemplateId,
                        principalTable: "ChecklistTemplates",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_ChecklistEntries_Plants_SiteId",
                        column: x => x.SiteId,
                        principalTable: "Plants",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_ChecklistEntries_ProductionLines_ProductionLineId",
                        column: x => x.ProductionLineId,
                        principalTable: "ProductionLines",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_ChecklistEntries_Users_OperatorUserId",
                        column: x => x.OperatorUserId,
                        principalTable: "Users",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_ChecklistEntries_WorkCenters_WorkCenterId",
                        column: x => x.WorkCenterId,
                        principalTable: "WorkCenters",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "ChecklistTemplateItems",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    ChecklistTemplateId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    SortOrder = table.Column<int>(type: "int", nullable: false),
                    Prompt = table.Column<string>(type: "nvarchar(500)", maxLength: 500, nullable: false),
                    IsRequired = table.Column<bool>(type: "bit", nullable: false),
                    ResponseMode = table.Column<string>(type: "nvarchar(16)", maxLength: 16, nullable: true),
                    RequireFailNote = table.Column<bool>(type: "bit", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_ChecklistTemplateItems", x => x.Id);
                    table.ForeignKey(
                        name: "FK_ChecklistTemplateItems_ChecklistTemplates_ChecklistTemplateId",
                        column: x => x.ChecklistTemplateId,
                        principalTable: "ChecklistTemplates",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "ChecklistEntryItemResponses",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    ChecklistEntryId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    ChecklistTemplateItemId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    ResponseValue = table.Column<string>(type: "nvarchar(16)", maxLength: 16, nullable: false),
                    Note = table.Column<string>(type: "nvarchar(1000)", maxLength: 1000, nullable: true),
                    RespondedAtUtc = table.Column<DateTime>(type: "datetime2", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_ChecklistEntryItemResponses", x => x.Id);
                    table.ForeignKey(
                        name: "FK_ChecklistEntryItemResponses_ChecklistEntries_ChecklistEntryId",
                        column: x => x.ChecklistEntryId,
                        principalTable: "ChecklistEntries",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_ChecklistEntryItemResponses_ChecklistTemplateItems_ChecklistTemplateItemId",
                        column: x => x.ChecklistTemplateItemId,
                        principalTable: "ChecklistTemplateItems",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateIndex(
                name: "IX_ChecklistEntries_ChecklistTemplateId",
                table: "ChecklistEntries",
                column: "ChecklistTemplateId");

            migrationBuilder.CreateIndex(
                name: "IX_ChecklistEntries_ChecklistType",
                table: "ChecklistEntries",
                column: "ChecklistType");

            migrationBuilder.CreateIndex(
                name: "IX_ChecklistEntries_OperatorUserId",
                table: "ChecklistEntries",
                column: "OperatorUserId");

            migrationBuilder.CreateIndex(
                name: "IX_ChecklistEntries_ProductionLineId",
                table: "ChecklistEntries",
                column: "ProductionLineId");

            migrationBuilder.CreateIndex(
                name: "IX_ChecklistEntries_SiteId_WorkCenterId_StartedAtUtc",
                table: "ChecklistEntries",
                columns: new[] { "SiteId", "WorkCenterId", "StartedAtUtc" });

            migrationBuilder.CreateIndex(
                name: "IX_ChecklistEntries_WorkCenterId",
                table: "ChecklistEntries",
                column: "WorkCenterId");

            migrationBuilder.CreateIndex(
                name: "IX_ChecklistEntryItemResponses_ChecklistEntryId_ChecklistTemplateItemId",
                table: "ChecklistEntryItemResponses",
                columns: new[] { "ChecklistEntryId", "ChecklistTemplateItemId" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_ChecklistEntryItemResponses_ChecklistTemplateItemId",
                table: "ChecklistEntryItemResponses",
                column: "ChecklistTemplateItemId");

            migrationBuilder.CreateIndex(
                name: "IX_ChecklistTemplateItems_ChecklistTemplateId_SortOrder",
                table: "ChecklistTemplateItems",
                columns: new[] { "ChecklistTemplateId", "SortOrder" });

            migrationBuilder.CreateIndex(
                name: "IX_ChecklistTemplates_ChecklistType_IsActive_EffectiveFromUtc_EffectiveToUtc",
                table: "ChecklistTemplates",
                columns: new[] { "ChecklistType", "IsActive", "EffectiveFromUtc", "EffectiveToUtc" });

            migrationBuilder.CreateIndex(
                name: "IX_ChecklistTemplates_ChecklistType_ScopeLevel_SiteId_WorkCenterId_ProductionLineId_VersionNo",
                table: "ChecklistTemplates",
                columns: new[] { "ChecklistType", "ScopeLevel", "SiteId", "WorkCenterId", "ProductionLineId", "VersionNo" },
                unique: true,
                filter: "[SiteId] IS NOT NULL AND [WorkCenterId] IS NOT NULL AND [ProductionLineId] IS NOT NULL");

            migrationBuilder.CreateIndex(
                name: "IX_ChecklistTemplates_CreatedByUserId",
                table: "ChecklistTemplates",
                column: "CreatedByUserId");

            migrationBuilder.CreateIndex(
                name: "IX_ChecklistTemplates_ProductionLineId",
                table: "ChecklistTemplates",
                column: "ProductionLineId");

            migrationBuilder.CreateIndex(
                name: "IX_ChecklistTemplates_SiteId",
                table: "ChecklistTemplates",
                column: "SiteId");

            migrationBuilder.CreateIndex(
                name: "IX_ChecklistTemplates_WorkCenterId",
                table: "ChecklistTemplates",
                column: "WorkCenterId");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "ChecklistEntryItemResponses");

            migrationBuilder.DropTable(
                name: "ChecklistEntries");

            migrationBuilder.DropTable(
                name: "ChecklistTemplateItems");

            migrationBuilder.DropTable(
                name: "ChecklistTemplates");
        }
    }
}
