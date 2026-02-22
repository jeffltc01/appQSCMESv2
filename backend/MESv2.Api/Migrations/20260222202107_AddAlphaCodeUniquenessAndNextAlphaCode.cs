using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace MESv2.Api.Migrations
{
    /// <inheritdoc />
    public partial class AddAlphaCodeUniquenessAndNextAlphaCode : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "IX_SerialNumbers_Serial",
                table: "SerialNumbers");

            migrationBuilder.AddColumn<string>(
                name: "NextTankAlphaCode",
                table: "Plants",
                type: "nvarchar(max)",
                nullable: false,
                defaultValue: "AA");

            migrationBuilder.CreateIndex(
                name: "IX_SerialNumbers_Serial_PlantId_CreatedAt",
                table: "SerialNumbers",
                columns: new[] { "Serial", "PlantId", "CreatedAt" },
                unique: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "IX_SerialNumbers_Serial_PlantId_CreatedAt",
                table: "SerialNumbers");

            migrationBuilder.DropColumn(
                name: "NextTankAlphaCode",
                table: "Plants");

            migrationBuilder.CreateIndex(
                name: "IX_SerialNumbers_Serial",
                table: "SerialNumbers",
                column: "Serial");
        }
    }
}
