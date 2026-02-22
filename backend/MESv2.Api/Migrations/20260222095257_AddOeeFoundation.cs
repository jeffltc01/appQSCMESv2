using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace MESv2.Api.Migrations
{
    /// <inheritdoc />
    public partial class AddOeeFoundation : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "ShiftSchedules",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    PlantId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    EffectiveDate = table.Column<DateOnly>(type: "date", nullable: false),
                    MondayHours = table.Column<decimal>(type: "decimal(5,2)", precision: 5, scale: 2, nullable: false),
                    MondayBreakMinutes = table.Column<int>(type: "int", nullable: false),
                    TuesdayHours = table.Column<decimal>(type: "decimal(5,2)", precision: 5, scale: 2, nullable: false),
                    TuesdayBreakMinutes = table.Column<int>(type: "int", nullable: false),
                    WednesdayHours = table.Column<decimal>(type: "decimal(5,2)", precision: 5, scale: 2, nullable: false),
                    WednesdayBreakMinutes = table.Column<int>(type: "int", nullable: false),
                    ThursdayHours = table.Column<decimal>(type: "decimal(5,2)", precision: 5, scale: 2, nullable: false),
                    ThursdayBreakMinutes = table.Column<int>(type: "int", nullable: false),
                    FridayHours = table.Column<decimal>(type: "decimal(5,2)", precision: 5, scale: 2, nullable: false),
                    FridayBreakMinutes = table.Column<int>(type: "int", nullable: false),
                    SaturdayHours = table.Column<decimal>(type: "decimal(5,2)", precision: 5, scale: 2, nullable: false),
                    SaturdayBreakMinutes = table.Column<int>(type: "int", nullable: false),
                    SundayHours = table.Column<decimal>(type: "decimal(5,2)", precision: 5, scale: 2, nullable: false),
                    SundayBreakMinutes = table.Column<int>(type: "int", nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "datetime2", nullable: false),
                    CreatedByUserId = table.Column<Guid>(type: "uniqueidentifier", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_ShiftSchedules", x => x.Id);
                    table.ForeignKey(
                        name: "FK_ShiftSchedules_Plants_PlantId",
                        column: x => x.PlantId,
                        principalTable: "Plants",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_ShiftSchedules_Users_CreatedByUserId",
                        column: x => x.CreatedByUserId,
                        principalTable: "Users",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "WorkCenterCapacityTargets",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    WorkCenterProductionLineId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    TankSize = table.Column<int>(type: "int", nullable: true),
                    PlantGearId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    TargetUnitsPerHour = table.Column<decimal>(type: "decimal(10,2)", precision: 10, scale: 2, nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_WorkCenterCapacityTargets", x => x.Id);
                    table.ForeignKey(
                        name: "FK_WorkCenterCapacityTargets_PlantGears_PlantGearId",
                        column: x => x.PlantGearId,
                        principalTable: "PlantGears",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_WorkCenterCapacityTargets_WorkCenterProductionLines_WorkCenterProductionLineId",
                        column: x => x.WorkCenterProductionLineId,
                        principalTable: "WorkCenterProductionLines",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "IX_ShiftSchedules_CreatedByUserId",
                table: "ShiftSchedules",
                column: "CreatedByUserId");

            migrationBuilder.CreateIndex(
                name: "IX_ShiftSchedules_PlantId_EffectiveDate",
                table: "ShiftSchedules",
                columns: new[] { "PlantId", "EffectiveDate" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_WorkCenterCapacityTargets_PlantGearId",
                table: "WorkCenterCapacityTargets",
                column: "PlantGearId");

            migrationBuilder.CreateIndex(
                name: "IX_WorkCenterCapacityTargets_WorkCenterProductionLineId_TankSize_PlantGearId",
                table: "WorkCenterCapacityTargets",
                columns: new[] { "WorkCenterProductionLineId", "TankSize", "PlantGearId" },
                unique: true,
                filter: "[TankSize] IS NOT NULL");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "ShiftSchedules");

            migrationBuilder.DropTable(
                name: "WorkCenterCapacityTargets");
        }
    }
}
