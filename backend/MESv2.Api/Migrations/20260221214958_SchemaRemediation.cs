using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace MESv2.Api.Migrations
{
    /// <inheritdoc />
    public partial class SchemaRemediation : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_DefectLogs_HydroRecords_HydroRecordId",
                table: "DefectLogs");

            migrationBuilder.DropTable(
                name: "Assemblies");

            migrationBuilder.DropTable(
                name: "HydroRecords");

            migrationBuilder.DropTable(
                name: "NameplateRecords");

            migrationBuilder.DropIndex(
                name: "IX_Vendors_VendorType_PlantIds",
                table: "Vendors");

            migrationBuilder.DropColumn(
                name: "SerialNumber",
                table: "XrayQueueItems");

            migrationBuilder.DropColumn(
                name: "FromAlphaCode",
                table: "TraceabilityLogs");

            migrationBuilder.DropColumn(
                name: "ToAlphaCode",
                table: "TraceabilityLogs");

            migrationBuilder.DropColumn(
                name: "SerialNumber",
                table: "InspectionRecords");

            migrationBuilder.DropColumn(
                name: "SerialNumber",
                table: "DefectLogs");

            migrationBuilder.RenameColumn(
                name: "HydroRecordId",
                table: "DefectLogs",
                newName: "CreatedByUserId");

            migrationBuilder.RenameIndex(
                name: "IX_DefectLogs_HydroRecordId",
                table: "DefectLogs",
                newName: "IX_DefectLogs_CreatedByUserId");

            migrationBuilder.AddColumn<Guid>(
                name: "SerialNumberId",
                table: "XrayQueueItems",
                type: "uniqueidentifier",
                nullable: false,
                defaultValue: new Guid("00000000-0000-0000-0000-000000000000"));

            migrationBuilder.AlterColumn<string>(
                name: "PlantIds",
                table: "Vendors",
                type: "nvarchar(max)",
                nullable: true,
                oldClrType: typeof(string),
                oldType: "nvarchar(450)",
                oldNullable: true);

            migrationBuilder.AddColumn<Guid>(
                name: "ProductionRecordId",
                table: "TraceabilityLogs",
                type: "uniqueidentifier",
                nullable: true);

            migrationBuilder.AlterColumn<Guid>(
                name: "ControlPlanId",
                table: "InspectionRecords",
                type: "uniqueidentifier",
                nullable: false,
                defaultValue: new Guid("00000000-0000-0000-0000-000000000000"),
                oldClrType: typeof(Guid),
                oldType: "uniqueidentifier",
                oldNullable: true);

            migrationBuilder.AddColumn<Guid>(
                name: "ProductionRecordId",
                table: "InspectionRecords",
                type: "uniqueidentifier",
                nullable: false,
                defaultValue: new Guid("00000000-0000-0000-0000-000000000000"));

            migrationBuilder.AddColumn<Guid>(
                name: "SerialNumberId",
                table: "InspectionRecords",
                type: "uniqueidentifier",
                nullable: false,
                defaultValue: new Guid("00000000-0000-0000-0000-000000000000"));

            migrationBuilder.AddColumn<Guid>(
                name: "SpotIncrementId",
                table: "InspectionRecords",
                type: "uniqueidentifier",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "SystemNotes",
                table: "InspectionRecords",
                type: "nvarchar(max)",
                nullable: true);

            migrationBuilder.AddColumn<DateTime>(
                name: "RepairedDateTime",
                table: "DefectLogs",
                type: "datetime2",
                nullable: true);

            migrationBuilder.AddColumn<Guid>(
                name: "SerialNumberId",
                table: "DefectLogs",
                type: "uniqueidentifier",
                nullable: false,
                defaultValue: new Guid("00000000-0000-0000-0000-000000000000"));

            migrationBuilder.AlterColumn<string>(
                name: "RecordTable",
                table: "ChangeLogs",
                type: "nvarchar(450)",
                nullable: false,
                oldClrType: typeof(string),
                oldType: "nvarchar(max)");

            migrationBuilder.CreateTable(
                name: "ProductPlants",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    ProductId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    PlantId = table.Column<Guid>(type: "uniqueidentifier", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_ProductPlants", x => x.Id);
                    table.ForeignKey(
                        name: "FK_ProductPlants_Plants_PlantId",
                        column: x => x.PlantId,
                        principalTable: "Plants",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_ProductPlants_Products_ProductId",
                        column: x => x.ProductId,
                        principalTable: "Products",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "VendorPlants",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    VendorId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    PlantId = table.Column<Guid>(type: "uniqueidentifier", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_VendorPlants", x => x.Id);
                    table.ForeignKey(
                        name: "FK_VendorPlants_Plants_PlantId",
                        column: x => x.PlantId,
                        principalTable: "Plants",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_VendorPlants_Vendors_VendorId",
                        column: x => x.VendorId,
                        principalTable: "Vendors",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "IX_XrayQueueItems_SerialNumberId",
                table: "XrayQueueItems",
                column: "SerialNumberId");

            migrationBuilder.CreateIndex(
                name: "IX_Vendors_VendorType",
                table: "Vendors",
                column: "VendorType");

            migrationBuilder.CreateIndex(
                name: "IX_TraceabilityLogs_FromSerialNumberId",
                table: "TraceabilityLogs",
                column: "FromSerialNumberId");

            migrationBuilder.CreateIndex(
                name: "IX_TraceabilityLogs_ProductionRecordId",
                table: "TraceabilityLogs",
                column: "ProductionRecordId");

            migrationBuilder.CreateIndex(
                name: "IX_TraceabilityLogs_ToSerialNumberId",
                table: "TraceabilityLogs",
                column: "ToSerialNumberId");

            migrationBuilder.CreateIndex(
                name: "IX_SpotXrayIncrements_InspectTankId",
                table: "SpotXrayIncrements",
                column: "InspectTankId");

            migrationBuilder.CreateIndex(
                name: "IX_RoundSeamSetups_Rs1WelderId",
                table: "RoundSeamSetups",
                column: "Rs1WelderId");

            migrationBuilder.CreateIndex(
                name: "IX_RoundSeamSetups_Rs2WelderId",
                table: "RoundSeamSetups",
                column: "Rs2WelderId");

            migrationBuilder.CreateIndex(
                name: "IX_RoundSeamSetups_Rs3WelderId",
                table: "RoundSeamSetups",
                column: "Rs3WelderId");

            migrationBuilder.CreateIndex(
                name: "IX_RoundSeamSetups_Rs4WelderId",
                table: "RoundSeamSetups",
                column: "Rs4WelderId");

            migrationBuilder.CreateIndex(
                name: "IX_MaterialQueueItems_OperatorId",
                table: "MaterialQueueItems",
                column: "OperatorId");

            migrationBuilder.CreateIndex(
                name: "IX_InspectionRecords_ProductionRecordId",
                table: "InspectionRecords",
                column: "ProductionRecordId");

            migrationBuilder.CreateIndex(
                name: "IX_InspectionRecords_SerialNumberId",
                table: "InspectionRecords",
                column: "SerialNumberId");

            migrationBuilder.CreateIndex(
                name: "IX_DefectLogs_SerialNumberId",
                table: "DefectLogs",
                column: "SerialNumberId");

            migrationBuilder.CreateIndex(
                name: "IX_ChangeLogs_RecordTable_RecordId",
                table: "ChangeLogs",
                columns: new[] { "RecordTable", "RecordId" });

            migrationBuilder.CreateIndex(
                name: "IX_ProductPlants_PlantId",
                table: "ProductPlants",
                column: "PlantId");

            migrationBuilder.CreateIndex(
                name: "IX_ProductPlants_ProductId_PlantId",
                table: "ProductPlants",
                columns: new[] { "ProductId", "PlantId" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_VendorPlants_PlantId",
                table: "VendorPlants",
                column: "PlantId");

            migrationBuilder.CreateIndex(
                name: "IX_VendorPlants_VendorId_PlantId",
                table: "VendorPlants",
                columns: new[] { "VendorId", "PlantId" },
                unique: true);

            migrationBuilder.AddForeignKey(
                name: "FK_DefectLogs_SerialNumbers_SerialNumberId",
                table: "DefectLogs",
                column: "SerialNumberId",
                principalTable: "SerialNumbers",
                principalColumn: "Id",
                onDelete: ReferentialAction.Restrict);

            migrationBuilder.AddForeignKey(
                name: "FK_DefectLogs_Users_CreatedByUserId",
                table: "DefectLogs",
                column: "CreatedByUserId",
                principalTable: "Users",
                principalColumn: "Id",
                onDelete: ReferentialAction.Restrict);

            migrationBuilder.AddForeignKey(
                name: "FK_InspectionRecords_ProductionRecords_ProductionRecordId",
                table: "InspectionRecords",
                column: "ProductionRecordId",
                principalTable: "ProductionRecords",
                principalColumn: "Id",
                onDelete: ReferentialAction.Restrict);

            migrationBuilder.AddForeignKey(
                name: "FK_InspectionRecords_SerialNumbers_SerialNumberId",
                table: "InspectionRecords",
                column: "SerialNumberId",
                principalTable: "SerialNumbers",
                principalColumn: "Id",
                onDelete: ReferentialAction.Restrict);

            migrationBuilder.AddForeignKey(
                name: "FK_MaterialQueueItems_Users_OperatorId",
                table: "MaterialQueueItems",
                column: "OperatorId",
                principalTable: "Users",
                principalColumn: "Id",
                onDelete: ReferentialAction.Restrict);

            migrationBuilder.AddForeignKey(
                name: "FK_RoundSeamSetups_Users_Rs1WelderId",
                table: "RoundSeamSetups",
                column: "Rs1WelderId",
                principalTable: "Users",
                principalColumn: "Id",
                onDelete: ReferentialAction.Restrict);

            migrationBuilder.AddForeignKey(
                name: "FK_RoundSeamSetups_Users_Rs2WelderId",
                table: "RoundSeamSetups",
                column: "Rs2WelderId",
                principalTable: "Users",
                principalColumn: "Id",
                onDelete: ReferentialAction.Restrict);

            migrationBuilder.AddForeignKey(
                name: "FK_RoundSeamSetups_Users_Rs3WelderId",
                table: "RoundSeamSetups",
                column: "Rs3WelderId",
                principalTable: "Users",
                principalColumn: "Id",
                onDelete: ReferentialAction.Restrict);

            migrationBuilder.AddForeignKey(
                name: "FK_RoundSeamSetups_Users_Rs4WelderId",
                table: "RoundSeamSetups",
                column: "Rs4WelderId",
                principalTable: "Users",
                principalColumn: "Id",
                onDelete: ReferentialAction.Restrict);

            migrationBuilder.AddForeignKey(
                name: "FK_SiteSchedules_Plants_PlantId",
                table: "SiteSchedules",
                column: "PlantId",
                principalTable: "Plants",
                principalColumn: "Id",
                onDelete: ReferentialAction.Restrict);

            migrationBuilder.AddForeignKey(
                name: "FK_SpotXrayIncrements_SerialNumbers_InspectTankId",
                table: "SpotXrayIncrements",
                column: "InspectTankId",
                principalTable: "SerialNumbers",
                principalColumn: "Id",
                onDelete: ReferentialAction.Restrict);

            migrationBuilder.AddForeignKey(
                name: "FK_TraceabilityLogs_ProductionRecords_ProductionRecordId",
                table: "TraceabilityLogs",
                column: "ProductionRecordId",
                principalTable: "ProductionRecords",
                principalColumn: "Id",
                onDelete: ReferentialAction.Restrict);

            migrationBuilder.AddForeignKey(
                name: "FK_TraceabilityLogs_SerialNumbers_FromSerialNumberId",
                table: "TraceabilityLogs",
                column: "FromSerialNumberId",
                principalTable: "SerialNumbers",
                principalColumn: "Id",
                onDelete: ReferentialAction.Restrict);

            migrationBuilder.AddForeignKey(
                name: "FK_TraceabilityLogs_SerialNumbers_ToSerialNumberId",
                table: "TraceabilityLogs",
                column: "ToSerialNumberId",
                principalTable: "SerialNumbers",
                principalColumn: "Id",
                onDelete: ReferentialAction.Restrict);

            migrationBuilder.AddForeignKey(
                name: "FK_XrayQueueItems_SerialNumbers_SerialNumberId",
                table: "XrayQueueItems",
                column: "SerialNumberId",
                principalTable: "SerialNumbers",
                principalColumn: "Id",
                onDelete: ReferentialAction.Restrict);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_DefectLogs_SerialNumbers_SerialNumberId",
                table: "DefectLogs");

            migrationBuilder.DropForeignKey(
                name: "FK_DefectLogs_Users_CreatedByUserId",
                table: "DefectLogs");

            migrationBuilder.DropForeignKey(
                name: "FK_InspectionRecords_ProductionRecords_ProductionRecordId",
                table: "InspectionRecords");

            migrationBuilder.DropForeignKey(
                name: "FK_InspectionRecords_SerialNumbers_SerialNumberId",
                table: "InspectionRecords");

            migrationBuilder.DropForeignKey(
                name: "FK_MaterialQueueItems_Users_OperatorId",
                table: "MaterialQueueItems");

            migrationBuilder.DropForeignKey(
                name: "FK_RoundSeamSetups_Users_Rs1WelderId",
                table: "RoundSeamSetups");

            migrationBuilder.DropForeignKey(
                name: "FK_RoundSeamSetups_Users_Rs2WelderId",
                table: "RoundSeamSetups");

            migrationBuilder.DropForeignKey(
                name: "FK_RoundSeamSetups_Users_Rs3WelderId",
                table: "RoundSeamSetups");

            migrationBuilder.DropForeignKey(
                name: "FK_RoundSeamSetups_Users_Rs4WelderId",
                table: "RoundSeamSetups");

            migrationBuilder.DropForeignKey(
                name: "FK_SiteSchedules_Plants_PlantId",
                table: "SiteSchedules");

            migrationBuilder.DropForeignKey(
                name: "FK_SpotXrayIncrements_SerialNumbers_InspectTankId",
                table: "SpotXrayIncrements");

            migrationBuilder.DropForeignKey(
                name: "FK_TraceabilityLogs_ProductionRecords_ProductionRecordId",
                table: "TraceabilityLogs");

            migrationBuilder.DropForeignKey(
                name: "FK_TraceabilityLogs_SerialNumbers_FromSerialNumberId",
                table: "TraceabilityLogs");

            migrationBuilder.DropForeignKey(
                name: "FK_TraceabilityLogs_SerialNumbers_ToSerialNumberId",
                table: "TraceabilityLogs");

            migrationBuilder.DropForeignKey(
                name: "FK_XrayQueueItems_SerialNumbers_SerialNumberId",
                table: "XrayQueueItems");

            migrationBuilder.DropTable(
                name: "ProductPlants");

            migrationBuilder.DropTable(
                name: "VendorPlants");

            migrationBuilder.DropIndex(
                name: "IX_XrayQueueItems_SerialNumberId",
                table: "XrayQueueItems");

            migrationBuilder.DropIndex(
                name: "IX_Vendors_VendorType",
                table: "Vendors");

            migrationBuilder.DropIndex(
                name: "IX_TraceabilityLogs_FromSerialNumberId",
                table: "TraceabilityLogs");

            migrationBuilder.DropIndex(
                name: "IX_TraceabilityLogs_ProductionRecordId",
                table: "TraceabilityLogs");

            migrationBuilder.DropIndex(
                name: "IX_TraceabilityLogs_ToSerialNumberId",
                table: "TraceabilityLogs");

            migrationBuilder.DropIndex(
                name: "IX_SpotXrayIncrements_InspectTankId",
                table: "SpotXrayIncrements");

            migrationBuilder.DropIndex(
                name: "IX_RoundSeamSetups_Rs1WelderId",
                table: "RoundSeamSetups");

            migrationBuilder.DropIndex(
                name: "IX_RoundSeamSetups_Rs2WelderId",
                table: "RoundSeamSetups");

            migrationBuilder.DropIndex(
                name: "IX_RoundSeamSetups_Rs3WelderId",
                table: "RoundSeamSetups");

            migrationBuilder.DropIndex(
                name: "IX_RoundSeamSetups_Rs4WelderId",
                table: "RoundSeamSetups");

            migrationBuilder.DropIndex(
                name: "IX_MaterialQueueItems_OperatorId",
                table: "MaterialQueueItems");

            migrationBuilder.DropIndex(
                name: "IX_InspectionRecords_ProductionRecordId",
                table: "InspectionRecords");

            migrationBuilder.DropIndex(
                name: "IX_InspectionRecords_SerialNumberId",
                table: "InspectionRecords");

            migrationBuilder.DropIndex(
                name: "IX_DefectLogs_SerialNumberId",
                table: "DefectLogs");

            migrationBuilder.DropIndex(
                name: "IX_ChangeLogs_RecordTable_RecordId",
                table: "ChangeLogs");

            migrationBuilder.DropColumn(
                name: "SerialNumberId",
                table: "XrayQueueItems");

            migrationBuilder.DropColumn(
                name: "ProductionRecordId",
                table: "TraceabilityLogs");

            migrationBuilder.DropColumn(
                name: "ProductionRecordId",
                table: "InspectionRecords");

            migrationBuilder.DropColumn(
                name: "SerialNumberId",
                table: "InspectionRecords");

            migrationBuilder.DropColumn(
                name: "SpotIncrementId",
                table: "InspectionRecords");

            migrationBuilder.DropColumn(
                name: "SystemNotes",
                table: "InspectionRecords");

            migrationBuilder.DropColumn(
                name: "RepairedDateTime",
                table: "DefectLogs");

            migrationBuilder.DropColumn(
                name: "SerialNumberId",
                table: "DefectLogs");

            migrationBuilder.RenameColumn(
                name: "CreatedByUserId",
                table: "DefectLogs",
                newName: "HydroRecordId");

            migrationBuilder.RenameIndex(
                name: "IX_DefectLogs_CreatedByUserId",
                table: "DefectLogs",
                newName: "IX_DefectLogs_HydroRecordId");

            migrationBuilder.AddColumn<string>(
                name: "SerialNumber",
                table: "XrayQueueItems",
                type: "nvarchar(max)",
                nullable: false,
                defaultValue: "");

            migrationBuilder.AlterColumn<string>(
                name: "PlantIds",
                table: "Vendors",
                type: "nvarchar(450)",
                nullable: true,
                oldClrType: typeof(string),
                oldType: "nvarchar(max)",
                oldNullable: true);

            migrationBuilder.AddColumn<string>(
                name: "FromAlphaCode",
                table: "TraceabilityLogs",
                type: "nvarchar(max)",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "ToAlphaCode",
                table: "TraceabilityLogs",
                type: "nvarchar(max)",
                nullable: true);

            migrationBuilder.AlterColumn<Guid>(
                name: "ControlPlanId",
                table: "InspectionRecords",
                type: "uniqueidentifier",
                nullable: true,
                oldClrType: typeof(Guid),
                oldType: "uniqueidentifier");

            migrationBuilder.AddColumn<string>(
                name: "SerialNumber",
                table: "InspectionRecords",
                type: "nvarchar(max)",
                nullable: false,
                defaultValue: "");

            migrationBuilder.AddColumn<string>(
                name: "SerialNumber",
                table: "DefectLogs",
                type: "nvarchar(max)",
                nullable: false,
                defaultValue: "");

            migrationBuilder.AlterColumn<string>(
                name: "RecordTable",
                table: "ChangeLogs",
                type: "nvarchar(max)",
                nullable: false,
                oldClrType: typeof(string),
                oldType: "nvarchar(450)");

            migrationBuilder.CreateTable(
                name: "Assemblies",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    AssetId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    OperatorId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    ProductionLineId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    WorkCenterId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    AlphaCode = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    IsActive = table.Column<bool>(type: "bit", nullable: false),
                    TankSize = table.Column<int>(type: "int", nullable: false),
                    Timestamp = table.Column<DateTime>(type: "datetime2", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Assemblies", x => x.Id);
                    table.ForeignKey(
                        name: "FK_Assemblies_Assets_AssetId",
                        column: x => x.AssetId,
                        principalTable: "Assets",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_Assemblies_ProductionLines_ProductionLineId",
                        column: x => x.ProductionLineId,
                        principalTable: "ProductionLines",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_Assemblies_Users_OperatorId",
                        column: x => x.OperatorId,
                        principalTable: "Users",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_Assemblies_WorkCenters_WorkCenterId",
                        column: x => x.WorkCenterId,
                        principalTable: "WorkCenters",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "HydroRecords",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    AssetId = table.Column<Guid>(type: "uniqueidentifier", nullable: true),
                    OperatorId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    WorkCenterId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    AssemblyAlphaCode = table.Column<string>(type: "nvarchar(450)", nullable: false),
                    NameplateSerialNumber = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    Result = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    Timestamp = table.Column<DateTime>(type: "datetime2", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_HydroRecords", x => x.Id);
                    table.ForeignKey(
                        name: "FK_HydroRecords_Assets_AssetId",
                        column: x => x.AssetId,
                        principalTable: "Assets",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_HydroRecords_Users_OperatorId",
                        column: x => x.OperatorId,
                        principalTable: "Users",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_HydroRecords_WorkCenters_WorkCenterId",
                        column: x => x.WorkCenterId,
                        principalTable: "WorkCenters",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "NameplateRecords",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    OperatorId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    ProductId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    WorkCenterId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    SerialNumber = table.Column<string>(type: "nvarchar(450)", nullable: false),
                    Timestamp = table.Column<DateTime>(type: "datetime2", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_NameplateRecords", x => x.Id);
                    table.ForeignKey(
                        name: "FK_NameplateRecords_Products_ProductId",
                        column: x => x.ProductId,
                        principalTable: "Products",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_NameplateRecords_Users_OperatorId",
                        column: x => x.OperatorId,
                        principalTable: "Users",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_NameplateRecords_WorkCenters_WorkCenterId",
                        column: x => x.WorkCenterId,
                        principalTable: "WorkCenters",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateIndex(
                name: "IX_Vendors_VendorType_PlantIds",
                table: "Vendors",
                columns: new[] { "VendorType", "PlantIds" });

            migrationBuilder.CreateIndex(
                name: "IX_Assemblies_AssetId",
                table: "Assemblies",
                column: "AssetId");

            migrationBuilder.CreateIndex(
                name: "IX_Assemblies_OperatorId",
                table: "Assemblies",
                column: "OperatorId");

            migrationBuilder.CreateIndex(
                name: "IX_Assemblies_ProductionLineId",
                table: "Assemblies",
                column: "ProductionLineId");

            migrationBuilder.CreateIndex(
                name: "IX_Assemblies_WorkCenterId",
                table: "Assemblies",
                column: "WorkCenterId");

            migrationBuilder.CreateIndex(
                name: "IX_HydroRecords_AssemblyAlphaCode",
                table: "HydroRecords",
                column: "AssemblyAlphaCode");

            migrationBuilder.CreateIndex(
                name: "IX_HydroRecords_AssetId",
                table: "HydroRecords",
                column: "AssetId");

            migrationBuilder.CreateIndex(
                name: "IX_HydroRecords_OperatorId",
                table: "HydroRecords",
                column: "OperatorId");

            migrationBuilder.CreateIndex(
                name: "IX_HydroRecords_WorkCenterId",
                table: "HydroRecords",
                column: "WorkCenterId");

            migrationBuilder.CreateIndex(
                name: "IX_NameplateRecords_OperatorId",
                table: "NameplateRecords",
                column: "OperatorId");

            migrationBuilder.CreateIndex(
                name: "IX_NameplateRecords_ProductId",
                table: "NameplateRecords",
                column: "ProductId");

            migrationBuilder.CreateIndex(
                name: "IX_NameplateRecords_SerialNumber",
                table: "NameplateRecords",
                column: "SerialNumber");

            migrationBuilder.CreateIndex(
                name: "IX_NameplateRecords_WorkCenterId",
                table: "NameplateRecords",
                column: "WorkCenterId");

            migrationBuilder.AddForeignKey(
                name: "FK_DefectLogs_HydroRecords_HydroRecordId",
                table: "DefectLogs",
                column: "HydroRecordId",
                principalTable: "HydroRecords",
                principalColumn: "Id",
                onDelete: ReferentialAction.Restrict);
        }
    }
}
