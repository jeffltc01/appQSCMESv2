using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace MESv2.Api.Migrations
{
    /// <inheritdoc />
    public partial class AddAssetIsActive : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<bool>(
                name: "IsActive",
                table: "Assets",
                type: "bit",
                nullable: false,
                defaultValue: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "IsActive",
                table: "Assets");
        }
    }
}
