using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace MESv2.Api.Migrations
{
    /// <inheritdoc />
    public partial class EnforceSinglePublishedSchedulePerWeekLine : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateIndex(
                name: "IX_Schedules_SiteCode_ProductionLineId_WeekStartDateLocal",
                table: "Schedules",
                columns: new[] { "SiteCode", "ProductionLineId", "WeekStartDateLocal" },
                unique: true,
                filter: "[Status] = 'Published'");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "IX_Schedules_SiteCode_ProductionLineId_WeekStartDateLocal",
                table: "Schedules");
        }
    }
}
