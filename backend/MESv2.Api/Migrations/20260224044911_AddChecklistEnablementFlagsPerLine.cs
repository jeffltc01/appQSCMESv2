using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace MESv2.Api.Migrations
{
    /// <inheritdoc />
    public partial class AddChecklistEnablementFlagsPerLine : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<bool>(
                name: "EnableSafetyChecklist",
                table: "WorkCenterProductionLines",
                type: "bit",
                nullable: false,
                defaultValue: false);

            migrationBuilder.AddColumn<bool>(
                name: "EnableWorkCenterChecklist",
                table: "WorkCenterProductionLines",
                type: "bit",
                nullable: false,
                defaultValue: false);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "EnableSafetyChecklist",
                table: "WorkCenterProductionLines");

            migrationBuilder.DropColumn(
                name: "EnableWorkCenterChecklist",
                table: "WorkCenterProductionLines");
        }
    }
}
