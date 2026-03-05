using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace MESv2.Api.Migrations
{
    /// <inheritdoc />
    public partial class AddPlantPrinterDocumentRouting : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "IX_PlantPrinters_PlantId_PrinterName",
                table: "PlantPrinters");

            migrationBuilder.AlterColumn<string>(
                name: "PrinterName",
                table: "PlantPrinters",
                type: "nvarchar(max)",
                nullable: false,
                oldClrType: typeof(string),
                oldType: "nvarchar(450)");

            migrationBuilder.AlterColumn<string>(
                name: "PrintLocation",
                table: "PlantPrinters",
                type: "nvarchar(450)",
                nullable: false,
                oldClrType: typeof(string),
                oldType: "nvarchar(max)");

            migrationBuilder.AddColumn<string>(
                name: "DocumentPath",
                table: "PlantPrinters",
                type: "nvarchar(max)",
                nullable: false,
                defaultValue: "");

            migrationBuilder.CreateIndex(
                name: "IX_PlantPrinters_PlantId_PrintLocation",
                table: "PlantPrinters",
                columns: new[] { "PlantId", "PrintLocation" },
                unique: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "IX_PlantPrinters_PlantId_PrintLocation",
                table: "PlantPrinters");

            migrationBuilder.DropColumn(
                name: "DocumentPath",
                table: "PlantPrinters");

            migrationBuilder.AlterColumn<string>(
                name: "PrinterName",
                table: "PlantPrinters",
                type: "nvarchar(450)",
                nullable: false,
                oldClrType: typeof(string),
                oldType: "nvarchar(max)");

            migrationBuilder.AlterColumn<string>(
                name: "PrintLocation",
                table: "PlantPrinters",
                type: "nvarchar(max)",
                nullable: false,
                oldClrType: typeof(string),
                oldType: "nvarchar(450)");

            migrationBuilder.CreateIndex(
                name: "IX_PlantPrinters_PlantId_PrinterName",
                table: "PlantPrinters",
                columns: new[] { "PlantId", "PrinterName" },
                unique: true);
        }
    }
}
