using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace MESv2.Api.Migrations
{
    /// <inheritdoc />
    public partial class AddChecklistItemResponseTypeMetadata : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "HelpText",
                table: "ChecklistTemplateItems",
                type: "nvarchar(500)",
                maxLength: 500,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "ResponseOptionsJson",
                table: "ChecklistTemplateItems",
                type: "nvarchar(4000)",
                maxLength: 4000,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "ResponseType",
                table: "ChecklistTemplateItems",
                type: "nvarchar(32)",
                maxLength: 32,
                nullable: false,
                defaultValue: "PassFail");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "HelpText",
                table: "ChecklistTemplateItems");

            migrationBuilder.DropColumn(
                name: "ResponseOptionsJson",
                table: "ChecklistTemplateItems");

            migrationBuilder.DropColumn(
                name: "ResponseType",
                table: "ChecklistTemplateItems");
        }
    }
}
