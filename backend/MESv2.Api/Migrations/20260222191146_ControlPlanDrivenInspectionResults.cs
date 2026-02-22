using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace MESv2.Api.Migrations
{
    /// <inheritdoc />
    public partial class ControlPlanDrivenInspectionResults : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_DefectLogs_InspectionRecords_InspectionRecordId",
                table: "DefectLogs");

            migrationBuilder.DropIndex(
                name: "IX_DefectLogs_InspectionRecordId",
                table: "DefectLogs");

            migrationBuilder.DropColumn(
                name: "InspectionResult",
                table: "ProductionRecords");

            migrationBuilder.DropColumn(
                name: "InspectionRecordId",
                table: "DefectLogs");

            migrationBuilder.Sql(
                "DELETE FROM InspectionRecords WHERE ControlPlanId IS NULL;");

            migrationBuilder.AlterColumn<Guid>(
                name: "ControlPlanId",
                table: "InspectionRecords",
                type: "uniqueidentifier",
                nullable: false,
                defaultValue: new Guid("00000000-0000-0000-0000-000000000000"),
                oldClrType: typeof(Guid),
                oldType: "uniqueidentifier",
                oldNullable: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "InspectionResult",
                table: "ProductionRecords",
                type: "nvarchar(max)",
                nullable: true);

            migrationBuilder.AlterColumn<Guid>(
                name: "ControlPlanId",
                table: "InspectionRecords",
                type: "uniqueidentifier",
                nullable: true,
                oldClrType: typeof(Guid),
                oldType: "uniqueidentifier");

            migrationBuilder.AddColumn<Guid>(
                name: "InspectionRecordId",
                table: "DefectLogs",
                type: "uniqueidentifier",
                nullable: true);

            migrationBuilder.CreateIndex(
                name: "IX_DefectLogs_InspectionRecordId",
                table: "DefectLogs",
                column: "InspectionRecordId");

            migrationBuilder.AddForeignKey(
                name: "FK_DefectLogs_InspectionRecords_InspectionRecordId",
                table: "DefectLogs",
                column: "InspectionRecordId",
                principalTable: "InspectionRecords",
                principalColumn: "Id",
                onDelete: ReferentialAction.Restrict);
        }
    }
}
