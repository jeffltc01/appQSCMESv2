using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace MESv2.Api.Migrations
{
    /// <inheritdoc />
    public partial class CharacteristicsAndControlPlanImprovements : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_ControlPlans_WorkCenters_WorkCenterId",
                table: "ControlPlans");

            migrationBuilder.RenameColumn(
                name: "WorkCenterId",
                table: "ControlPlans",
                newName: "WorkCenterProductionLineId");

            migrationBuilder.RenameIndex(
                name: "IX_ControlPlans_WorkCenterId",
                table: "ControlPlans",
                newName: "IX_ControlPlans_WorkCenterProductionLineId");

            // Migrate data: resolve WorkCenter GUIDs to WorkCenterProductionLine GUIDs
            migrationBuilder.Sql(@"
                UPDATE cp
                SET cp.WorkCenterProductionLineId = wcpl.Id
                FROM ControlPlans cp
                INNER JOIN WorkCenterProductionLines wcpl ON wcpl.WorkCenterId = cp.WorkCenterProductionLineId
            ");

            migrationBuilder.AddColumn<bool>(
                name: "CodeRequired",
                table: "ControlPlans",
                type: "bit",
                nullable: false,
                defaultValue: false);

            migrationBuilder.AddColumn<bool>(
                name: "IsActive",
                table: "ControlPlans",
                type: "bit",
                nullable: false,
                defaultValue: true);

            migrationBuilder.AddColumn<string>(
                name: "Code",
                table: "Characteristics",
                type: "nvarchar(450)",
                nullable: false,
                defaultValue: "");

            // Assign unique Code values to existing Characteristics that have empty codes
            migrationBuilder.Sql(@"
                ;WITH Numbered AS (
                    SELECT Id, Code, ROW_NUMBER() OVER (ORDER BY Name) AS RowNum
                    FROM Characteristics
                    WHERE Code = ''
                )
                UPDATE Numbered
                SET Code = RIGHT('000' + CAST(RowNum AS NVARCHAR(10)), 3)
            ");

            migrationBuilder.AddColumn<bool>(
                name: "IsActive",
                table: "Characteristics",
                type: "bit",
                nullable: false,
                defaultValue: true);

            migrationBuilder.CreateIndex(
                name: "IX_Characteristics_Code",
                table: "Characteristics",
                column: "Code",
                unique: true);

            migrationBuilder.AddForeignKey(
                name: "FK_ControlPlans_WorkCenterProductionLines_WorkCenterProductionLineId",
                table: "ControlPlans",
                column: "WorkCenterProductionLineId",
                principalTable: "WorkCenterProductionLines",
                principalColumn: "Id",
                onDelete: ReferentialAction.Restrict);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_ControlPlans_WorkCenterProductionLines_WorkCenterProductionLineId",
                table: "ControlPlans");

            migrationBuilder.DropIndex(
                name: "IX_Characteristics_Code",
                table: "Characteristics");

            migrationBuilder.DropColumn(
                name: "CodeRequired",
                table: "ControlPlans");

            migrationBuilder.DropColumn(
                name: "IsActive",
                table: "ControlPlans");

            migrationBuilder.DropColumn(
                name: "Code",
                table: "Characteristics");

            migrationBuilder.DropColumn(
                name: "IsActive",
                table: "Characteristics");

            migrationBuilder.RenameColumn(
                name: "WorkCenterProductionLineId",
                table: "ControlPlans",
                newName: "WorkCenterId");

            migrationBuilder.RenameIndex(
                name: "IX_ControlPlans_WorkCenterProductionLineId",
                table: "ControlPlans",
                newName: "IX_ControlPlans_WorkCenterId");

            migrationBuilder.AddForeignKey(
                name: "FK_ControlPlans_WorkCenters_WorkCenterId",
                table: "ControlPlans",
                column: "WorkCenterId",
                principalTable: "WorkCenters",
                principalColumn: "Id",
                onDelete: ReferentialAction.Restrict);
        }
    }
}
