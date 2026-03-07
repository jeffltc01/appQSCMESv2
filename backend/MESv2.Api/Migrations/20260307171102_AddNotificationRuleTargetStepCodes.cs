using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace MESv2.Api.Migrations
{
    /// <inheritdoc />
    public partial class AddNotificationRuleTargetStepCodes : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "TargetStepCodesJson",
                table: "NotificationRules",
                type: "nvarchar(max)",
                nullable: false,
                defaultValue: "[]");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "TargetStepCodesJson",
                table: "NotificationRules");
        }
    }
}
