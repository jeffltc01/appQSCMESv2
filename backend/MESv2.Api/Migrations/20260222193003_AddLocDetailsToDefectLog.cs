using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace MESv2.Api.Migrations
{
    /// <inheritdoc />
    public partial class AddLocDetailsToDefectLog : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "LocationDetail",
                table: "DefectLogs");

            migrationBuilder.AddColumn<decimal>(
                name: "LocDetails1",
                table: "DefectLogs",
                type: "decimal(18,6)",
                precision: 18,
                scale: 6,
                nullable: true);

            migrationBuilder.AddColumn<decimal>(
                name: "LocDetails2",
                table: "DefectLogs",
                type: "decimal(18,6)",
                precision: 18,
                scale: 6,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "LocDetailsCode",
                table: "DefectLogs",
                type: "nvarchar(20)",
                maxLength: 20,
                nullable: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "LocDetails1",
                table: "DefectLogs");

            migrationBuilder.DropColumn(
                name: "LocDetails2",
                table: "DefectLogs");

            migrationBuilder.DropColumn(
                name: "LocDetailsCode",
                table: "DefectLogs");

            migrationBuilder.AddColumn<string>(
                name: "LocationDetail",
                table: "DefectLogs",
                type: "nvarchar(max)",
                nullable: true);
        }
    }
}
