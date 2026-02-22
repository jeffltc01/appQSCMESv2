using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace MESv2.Api.Migrations
{
    /// <inheritdoc />
    public partial class ReplaceAnnotationFlagWithStatus : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "Status",
                table: "Annotations",
                type: "nvarchar(20)",
                maxLength: 20,
                nullable: false,
                defaultValue: "Open");

            migrationBuilder.Sql(
                "UPDATE Annotations SET Status = CASE WHEN Flag = 1 THEN 'Open' ELSE 'Closed' END");

            migrationBuilder.DropColumn(
                name: "Flag",
                table: "Annotations");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<bool>(
                name: "Flag",
                table: "Annotations",
                type: "bit",
                nullable: false,
                defaultValue: false);

            migrationBuilder.Sql(
                "UPDATE Annotations SET Flag = CASE WHEN Status = 'Open' THEN 1 ELSE 0 END");

            migrationBuilder.DropColumn(
                name: "Status",
                table: "Annotations");
        }
    }
}
