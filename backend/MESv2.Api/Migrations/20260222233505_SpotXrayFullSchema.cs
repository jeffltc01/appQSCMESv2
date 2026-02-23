using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace MESv2.Api.Migrations
{
    /// <inheritdoc />
    public partial class SpotXrayFullSchema : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            // Drop old string welder columns (replaced by Guid FK columns below)
            migrationBuilder.DropColumn(name: "Welder1", table: "SpotXrayIncrements");
            migrationBuilder.DropColumn(name: "Welder2", table: "SpotXrayIncrements");
            migrationBuilder.DropColumn(name: "Welder3", table: "SpotXrayIncrements");
            migrationBuilder.DropColumn(name: "Welder4", table: "SpotXrayIncrements");

            // Rename InitialResult → Result (the "initial" is implied)
            migrationBuilder.RenameColumn(name: "Seam1InitialResult", table: "SpotXrayIncrements", newName: "Seam1Result");
            migrationBuilder.RenameColumn(name: "Seam2InitialResult", table: "SpotXrayIncrements", newName: "Seam2Result");
            migrationBuilder.RenameColumn(name: "Seam3InitialResult", table: "SpotXrayIncrements", newName: "Seam3Result");
            migrationBuilder.RenameColumn(name: "Seam4InitialResult", table: "SpotXrayIncrements", newName: "Seam4Result");

            migrationBuilder.AlterColumn<DateTime>(
                name: "Seam4ShotDateTime",
                table: "SpotXrayIncrements",
                type: "datetime2",
                nullable: true,
                oldClrType: typeof(string),
                oldType: "nvarchar(max)",
                oldNullable: true);

            migrationBuilder.AlterColumn<DateTime>(
                name: "Seam3ShotDateTime",
                table: "SpotXrayIncrements",
                type: "datetime2",
                nullable: true,
                oldClrType: typeof(string),
                oldType: "nvarchar(max)",
                oldNullable: true);

            migrationBuilder.AlterColumn<DateTime>(
                name: "Seam2ShotDateTime",
                table: "SpotXrayIncrements",
                type: "datetime2",
                nullable: true,
                oldClrType: typeof(string),
                oldType: "nvarchar(max)",
                oldNullable: true);

            migrationBuilder.AlterColumn<DateTime>(
                name: "Seam1ShotDateTime",
                table: "SpotXrayIncrements",
                type: "datetime2",
                nullable: true,
                oldClrType: typeof(string),
                oldType: "nvarchar(max)",
                oldNullable: true);

            migrationBuilder.AddColumn<DateTime>(
                name: "Seam1FinalDateTime",
                table: "SpotXrayIncrements",
                type: "datetime2",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "Seam1FinalShotNo",
                table: "SpotXrayIncrements",
                type: "nvarchar(max)",
                nullable: true);

            migrationBuilder.AddColumn<DateTime>(
                name: "Seam1Trace1DateTime",
                table: "SpotXrayIncrements",
                type: "datetime2",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "Seam1Trace1Result",
                table: "SpotXrayIncrements",
                type: "nvarchar(max)",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "Seam1Trace1ShotNo",
                table: "SpotXrayIncrements",
                type: "nvarchar(max)",
                nullable: true);

            migrationBuilder.AddColumn<Guid>(
                name: "Seam1Trace1TankId",
                table: "SpotXrayIncrements",
                type: "uniqueidentifier",
                nullable: true);

            migrationBuilder.AddColumn<DateTime>(
                name: "Seam1Trace2DateTime",
                table: "SpotXrayIncrements",
                type: "datetime2",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "Seam1Trace2Result",
                table: "SpotXrayIncrements",
                type: "nvarchar(max)",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "Seam1Trace2ShotNo",
                table: "SpotXrayIncrements",
                type: "nvarchar(max)",
                nullable: true);

            migrationBuilder.AddColumn<Guid>(
                name: "Seam1Trace2TankId",
                table: "SpotXrayIncrements",
                type: "uniqueidentifier",
                nullable: true);

            migrationBuilder.AddColumn<DateTime>(
                name: "Seam2FinalDateTime",
                table: "SpotXrayIncrements",
                type: "datetime2",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "Seam2FinalShotNo",
                table: "SpotXrayIncrements",
                type: "nvarchar(max)",
                nullable: true);

            migrationBuilder.AddColumn<DateTime>(
                name: "Seam2Trace1DateTime",
                table: "SpotXrayIncrements",
                type: "datetime2",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "Seam2Trace1Result",
                table: "SpotXrayIncrements",
                type: "nvarchar(max)",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "Seam2Trace1ShotNo",
                table: "SpotXrayIncrements",
                type: "nvarchar(max)",
                nullable: true);

            migrationBuilder.AddColumn<Guid>(
                name: "Seam2Trace1TankId",
                table: "SpotXrayIncrements",
                type: "uniqueidentifier",
                nullable: true);

            migrationBuilder.AddColumn<DateTime>(
                name: "Seam2Trace2DateTime",
                table: "SpotXrayIncrements",
                type: "datetime2",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "Seam2Trace2Result",
                table: "SpotXrayIncrements",
                type: "nvarchar(max)",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "Seam2Trace2ShotNo",
                table: "SpotXrayIncrements",
                type: "nvarchar(max)",
                nullable: true);

            migrationBuilder.AddColumn<Guid>(
                name: "Seam2Trace2TankId",
                table: "SpotXrayIncrements",
                type: "uniqueidentifier",
                nullable: true);

            migrationBuilder.AddColumn<DateTime>(
                name: "Seam3FinalDateTime",
                table: "SpotXrayIncrements",
                type: "datetime2",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "Seam3FinalShotNo",
                table: "SpotXrayIncrements",
                type: "nvarchar(max)",
                nullable: true);

            migrationBuilder.AddColumn<DateTime>(
                name: "Seam3Trace1DateTime",
                table: "SpotXrayIncrements",
                type: "datetime2",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "Seam3Trace1Result",
                table: "SpotXrayIncrements",
                type: "nvarchar(max)",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "Seam3Trace1ShotNo",
                table: "SpotXrayIncrements",
                type: "nvarchar(max)",
                nullable: true);

            migrationBuilder.AddColumn<Guid>(
                name: "Seam3Trace1TankId",
                table: "SpotXrayIncrements",
                type: "uniqueidentifier",
                nullable: true);

            migrationBuilder.AddColumn<DateTime>(
                name: "Seam3Trace2DateTime",
                table: "SpotXrayIncrements",
                type: "datetime2",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "Seam3Trace2Result",
                table: "SpotXrayIncrements",
                type: "nvarchar(max)",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "Seam3Trace2ShotNo",
                table: "SpotXrayIncrements",
                type: "nvarchar(max)",
                nullable: true);

            migrationBuilder.AddColumn<Guid>(
                name: "Seam3Trace2TankId",
                table: "SpotXrayIncrements",
                type: "uniqueidentifier",
                nullable: true);

            migrationBuilder.AddColumn<DateTime>(
                name: "Seam4FinalDateTime",
                table: "SpotXrayIncrements",
                type: "datetime2",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "Seam4FinalShotNo",
                table: "SpotXrayIncrements",
                type: "nvarchar(max)",
                nullable: true);

            migrationBuilder.AddColumn<DateTime>(
                name: "Seam4Trace1DateTime",
                table: "SpotXrayIncrements",
                type: "datetime2",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "Seam4Trace1Result",
                table: "SpotXrayIncrements",
                type: "nvarchar(max)",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "Seam4Trace1ShotNo",
                table: "SpotXrayIncrements",
                type: "nvarchar(max)",
                nullable: true);

            migrationBuilder.AddColumn<Guid>(
                name: "Seam4Trace1TankId",
                table: "SpotXrayIncrements",
                type: "uniqueidentifier",
                nullable: true);

            migrationBuilder.AddColumn<DateTime>(
                name: "Seam4Trace2DateTime",
                table: "SpotXrayIncrements",
                type: "datetime2",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "Seam4Trace2Result",
                table: "SpotXrayIncrements",
                type: "nvarchar(max)",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "Seam4Trace2ShotNo",
                table: "SpotXrayIncrements",
                type: "nvarchar(max)",
                nullable: true);

            migrationBuilder.AddColumn<Guid>(
                name: "Seam4Trace2TankId",
                table: "SpotXrayIncrements",
                type: "uniqueidentifier",
                nullable: true);

            migrationBuilder.AddColumn<Guid>(
                name: "Welder1Id",
                table: "SpotXrayIncrements",
                type: "uniqueidentifier",
                nullable: true);

            migrationBuilder.AddColumn<Guid>(
                name: "Welder2Id",
                table: "SpotXrayIncrements",
                type: "uniqueidentifier",
                nullable: true);

            migrationBuilder.AddColumn<Guid>(
                name: "Welder3Id",
                table: "SpotXrayIncrements",
                type: "uniqueidentifier",
                nullable: true);

            migrationBuilder.AddColumn<Guid>(
                name: "Welder4Id",
                table: "SpotXrayIncrements",
                type: "uniqueidentifier",
                nullable: true);

            migrationBuilder.CreateTable(
                name: "SpotXrayIncrementTanks",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    SpotXrayIncrementId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    SerialNumberId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    Position = table.Column<int>(type: "int", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_SpotXrayIncrementTanks", x => x.Id);
                    table.ForeignKey(
                        name: "FK_SpotXrayIncrementTanks_SerialNumbers_SerialNumberId",
                        column: x => x.SerialNumberId,
                        principalTable: "SerialNumbers",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_SpotXrayIncrementTanks_SpotXrayIncrements_SpotXrayIncrementId",
                        column: x => x.SpotXrayIncrementId,
                        principalTable: "SpotXrayIncrements",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "XrayShotCounters",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    PlantId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    CounterDate = table.Column<DateOnly>(type: "date", nullable: false),
                    LastShotNumber = table.Column<int>(type: "int", nullable: false),
                    LastIncrementNumber = table.Column<int>(type: "int", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_XrayShotCounters", x => x.Id);
                    table.ForeignKey(
                        name: "FK_XrayShotCounters_Plants_PlantId",
                        column: x => x.PlantId,
                        principalTable: "Plants",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateIndex(
                name: "IX_SpotXrayIncrements_Seam1Trace1TankId",
                table: "SpotXrayIncrements",
                column: "Seam1Trace1TankId");

            migrationBuilder.CreateIndex(
                name: "IX_SpotXrayIncrements_Seam1Trace2TankId",
                table: "SpotXrayIncrements",
                column: "Seam1Trace2TankId");

            migrationBuilder.CreateIndex(
                name: "IX_SpotXrayIncrements_Seam2Trace1TankId",
                table: "SpotXrayIncrements",
                column: "Seam2Trace1TankId");

            migrationBuilder.CreateIndex(
                name: "IX_SpotXrayIncrements_Seam2Trace2TankId",
                table: "SpotXrayIncrements",
                column: "Seam2Trace2TankId");

            migrationBuilder.CreateIndex(
                name: "IX_SpotXrayIncrements_Seam3Trace1TankId",
                table: "SpotXrayIncrements",
                column: "Seam3Trace1TankId");

            migrationBuilder.CreateIndex(
                name: "IX_SpotXrayIncrements_Seam3Trace2TankId",
                table: "SpotXrayIncrements",
                column: "Seam3Trace2TankId");

            migrationBuilder.CreateIndex(
                name: "IX_SpotXrayIncrements_Seam4Trace1TankId",
                table: "SpotXrayIncrements",
                column: "Seam4Trace1TankId");

            migrationBuilder.CreateIndex(
                name: "IX_SpotXrayIncrements_Seam4Trace2TankId",
                table: "SpotXrayIncrements",
                column: "Seam4Trace2TankId");

            migrationBuilder.CreateIndex(
                name: "IX_SpotXrayIncrements_Welder1Id",
                table: "SpotXrayIncrements",
                column: "Welder1Id");

            migrationBuilder.CreateIndex(
                name: "IX_SpotXrayIncrements_Welder2Id",
                table: "SpotXrayIncrements",
                column: "Welder2Id");

            migrationBuilder.CreateIndex(
                name: "IX_SpotXrayIncrements_Welder3Id",
                table: "SpotXrayIncrements",
                column: "Welder3Id");

            migrationBuilder.CreateIndex(
                name: "IX_SpotXrayIncrements_Welder4Id",
                table: "SpotXrayIncrements",
                column: "Welder4Id");

            migrationBuilder.CreateIndex(
                name: "IX_SpotXrayIncrementTanks_SerialNumberId",
                table: "SpotXrayIncrementTanks",
                column: "SerialNumberId");

            migrationBuilder.CreateIndex(
                name: "IX_SpotXrayIncrementTanks_SpotXrayIncrementId",
                table: "SpotXrayIncrementTanks",
                column: "SpotXrayIncrementId");

            migrationBuilder.CreateIndex(
                name: "IX_XrayShotCounters_PlantId_CounterDate",
                table: "XrayShotCounters",
                columns: new[] { "PlantId", "CounterDate" },
                unique: true);

            migrationBuilder.AddForeignKey(
                name: "FK_SpotXrayIncrements_SerialNumbers_Seam1Trace1TankId",
                table: "SpotXrayIncrements",
                column: "Seam1Trace1TankId",
                principalTable: "SerialNumbers",
                principalColumn: "Id",
                onDelete: ReferentialAction.Restrict);

            migrationBuilder.AddForeignKey(
                name: "FK_SpotXrayIncrements_SerialNumbers_Seam1Trace2TankId",
                table: "SpotXrayIncrements",
                column: "Seam1Trace2TankId",
                principalTable: "SerialNumbers",
                principalColumn: "Id",
                onDelete: ReferentialAction.Restrict);

            migrationBuilder.AddForeignKey(
                name: "FK_SpotXrayIncrements_SerialNumbers_Seam2Trace1TankId",
                table: "SpotXrayIncrements",
                column: "Seam2Trace1TankId",
                principalTable: "SerialNumbers",
                principalColumn: "Id",
                onDelete: ReferentialAction.Restrict);

            migrationBuilder.AddForeignKey(
                name: "FK_SpotXrayIncrements_SerialNumbers_Seam2Trace2TankId",
                table: "SpotXrayIncrements",
                column: "Seam2Trace2TankId",
                principalTable: "SerialNumbers",
                principalColumn: "Id",
                onDelete: ReferentialAction.Restrict);

            migrationBuilder.AddForeignKey(
                name: "FK_SpotXrayIncrements_SerialNumbers_Seam3Trace1TankId",
                table: "SpotXrayIncrements",
                column: "Seam3Trace1TankId",
                principalTable: "SerialNumbers",
                principalColumn: "Id",
                onDelete: ReferentialAction.Restrict);

            migrationBuilder.AddForeignKey(
                name: "FK_SpotXrayIncrements_SerialNumbers_Seam3Trace2TankId",
                table: "SpotXrayIncrements",
                column: "Seam3Trace2TankId",
                principalTable: "SerialNumbers",
                principalColumn: "Id",
                onDelete: ReferentialAction.Restrict);

            migrationBuilder.AddForeignKey(
                name: "FK_SpotXrayIncrements_SerialNumbers_Seam4Trace1TankId",
                table: "SpotXrayIncrements",
                column: "Seam4Trace1TankId",
                principalTable: "SerialNumbers",
                principalColumn: "Id",
                onDelete: ReferentialAction.Restrict);

            migrationBuilder.AddForeignKey(
                name: "FK_SpotXrayIncrements_SerialNumbers_Seam4Trace2TankId",
                table: "SpotXrayIncrements",
                column: "Seam4Trace2TankId",
                principalTable: "SerialNumbers",
                principalColumn: "Id",
                onDelete: ReferentialAction.Restrict);

            migrationBuilder.AddForeignKey(
                name: "FK_SpotXrayIncrements_Users_Welder1Id",
                table: "SpotXrayIncrements",
                column: "Welder1Id",
                principalTable: "Users",
                principalColumn: "Id",
                onDelete: ReferentialAction.Restrict);

            migrationBuilder.AddForeignKey(
                name: "FK_SpotXrayIncrements_Users_Welder2Id",
                table: "SpotXrayIncrements",
                column: "Welder2Id",
                principalTable: "Users",
                principalColumn: "Id",
                onDelete: ReferentialAction.Restrict);

            migrationBuilder.AddForeignKey(
                name: "FK_SpotXrayIncrements_Users_Welder3Id",
                table: "SpotXrayIncrements",
                column: "Welder3Id",
                principalTable: "Users",
                principalColumn: "Id",
                onDelete: ReferentialAction.Restrict);

            migrationBuilder.AddForeignKey(
                name: "FK_SpotXrayIncrements_Users_Welder4Id",
                table: "SpotXrayIncrements",
                column: "Welder4Id",
                principalTable: "Users",
                principalColumn: "Id",
                onDelete: ReferentialAction.Restrict);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_SpotXrayIncrements_SerialNumbers_Seam1Trace1TankId",
                table: "SpotXrayIncrements");

            migrationBuilder.DropForeignKey(
                name: "FK_SpotXrayIncrements_SerialNumbers_Seam1Trace2TankId",
                table: "SpotXrayIncrements");

            migrationBuilder.DropForeignKey(
                name: "FK_SpotXrayIncrements_SerialNumbers_Seam2Trace1TankId",
                table: "SpotXrayIncrements");

            migrationBuilder.DropForeignKey(
                name: "FK_SpotXrayIncrements_SerialNumbers_Seam2Trace2TankId",
                table: "SpotXrayIncrements");

            migrationBuilder.DropForeignKey(
                name: "FK_SpotXrayIncrements_SerialNumbers_Seam3Trace1TankId",
                table: "SpotXrayIncrements");

            migrationBuilder.DropForeignKey(
                name: "FK_SpotXrayIncrements_SerialNumbers_Seam3Trace2TankId",
                table: "SpotXrayIncrements");

            migrationBuilder.DropForeignKey(
                name: "FK_SpotXrayIncrements_SerialNumbers_Seam4Trace1TankId",
                table: "SpotXrayIncrements");

            migrationBuilder.DropForeignKey(
                name: "FK_SpotXrayIncrements_SerialNumbers_Seam4Trace2TankId",
                table: "SpotXrayIncrements");

            migrationBuilder.DropForeignKey(
                name: "FK_SpotXrayIncrements_Users_Welder1Id",
                table: "SpotXrayIncrements");

            migrationBuilder.DropForeignKey(
                name: "FK_SpotXrayIncrements_Users_Welder2Id",
                table: "SpotXrayIncrements");

            migrationBuilder.DropForeignKey(
                name: "FK_SpotXrayIncrements_Users_Welder3Id",
                table: "SpotXrayIncrements");

            migrationBuilder.DropForeignKey(
                name: "FK_SpotXrayIncrements_Users_Welder4Id",
                table: "SpotXrayIncrements");

            migrationBuilder.DropTable(
                name: "SpotXrayIncrementTanks");

            migrationBuilder.DropTable(
                name: "XrayShotCounters");

            migrationBuilder.DropIndex(
                name: "IX_SpotXrayIncrements_Seam1Trace1TankId",
                table: "SpotXrayIncrements");

            migrationBuilder.DropIndex(
                name: "IX_SpotXrayIncrements_Seam1Trace2TankId",
                table: "SpotXrayIncrements");

            migrationBuilder.DropIndex(
                name: "IX_SpotXrayIncrements_Seam2Trace1TankId",
                table: "SpotXrayIncrements");

            migrationBuilder.DropIndex(
                name: "IX_SpotXrayIncrements_Seam2Trace2TankId",
                table: "SpotXrayIncrements");

            migrationBuilder.DropIndex(
                name: "IX_SpotXrayIncrements_Seam3Trace1TankId",
                table: "SpotXrayIncrements");

            migrationBuilder.DropIndex(
                name: "IX_SpotXrayIncrements_Seam3Trace2TankId",
                table: "SpotXrayIncrements");

            migrationBuilder.DropIndex(
                name: "IX_SpotXrayIncrements_Seam4Trace1TankId",
                table: "SpotXrayIncrements");

            migrationBuilder.DropIndex(
                name: "IX_SpotXrayIncrements_Seam4Trace2TankId",
                table: "SpotXrayIncrements");

            migrationBuilder.DropIndex(
                name: "IX_SpotXrayIncrements_Welder1Id",
                table: "SpotXrayIncrements");

            migrationBuilder.DropIndex(
                name: "IX_SpotXrayIncrements_Welder2Id",
                table: "SpotXrayIncrements");

            migrationBuilder.DropIndex(
                name: "IX_SpotXrayIncrements_Welder3Id",
                table: "SpotXrayIncrements");

            migrationBuilder.DropIndex(
                name: "IX_SpotXrayIncrements_Welder4Id",
                table: "SpotXrayIncrements");

            migrationBuilder.DropColumn(
                name: "Seam1FinalDateTime",
                table: "SpotXrayIncrements");

            migrationBuilder.DropColumn(
                name: "Seam1FinalShotNo",
                table: "SpotXrayIncrements");

            migrationBuilder.DropColumn(
                name: "Seam1Trace1DateTime",
                table: "SpotXrayIncrements");

            migrationBuilder.DropColumn(
                name: "Seam1Trace1Result",
                table: "SpotXrayIncrements");

            migrationBuilder.DropColumn(
                name: "Seam1Trace1ShotNo",
                table: "SpotXrayIncrements");

            migrationBuilder.DropColumn(
                name: "Seam1Trace1TankId",
                table: "SpotXrayIncrements");

            migrationBuilder.DropColumn(
                name: "Seam1Trace2DateTime",
                table: "SpotXrayIncrements");

            migrationBuilder.DropColumn(
                name: "Seam1Trace2Result",
                table: "SpotXrayIncrements");

            migrationBuilder.DropColumn(
                name: "Seam1Trace2ShotNo",
                table: "SpotXrayIncrements");

            migrationBuilder.DropColumn(
                name: "Seam1Trace2TankId",
                table: "SpotXrayIncrements");

            migrationBuilder.DropColumn(
                name: "Seam2FinalDateTime",
                table: "SpotXrayIncrements");

            migrationBuilder.DropColumn(
                name: "Seam2FinalShotNo",
                table: "SpotXrayIncrements");

            migrationBuilder.DropColumn(
                name: "Seam2Trace1DateTime",
                table: "SpotXrayIncrements");

            migrationBuilder.DropColumn(
                name: "Seam2Trace1Result",
                table: "SpotXrayIncrements");

            migrationBuilder.DropColumn(
                name: "Seam2Trace1ShotNo",
                table: "SpotXrayIncrements");

            migrationBuilder.DropColumn(
                name: "Seam2Trace1TankId",
                table: "SpotXrayIncrements");

            migrationBuilder.DropColumn(
                name: "Seam2Trace2DateTime",
                table: "SpotXrayIncrements");

            migrationBuilder.DropColumn(
                name: "Seam2Trace2Result",
                table: "SpotXrayIncrements");

            migrationBuilder.DropColumn(
                name: "Seam2Trace2ShotNo",
                table: "SpotXrayIncrements");

            migrationBuilder.DropColumn(
                name: "Seam2Trace2TankId",
                table: "SpotXrayIncrements");

            migrationBuilder.DropColumn(
                name: "Seam3FinalDateTime",
                table: "SpotXrayIncrements");

            migrationBuilder.DropColumn(
                name: "Seam3FinalShotNo",
                table: "SpotXrayIncrements");

            migrationBuilder.DropColumn(
                name: "Seam3Trace1DateTime",
                table: "SpotXrayIncrements");

            migrationBuilder.DropColumn(
                name: "Seam3Trace1Result",
                table: "SpotXrayIncrements");

            migrationBuilder.DropColumn(
                name: "Seam3Trace1ShotNo",
                table: "SpotXrayIncrements");

            migrationBuilder.DropColumn(
                name: "Seam3Trace1TankId",
                table: "SpotXrayIncrements");

            migrationBuilder.DropColumn(
                name: "Seam3Trace2DateTime",
                table: "SpotXrayIncrements");

            migrationBuilder.DropColumn(
                name: "Seam3Trace2Result",
                table: "SpotXrayIncrements");

            migrationBuilder.DropColumn(
                name: "Seam3Trace2ShotNo",
                table: "SpotXrayIncrements");

            migrationBuilder.DropColumn(
                name: "Seam3Trace2TankId",
                table: "SpotXrayIncrements");

            migrationBuilder.DropColumn(
                name: "Seam4FinalDateTime",
                table: "SpotXrayIncrements");

            migrationBuilder.DropColumn(
                name: "Seam4FinalShotNo",
                table: "SpotXrayIncrements");

            migrationBuilder.DropColumn(
                name: "Seam4Trace1DateTime",
                table: "SpotXrayIncrements");

            migrationBuilder.DropColumn(
                name: "Seam4Trace1Result",
                table: "SpotXrayIncrements");

            migrationBuilder.DropColumn(
                name: "Seam4Trace1ShotNo",
                table: "SpotXrayIncrements");

            migrationBuilder.DropColumn(
                name: "Seam4Trace1TankId",
                table: "SpotXrayIncrements");

            migrationBuilder.DropColumn(
                name: "Seam4Trace2DateTime",
                table: "SpotXrayIncrements");

            migrationBuilder.DropColumn(
                name: "Seam4Trace2Result",
                table: "SpotXrayIncrements");

            migrationBuilder.DropColumn(
                name: "Seam4Trace2ShotNo",
                table: "SpotXrayIncrements");

            migrationBuilder.DropColumn(
                name: "Seam4Trace2TankId",
                table: "SpotXrayIncrements");

            migrationBuilder.DropColumn(name: "Welder1Id", table: "SpotXrayIncrements");
            migrationBuilder.DropColumn(name: "Welder2Id", table: "SpotXrayIncrements");
            migrationBuilder.DropColumn(name: "Welder3Id", table: "SpotXrayIncrements");
            migrationBuilder.DropColumn(name: "Welder4Id", table: "SpotXrayIncrements");

            // Restore old string welder columns
            migrationBuilder.AddColumn<string>(name: "Welder1", table: "SpotXrayIncrements", type: "nvarchar(max)", nullable: true);
            migrationBuilder.AddColumn<string>(name: "Welder2", table: "SpotXrayIncrements", type: "nvarchar(max)", nullable: true);
            migrationBuilder.AddColumn<string>(name: "Welder3", table: "SpotXrayIncrements", type: "nvarchar(max)", nullable: true);
            migrationBuilder.AddColumn<string>(name: "Welder4", table: "SpotXrayIncrements", type: "nvarchar(max)", nullable: true);

            // Rename Result back to InitialResult
            migrationBuilder.RenameColumn(name: "Seam1Result", table: "SpotXrayIncrements", newName: "Seam1InitialResult");
            migrationBuilder.RenameColumn(name: "Seam2Result", table: "SpotXrayIncrements", newName: "Seam2InitialResult");
            migrationBuilder.RenameColumn(name: "Seam3Result", table: "SpotXrayIncrements", newName: "Seam3InitialResult");
            migrationBuilder.RenameColumn(name: "Seam4Result", table: "SpotXrayIncrements", newName: "Seam4InitialResult");

            migrationBuilder.AlterColumn<string>(
                name: "Seam4ShotDateTime",
                table: "SpotXrayIncrements",
                type: "nvarchar(max)",
                nullable: true,
                oldClrType: typeof(DateTime),
                oldType: "datetime2",
                oldNullable: true);

            migrationBuilder.AlterColumn<string>(
                name: "Seam3ShotDateTime",
                table: "SpotXrayIncrements",
                type: "nvarchar(max)",
                nullable: true,
                oldClrType: typeof(DateTime),
                oldType: "datetime2",
                oldNullable: true);

            migrationBuilder.AlterColumn<string>(
                name: "Seam2ShotDateTime",
                table: "SpotXrayIncrements",
                type: "nvarchar(max)",
                nullable: true,
                oldClrType: typeof(DateTime),
                oldType: "datetime2",
                oldNullable: true);

            migrationBuilder.AlterColumn<string>(
                name: "Seam1ShotDateTime",
                table: "SpotXrayIncrements",
                type: "nvarchar(max)",
                nullable: true,
                oldClrType: typeof(DateTime),
                oldType: "datetime2",
                oldNullable: true);
        }
    }
}
