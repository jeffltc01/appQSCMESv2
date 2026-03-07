using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace MESv2.Api.Migrations
{
    /// <inheritdoc />
    public partial class AddNotificationRuleTemplateContent : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "TemplateBody",
                table: "NotificationRules",
                type: "nvarchar(max)",
                nullable: false,
                defaultValue: "");

            migrationBuilder.AddColumn<string>(
                name: "TemplateTitle",
                table: "NotificationRules",
                type: "nvarchar(max)",
                nullable: false,
                defaultValue: "");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "TemplateBody",
                table: "NotificationRules");

            migrationBuilder.DropColumn(
                name: "TemplateTitle",
                table: "NotificationRules");
        }
    }
}
