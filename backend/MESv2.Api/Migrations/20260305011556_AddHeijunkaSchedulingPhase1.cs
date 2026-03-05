using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace MESv2.Api.Migrations
{
    /// <inheritdoc />
    public partial class AddHeijunkaSchedulingPhase1 : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "ErpDemandSnapshots",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    SiteCode = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    ErpSalesOrderId = table.Column<string>(type: "nvarchar(450)", nullable: false),
                    ErpSalesOrderLineId = table.Column<string>(type: "nvarchar(450)", nullable: false),
                    ErpLoadNumberRaw = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    LoadGroupId = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    LoadLegIndex = table.Column<int>(type: "int", nullable: false),
                    DispatchDateLocal = table.Column<DateTime>(type: "datetime2", nullable: false),
                    ProductId = table.Column<Guid>(type: "uniqueidentifier", nullable: true),
                    ErpSkuCode = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    MesPlanningGroupId = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    RequiredQty = table.Column<decimal>(type: "decimal(18,2)", nullable: false),
                    OrderStatus = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    ErpLastChangedAtUtc = table.Column<DateTime>(type: "datetime2", nullable: true),
                    CapturedAtUtc = table.Column<DateTime>(type: "datetime2", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_ErpDemandSnapshots", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "ErpSalesOrderDemandRows",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    ErpSalesOrderId = table.Column<string>(type: "nvarchar(450)", nullable: false),
                    ErpSalesOrderLineId = table.Column<string>(type: "nvarchar(450)", nullable: false),
                    ErpSkuCode = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    SiteCode = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    ErpLoadNumberRaw = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    DispatchDateLocal = table.Column<DateTime>(type: "datetime2", nullable: false),
                    RequiredQty = table.Column<decimal>(type: "decimal(18,2)", nullable: false),
                    OrderStatus = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    SourceExtractedAtUtc = table.Column<DateTime>(type: "datetime2", nullable: false),
                    ErpLastChangedAtUtc = table.Column<DateTime>(type: "datetime2", nullable: true),
                    SourceBatchId = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    IngestedAtUtc = table.Column<DateTime>(type: "datetime2", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_ErpSalesOrderDemandRows", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "ErpSkuPlanningGroupMappings",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    ErpSkuCode = table.Column<string>(type: "nvarchar(450)", nullable: false),
                    MesPlanningGroupId = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    SiteCode = table.Column<string>(type: "nvarchar(450)", nullable: true),
                    EffectiveFromUtc = table.Column<DateTime>(type: "datetime2", nullable: false),
                    EffectiveToUtc = table.Column<DateTime>(type: "datetime2", nullable: true),
                    IsActive = table.Column<bool>(type: "bit", nullable: false),
                    MappingOwnerUserId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    LastReviewedAtUtc = table.Column<DateTime>(type: "datetime2", nullable: true),
                    RequiresReview = table.Column<bool>(type: "bit", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_ErpSkuPlanningGroupMappings", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "ScheduleExecutionEvents",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    SiteCode = table.Column<string>(type: "nvarchar(450)", nullable: false),
                    ProductionLineId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    ExecutionResourceId = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    ProductId = table.Column<Guid>(type: "uniqueidentifier", nullable: true),
                    MesPlanningGroupId = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    ExecutionDateLocal = table.Column<DateTime>(type: "datetime2", nullable: false),
                    ActualQty = table.Column<decimal>(type: "decimal(18,2)", nullable: false),
                    RunStartUtc = table.Column<DateTime>(type: "datetime2", nullable: true),
                    RunEndUtc = table.Column<DateTime>(type: "datetime2", nullable: true),
                    ScheduleLineId = table.Column<Guid>(type: "uniqueidentifier", nullable: true),
                    ExecutionState = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    ShortfallReasonCode = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    RecordedAtUtc = table.Column<DateTime>(type: "datetime2", nullable: false),
                    RecordedByUserId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    IdempotencyKey = table.Column<string>(type: "nvarchar(450)", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_ScheduleExecutionEvents", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "Schedules",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    SiteCode = table.Column<string>(type: "nvarchar(450)", nullable: false),
                    ProductionLineId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    WeekStartDateLocal = table.Column<DateTime>(type: "datetime2", nullable: false),
                    Status = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    PublishedAtUtc = table.Column<DateTime>(type: "datetime2", nullable: true),
                    PublishedByUserId = table.Column<Guid>(type: "uniqueidentifier", nullable: true),
                    FreezeHours = table.Column<int>(type: "int", nullable: false),
                    RevisionNumber = table.Column<int>(type: "int", nullable: false),
                    RowVersion = table.Column<byte[]>(type: "rowversion", rowVersion: true, nullable: true),
                    CreatedAtUtc = table.Column<DateTime>(type: "datetime2", nullable: false),
                    CreatedByUserId = table.Column<Guid>(type: "uniqueidentifier", nullable: true),
                    LastModifiedAtUtc = table.Column<DateTime>(type: "datetime2", nullable: false),
                    LastModifiedByUserId = table.Column<Guid>(type: "uniqueidentifier", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Schedules", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "SupermarketPositionSnapshots",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    SiteCode = table.Column<string>(type: "nvarchar(450)", nullable: false),
                    ProductionLineId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    ProductId = table.Column<Guid>(type: "uniqueidentifier", nullable: true),
                    OnHandQty = table.Column<decimal>(type: "decimal(18,2)", nullable: false),
                    InTransitQty = table.Column<decimal>(type: "decimal(18,2)", nullable: false),
                    DemandQty = table.Column<decimal>(type: "decimal(18,2)", nullable: false),
                    StockoutStartUtc = table.Column<DateTime>(type: "datetime2", nullable: true),
                    StockoutEndUtc = table.Column<DateTime>(type: "datetime2", nullable: true),
                    CapturedAtUtc = table.Column<DateTime>(type: "datetime2", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_SupermarketPositionSnapshots", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "UnmappedDemandExceptions",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    ErpSkuCode = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    SiteCode = table.Column<string>(type: "nvarchar(450)", nullable: false),
                    LoadGroupId = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    DispatchDateLocal = table.Column<DateTime>(type: "datetime2", nullable: false),
                    RequiredQty = table.Column<decimal>(type: "decimal(18,2)", nullable: false),
                    DetectedAtUtc = table.Column<DateTime>(type: "datetime2", nullable: false),
                    ExceptionStatus = table.Column<string>(type: "nvarchar(450)", nullable: false),
                    ResolutionNotes = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    ResolvedByUserId = table.Column<Guid>(type: "uniqueidentifier", nullable: true),
                    ResolvedAtUtc = table.Column<DateTime>(type: "datetime2", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_UnmappedDemandExceptions", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "ScheduleChangeLogs",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    ScheduleId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    ScheduleLineId = table.Column<Guid>(type: "uniqueidentifier", nullable: true),
                    ChangedAtUtc = table.Column<DateTime>(type: "datetime2", nullable: false),
                    ChangedByUserId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    ChangeReasonCode = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    FieldName = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    FromValue = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    ToValue = table.Column<string>(type: "nvarchar(max)", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_ScheduleChangeLogs", x => x.Id);
                    table.ForeignKey(
                        name: "FK_ScheduleChangeLogs_Schedules_ScheduleId",
                        column: x => x.ScheduleId,
                        principalTable: "Schedules",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "ScheduleLines",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    ScheduleId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    PlannedDateLocal = table.Column<DateTime>(type: "datetime2", nullable: false),
                    SequenceIndex = table.Column<int>(type: "int", nullable: true),
                    ProductId = table.Column<Guid>(type: "uniqueidentifier", nullable: true),
                    PlanningClass = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    PlannedQty = table.Column<decimal>(type: "decimal(18,2)", nullable: false),
                    PlannedStartLocal = table.Column<DateTime>(type: "datetime2", nullable: true),
                    PlannedEndLocal = table.Column<DateTime>(type: "datetime2", nullable: true),
                    PolicySnapshotJson = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    LoadGroupId = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    DispatchDateLocal = table.Column<DateTime>(type: "datetime2", nullable: true),
                    MesPlanningGroupId = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    PlanningResourceId = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    ExecutionResourceId = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    CreatedAtUtc = table.Column<DateTime>(type: "datetime2", nullable: false),
                    CreatedByUserId = table.Column<Guid>(type: "uniqueidentifier", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_ScheduleLines", x => x.Id);
                    table.ForeignKey(
                        name: "FK_ScheduleLines_Schedules_ScheduleId",
                        column: x => x.ScheduleId,
                        principalTable: "Schedules",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "IX_ErpDemandSnapshots_ErpSalesOrderId_ErpSalesOrderLineId_CapturedAtUtc",
                table: "ErpDemandSnapshots",
                columns: new[] { "ErpSalesOrderId", "ErpSalesOrderLineId", "CapturedAtUtc" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_ErpSalesOrderDemandRows_ErpSalesOrderId_ErpSalesOrderLineId_SourceExtractedAtUtc",
                table: "ErpSalesOrderDemandRows",
                columns: new[] { "ErpSalesOrderId", "ErpSalesOrderLineId", "SourceExtractedAtUtc" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_ErpSkuPlanningGroupMappings_ErpSkuCode_SiteCode_EffectiveFromUtc",
                table: "ErpSkuPlanningGroupMappings",
                columns: new[] { "ErpSkuCode", "SiteCode", "EffectiveFromUtc" },
                unique: true,
                filter: "[SiteCode] IS NOT NULL");

            migrationBuilder.CreateIndex(
                name: "IX_ScheduleChangeLogs_ScheduleId_ChangedAtUtc",
                table: "ScheduleChangeLogs",
                columns: new[] { "ScheduleId", "ChangedAtUtc" });

            migrationBuilder.CreateIndex(
                name: "IX_ScheduleExecutionEvents_IdempotencyKey",
                table: "ScheduleExecutionEvents",
                column: "IdempotencyKey",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_ScheduleExecutionEvents_SiteCode_ProductionLineId_ExecutionDateLocal",
                table: "ScheduleExecutionEvents",
                columns: new[] { "SiteCode", "ProductionLineId", "ExecutionDateLocal" });

            migrationBuilder.CreateIndex(
                name: "IX_ScheduleLines_ScheduleId_PlannedDateLocal_SequenceIndex",
                table: "ScheduleLines",
                columns: new[] { "ScheduleId", "PlannedDateLocal", "SequenceIndex" });

            migrationBuilder.CreateIndex(
                name: "IX_Schedules_SiteCode_ProductionLineId_WeekStartDateLocal_RevisionNumber",
                table: "Schedules",
                columns: new[] { "SiteCode", "ProductionLineId", "WeekStartDateLocal", "RevisionNumber" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_SupermarketPositionSnapshots_SiteCode_ProductionLineId_ProductId_CapturedAtUtc",
                table: "SupermarketPositionSnapshots",
                columns: new[] { "SiteCode", "ProductionLineId", "ProductId", "CapturedAtUtc" });

            migrationBuilder.CreateIndex(
                name: "IX_UnmappedDemandExceptions_SiteCode_ExceptionStatus_DispatchDateLocal",
                table: "UnmappedDemandExceptions",
                columns: new[] { "SiteCode", "ExceptionStatus", "DispatchDateLocal" });
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "ErpDemandSnapshots");

            migrationBuilder.DropTable(
                name: "ErpSalesOrderDemandRows");

            migrationBuilder.DropTable(
                name: "ErpSkuPlanningGroupMappings");

            migrationBuilder.DropTable(
                name: "ScheduleChangeLogs");

            migrationBuilder.DropTable(
                name: "ScheduleExecutionEvents");

            migrationBuilder.DropTable(
                name: "ScheduleLines");

            migrationBuilder.DropTable(
                name: "SupermarketPositionSnapshots");

            migrationBuilder.DropTable(
                name: "UnmappedDemandExceptions");

            migrationBuilder.DropTable(
                name: "Schedules");
        }
    }
}
