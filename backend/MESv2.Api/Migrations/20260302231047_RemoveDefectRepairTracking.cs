using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace MESv2.Api.Migrations
{
    /// <inheritdoc />
    public partial class RemoveDefectRepairTracking : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_DefectLogs_Users_RepairedByUserId",
                table: "DefectLogs");

            migrationBuilder.DropIndex(
                name: "IX_DefectLogs_RepairedByUserId",
                table: "DefectLogs");

            migrationBuilder.DropColumn(
                name: "IsRepaired",
                table: "DefectLogs");

            migrationBuilder.DropColumn(
                name: "RepairedByUserId",
                table: "DefectLogs");

            migrationBuilder.DropColumn(
                name: "RepairedDateTime",
                table: "DefectLogs");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<bool>(
                name: "IsRepaired",
                table: "DefectLogs",
                type: "bit",
                nullable: false,
                defaultValue: false);

            migrationBuilder.AddColumn<Guid>(
                name: "RepairedByUserId",
                table: "DefectLogs",
                type: "uniqueidentifier",
                nullable: true);

            migrationBuilder.AddColumn<DateTime>(
                name: "RepairedDateTime",
                table: "DefectLogs",
                type: "datetime2",
                nullable: true);

            migrationBuilder.CreateIndex(
                name: "IX_DefectLogs_RepairedByUserId",
                table: "DefectLogs",
                column: "RepairedByUserId");

            migrationBuilder.AddForeignKey(
                name: "FK_DefectLogs_Users_RepairedByUserId",
                table: "DefectLogs",
                column: "RepairedByUserId",
                principalTable: "Users",
                principalColumn: "Id",
                onDelete: ReferentialAction.Restrict);
        }
    }
}
