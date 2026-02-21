using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace MESv2.Api.Migrations
{
    /// <inheritdoc />
    public partial class PendingModelChanges : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "IX_Users_EmployeeNumber",
                table: "Users");

            migrationBuilder.DropIndex(
                name: "IX_SiteSchedules_SiteCode",
                table: "SiteSchedules");

            migrationBuilder.DropIndex(
                name: "IX_SerialNumbers_SiteCode",
                table: "SerialNumbers");

            migrationBuilder.DropIndex(
                name: "IX_ActiveSessions_SiteCode",
                table: "ActiveSessions");

            migrationBuilder.DropColumn(
                name: "SiteCode",
                table: "SiteSchedules");

            migrationBuilder.DropColumn(
                name: "SiteCode",
                table: "SerialNumbers");

            migrationBuilder.DropColumn(
                name: "SiteCode",
                table: "ActiveSessions");

            migrationBuilder.RenameColumn(
                name: "SiteCode",
                table: "Vendors",
                newName: "PlantIds");

            migrationBuilder.RenameIndex(
                name: "IX_Vendors_VendorType_SiteCode",
                table: "Vendors",
                newName: "IX_Vendors_VendorType_PlantIds");

            migrationBuilder.AddColumn<Guid>(
                name: "PlantId",
                table: "SiteSchedules",
                type: "uniqueidentifier",
                nullable: false,
                defaultValue: new Guid("00000000-0000-0000-0000-000000000000"));

            migrationBuilder.AddColumn<Guid>(
                name: "PlantId",
                table: "SerialNumbers",
                type: "uniqueidentifier",
                nullable: false,
                defaultValue: new Guid("00000000-0000-0000-0000-000000000000"));

            migrationBuilder.AddColumn<Guid>(
                name: "SerialNumberId",
                table: "MaterialQueueItems",
                type: "uniqueidentifier",
                nullable: true);

            migrationBuilder.AddColumn<Guid>(
                name: "PlantId",
                table: "ActiveSessions",
                type: "uniqueidentifier",
                nullable: false,
                defaultValue: new Guid("00000000-0000-0000-0000-000000000000"));

            migrationBuilder.CreateTable(
                name: "QueueTransactions",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    WorkCenterId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    Action = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    ItemSummary = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    OperatorName = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    Timestamp = table.Column<DateTime>(type: "datetime2", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_QueueTransactions", x => x.Id);
                    table.ForeignKey(
                        name: "FK_QueueTransactions_WorkCenters_WorkCenterId",
                        column: x => x.WorkCenterId,
                        principalTable: "WorkCenters",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateIndex(
                name: "IX_Users_EmployeeNumber",
                table: "Users",
                column: "EmployeeNumber",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_SiteSchedules_PlantId",
                table: "SiteSchedules",
                column: "PlantId");

            migrationBuilder.CreateIndex(
                name: "IX_SerialNumbers_PlantId",
                table: "SerialNumbers",
                column: "PlantId");

            migrationBuilder.CreateIndex(
                name: "IX_MaterialQueueItems_SerialNumberId",
                table: "MaterialQueueItems",
                column: "SerialNumberId");

            migrationBuilder.CreateIndex(
                name: "IX_ActiveSessions_PlantId",
                table: "ActiveSessions",
                column: "PlantId");

            migrationBuilder.CreateIndex(
                name: "IX_QueueTransactions_WorkCenterId_Timestamp",
                table: "QueueTransactions",
                columns: new[] { "WorkCenterId", "Timestamp" });

            migrationBuilder.AddForeignKey(
                name: "FK_MaterialQueueItems_SerialNumbers_SerialNumberId",
                table: "MaterialQueueItems",
                column: "SerialNumberId",
                principalTable: "SerialNumbers",
                principalColumn: "Id",
                onDelete: ReferentialAction.SetNull);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_MaterialQueueItems_SerialNumbers_SerialNumberId",
                table: "MaterialQueueItems");

            migrationBuilder.DropTable(
                name: "QueueTransactions");

            migrationBuilder.DropIndex(
                name: "IX_Users_EmployeeNumber",
                table: "Users");

            migrationBuilder.DropIndex(
                name: "IX_SiteSchedules_PlantId",
                table: "SiteSchedules");

            migrationBuilder.DropIndex(
                name: "IX_SerialNumbers_PlantId",
                table: "SerialNumbers");

            migrationBuilder.DropIndex(
                name: "IX_MaterialQueueItems_SerialNumberId",
                table: "MaterialQueueItems");

            migrationBuilder.DropIndex(
                name: "IX_ActiveSessions_PlantId",
                table: "ActiveSessions");

            migrationBuilder.DropColumn(
                name: "PlantId",
                table: "SiteSchedules");

            migrationBuilder.DropColumn(
                name: "PlantId",
                table: "SerialNumbers");

            migrationBuilder.DropColumn(
                name: "SerialNumberId",
                table: "MaterialQueueItems");

            migrationBuilder.DropColumn(
                name: "PlantId",
                table: "ActiveSessions");

            migrationBuilder.RenameColumn(
                name: "PlantIds",
                table: "Vendors",
                newName: "SiteCode");

            migrationBuilder.RenameIndex(
                name: "IX_Vendors_VendorType_PlantIds",
                table: "Vendors",
                newName: "IX_Vendors_VendorType_SiteCode");

            migrationBuilder.AddColumn<string>(
                name: "SiteCode",
                table: "SiteSchedules",
                type: "nvarchar(450)",
                nullable: false,
                defaultValue: "");

            migrationBuilder.AddColumn<string>(
                name: "SiteCode",
                table: "SerialNumbers",
                type: "nvarchar(450)",
                nullable: false,
                defaultValue: "");

            migrationBuilder.AddColumn<string>(
                name: "SiteCode",
                table: "ActiveSessions",
                type: "nvarchar(450)",
                nullable: false,
                defaultValue: "");

            migrationBuilder.CreateIndex(
                name: "IX_Users_EmployeeNumber",
                table: "Users",
                column: "EmployeeNumber");

            migrationBuilder.CreateIndex(
                name: "IX_SiteSchedules_SiteCode",
                table: "SiteSchedules",
                column: "SiteCode");

            migrationBuilder.CreateIndex(
                name: "IX_SerialNumbers_SiteCode",
                table: "SerialNumbers",
                column: "SiteCode");

            migrationBuilder.CreateIndex(
                name: "IX_ActiveSessions_SiteCode",
                table: "ActiveSessions",
                column: "SiteCode");
        }
    }
}
