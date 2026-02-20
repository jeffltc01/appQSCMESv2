using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace MESv2.Api.Migrations
{
    /// <inheritdoc />
    public partial class InitialCreate : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "AnnotationTypes",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    Name = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    Abbreviation = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    RequiresResolution = table.Column<bool>(type: "bit", nullable: false),
                    OperatorCanCreate = table.Column<bool>(type: "bit", nullable: false),
                    DisplayColor = table.Column<string>(type: "nvarchar(max)", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_AnnotationTypes", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "BarcodeCards",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    CardValue = table.Column<string>(type: "nvarchar(450)", nullable: false),
                    Color = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    Description = table.Column<string>(type: "nvarchar(max)", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_BarcodeCards", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "DefectCodes",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    Code = table.Column<string>(type: "nvarchar(450)", nullable: false),
                    Name = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    Severity = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    SystemType = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    IsActive = table.Column<bool>(type: "bit", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_DefectCodes", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "ProductTypes",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    Name = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    SystemTypeName = table.Column<string>(type: "nvarchar(max)", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_ProductTypes", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "SiteSchedules",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    SiteCode = table.Column<string>(type: "nvarchar(450)", nullable: false),
                    Quantity = table.Column<decimal>(type: "decimal(18,2)", nullable: false),
                    QuantityComplete = table.Column<decimal>(type: "decimal(18,2)", nullable: false),
                    TankSize = table.Column<int>(type: "int", nullable: false),
                    TankType = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    ItemNo = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    ColorName = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    ColorValue = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    BuildDate = table.Column<DateTime>(type: "datetime2", nullable: true),
                    DispatchDate = table.Column<DateTime>(type: "datetime2", nullable: true),
                    Comments = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    Status = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    MasterScheduleId = table.Column<Guid>(type: "uniqueidentifier", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_SiteSchedules", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "TraceabilityLogs",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    FromSerialNumberId = table.Column<Guid>(type: "uniqueidentifier", nullable: true),
                    ToSerialNumberId = table.Column<Guid>(type: "uniqueidentifier", nullable: true),
                    FromAlphaCode = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    ToAlphaCode = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    Relationship = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    Quantity = table.Column<int>(type: "int", nullable: true),
                    TankLocation = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    Timestamp = table.Column<DateTime>(type: "datetime2", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_TraceabilityLogs", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "Vendors",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    Name = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    VendorType = table.Column<string>(type: "nvarchar(450)", nullable: false),
                    SiteCode = table.Column<string>(type: "nvarchar(450)", nullable: true),
                    IsActive = table.Column<bool>(type: "bit", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Vendors", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "WorkCenterTypes",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    Name = table.Column<string>(type: "nvarchar(max)", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_WorkCenterTypes", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "Characteristics",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    Name = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    SpecHigh = table.Column<decimal>(type: "decimal(18,2)", nullable: true),
                    SpecLow = table.Column<decimal>(type: "decimal(18,2)", nullable: true),
                    SpecTarget = table.Column<decimal>(type: "decimal(18,2)", nullable: true),
                    ProductTypeId = table.Column<Guid>(type: "uniqueidentifier", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Characteristics", x => x.Id);
                    table.ForeignKey(
                        name: "FK_Characteristics_ProductTypes_ProductTypeId",
                        column: x => x.ProductTypeId,
                        principalTable: "ProductTypes",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "Products",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    ProductNumber = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    TankSize = table.Column<int>(type: "int", nullable: false),
                    TankType = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    SageItemNumber = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    NameplateNumber = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    SiteNumbers = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    ProductTypeId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    IsActive = table.Column<bool>(type: "bit", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Products", x => x.Id);
                    table.ForeignKey(
                        name: "FK_Products_ProductTypes_ProductTypeId",
                        column: x => x.ProductTypeId,
                        principalTable: "ProductTypes",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "DefectLocations",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    Code = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    Name = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    DefaultLocationDetail = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    CharacteristicId = table.Column<Guid>(type: "uniqueidentifier", nullable: true),
                    IsActive = table.Column<bool>(type: "bit", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_DefectLocations", x => x.Id);
                    table.ForeignKey(
                        name: "FK_DefectLocations_Characteristics_CharacteristicId",
                        column: x => x.CharacteristicId,
                        principalTable: "Characteristics",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "ActiveSessions",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    UserId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    SiteCode = table.Column<string>(type: "nvarchar(450)", nullable: false),
                    ProductionLineId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    WorkCenterId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    AssetId = table.Column<Guid>(type: "uniqueidentifier", nullable: true),
                    LoginDateTime = table.Column<DateTime>(type: "datetime2", nullable: false),
                    LastHeartbeatDateTime = table.Column<DateTime>(type: "datetime2", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_ActiveSessions", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "Annotations",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    ProductionRecordId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    AnnotationTypeId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    Flag = table.Column<bool>(type: "bit", nullable: false),
                    Notes = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    InitiatedByUserId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    ResolvedByUserId = table.Column<Guid>(type: "uniqueidentifier", nullable: true),
                    ResolvedNotes = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    CreatedAt = table.Column<DateTime>(type: "datetime2", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Annotations", x => x.Id);
                    table.ForeignKey(
                        name: "FK_Annotations_AnnotationTypes_AnnotationTypeId",
                        column: x => x.AnnotationTypeId,
                        principalTable: "AnnotationTypes",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "Assemblies",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    AlphaCode = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    TankSize = table.Column<int>(type: "int", nullable: false),
                    WorkCenterId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    AssetId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    ProductionLineId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    OperatorId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    Timestamp = table.Column<DateTime>(type: "datetime2", nullable: false),
                    IsActive = table.Column<bool>(type: "bit", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Assemblies", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "Assets",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    Name = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    WorkCenterId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    ProductionLineId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    LimbleIdentifier = table.Column<string>(type: "nvarchar(max)", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Assets", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "ChangeLogs",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    RecordTable = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    RecordId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    ChangeDateTime = table.Column<DateTime>(type: "datetime2", nullable: false),
                    ChangeByUserId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    FieldName = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    FromValue = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    ToValue = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    FromValueId = table.Column<Guid>(type: "uniqueidentifier", nullable: true),
                    ToValueId = table.Column<Guid>(type: "uniqueidentifier", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_ChangeLogs", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "CharacteristicWorkCenters",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    CharacteristicId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    WorkCenterId = table.Column<Guid>(type: "uniqueidentifier", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_CharacteristicWorkCenters", x => x.Id);
                    table.ForeignKey(
                        name: "FK_CharacteristicWorkCenters_Characteristics_CharacteristicId",
                        column: x => x.CharacteristicId,
                        principalTable: "Characteristics",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "ControlPlans",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    CharacteristicId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    WorkCenterId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    IsEnabled = table.Column<bool>(type: "bit", nullable: false),
                    ResultType = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    IsGateCheck = table.Column<bool>(type: "bit", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_ControlPlans", x => x.Id);
                    table.ForeignKey(
                        name: "FK_ControlPlans_Characteristics_CharacteristicId",
                        column: x => x.CharacteristicId,
                        principalTable: "Characteristics",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "DefectLogs",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    ProductionRecordId = table.Column<Guid>(type: "uniqueidentifier", nullable: true),
                    InspectionRecordId = table.Column<Guid>(type: "uniqueidentifier", nullable: true),
                    HydroRecordId = table.Column<Guid>(type: "uniqueidentifier", nullable: true),
                    SerialNumber = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    DefectCodeId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    CharacteristicId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    LocationId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    LocationDetail = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    IsRepaired = table.Column<bool>(type: "bit", nullable: false),
                    RepairedByUserId = table.Column<Guid>(type: "uniqueidentifier", nullable: true),
                    CreatedAt = table.Column<DateTime>(type: "datetime2", nullable: false),
                    Timestamp = table.Column<DateTime>(type: "datetime2", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_DefectLogs", x => x.Id);
                    table.ForeignKey(
                        name: "FK_DefectLogs_Characteristics_CharacteristicId",
                        column: x => x.CharacteristicId,
                        principalTable: "Characteristics",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_DefectLogs_DefectCodes_DefectCodeId",
                        column: x => x.DefectCodeId,
                        principalTable: "DefectCodes",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_DefectLogs_DefectLocations_LocationId",
                        column: x => x.LocationId,
                        principalTable: "DefectLocations",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "DefectWorkCenters",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    DefectCodeId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    WorkCenterId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    EarliestDetectionWorkCenterId = table.Column<Guid>(type: "uniqueidentifier", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_DefectWorkCenters", x => x.Id);
                    table.ForeignKey(
                        name: "FK_DefectWorkCenters_DefectCodes_DefectCodeId",
                        column: x => x.DefectCodeId,
                        principalTable: "DefectCodes",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "HydroRecords",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    AssemblyAlphaCode = table.Column<string>(type: "nvarchar(450)", nullable: false),
                    NameplateSerialNumber = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    Result = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    WorkCenterId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    AssetId = table.Column<Guid>(type: "uniqueidentifier", nullable: true),
                    OperatorId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
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
                });

            migrationBuilder.CreateTable(
                name: "InspectionRecords",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    SerialNumber = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    WorkCenterId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    OperatorId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    Timestamp = table.Column<DateTime>(type: "datetime2", nullable: false),
                    ControlPlanId = table.Column<Guid>(type: "uniqueidentifier", nullable: true),
                    ResultText = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    ResultNumeric = table.Column<decimal>(type: "decimal(18,2)", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_InspectionRecords", x => x.Id);
                    table.ForeignKey(
                        name: "FK_InspectionRecords_ControlPlans_ControlPlanId",
                        column: x => x.ControlPlanId,
                        principalTable: "ControlPlans",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "MaterialQueueItems",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    WorkCenterId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    Position = table.Column<int>(type: "int", nullable: false),
                    Status = table.Column<string>(type: "nvarchar(450)", nullable: false),
                    ProductDescription = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    ShellSize = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    HeatNumber = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    CoilNumber = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    Quantity = table.Column<int>(type: "int", nullable: false),
                    CardId = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    CardColor = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    CreatedAt = table.Column<DateTime>(type: "datetime2", nullable: false),
                    ProductId = table.Column<Guid>(type: "uniqueidentifier", nullable: true),
                    VendorMillId = table.Column<Guid>(type: "uniqueidentifier", nullable: true),
                    VendorProcessorId = table.Column<Guid>(type: "uniqueidentifier", nullable: true),
                    VendorHeadId = table.Column<Guid>(type: "uniqueidentifier", nullable: true),
                    LotNumber = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    CoilSlabNumber = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    OperatorId = table.Column<Guid>(type: "uniqueidentifier", nullable: true),
                    QueueType = table.Column<string>(type: "nvarchar(max)", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_MaterialQueueItems", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "NameplateRecords",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    SerialNumber = table.Column<string>(type: "nvarchar(450)", nullable: false),
                    ProductId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    WorkCenterId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    OperatorId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
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
                });

            migrationBuilder.CreateTable(
                name: "PlantGears",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    Name = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    Level = table.Column<int>(type: "int", nullable: false),
                    PlantId = table.Column<Guid>(type: "uniqueidentifier", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_PlantGears", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "Plants",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    Code = table.Column<string>(type: "nvarchar(450)", nullable: false),
                    Name = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    TimeZoneId = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    CurrentPlantGearId = table.Column<Guid>(type: "uniqueidentifier", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Plants", x => x.Id);
                    table.ForeignKey(
                        name: "FK_Plants_PlantGears_CurrentPlantGearId",
                        column: x => x.CurrentPlantGearId,
                        principalTable: "PlantGears",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "ProductionLines",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    Name = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    PlantId = table.Column<Guid>(type: "uniqueidentifier", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_ProductionLines", x => x.Id);
                    table.ForeignKey(
                        name: "FK_ProductionLines_Plants_PlantId",
                        column: x => x.PlantId,
                        principalTable: "Plants",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "Users",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    EmployeeNumber = table.Column<string>(type: "nvarchar(450)", nullable: false),
                    FirstName = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    LastName = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    DisplayName = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    RoleTier = table.Column<decimal>(type: "decimal(18,2)", nullable: false),
                    RoleName = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    DefaultSiteId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    IsCertifiedWelder = table.Column<bool>(type: "bit", nullable: false),
                    RequirePinForLogin = table.Column<bool>(type: "bit", nullable: false),
                    PinHash = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    UserType = table.Column<int>(type: "int", nullable: false),
                    IsActive = table.Column<bool>(type: "bit", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Users", x => x.Id);
                    table.ForeignKey(
                        name: "FK_Users_Plants_DefaultSiteId",
                        column: x => x.DefaultSiteId,
                        principalTable: "Plants",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "WorkCenters",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    Name = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    PlantId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    WorkCenterTypeId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    ProductionLineId = table.Column<Guid>(type: "uniqueidentifier", nullable: true),
                    NumberOfWelders = table.Column<int>(type: "int", nullable: false),
                    DataEntryType = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    MaterialQueueForWCId = table.Column<Guid>(type: "uniqueidentifier", nullable: true),
                    WorkCenterGroupId = table.Column<Guid>(type: "uniqueidentifier", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_WorkCenters", x => x.Id);
                    table.ForeignKey(
                        name: "FK_WorkCenters_Plants_PlantId",
                        column: x => x.PlantId,
                        principalTable: "Plants",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_WorkCenters_ProductionLines_ProductionLineId",
                        column: x => x.ProductionLineId,
                        principalTable: "ProductionLines",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_WorkCenters_WorkCenterTypes_WorkCenterTypeId",
                        column: x => x.WorkCenterTypeId,
                        principalTable: "WorkCenterTypes",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_WorkCenters_WorkCenters_MaterialQueueForWCId",
                        column: x => x.MaterialQueueForWCId,
                        principalTable: "WorkCenters",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "SerialNumbers",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    Serial = table.Column<string>(type: "nvarchar(450)", nullable: false),
                    ProductId = table.Column<Guid>(type: "uniqueidentifier", nullable: true),
                    SiteCode = table.Column<string>(type: "nvarchar(450)", nullable: false),
                    Notes = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    MillVendorId = table.Column<Guid>(type: "uniqueidentifier", nullable: true),
                    ProcessorVendorId = table.Column<Guid>(type: "uniqueidentifier", nullable: true),
                    HeadsVendorId = table.Column<Guid>(type: "uniqueidentifier", nullable: true),
                    CoilNumber = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    HeatNumber = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    LotNumber = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    ReplaceBySNId = table.Column<Guid>(type: "uniqueidentifier", nullable: true),
                    Rs1Changed = table.Column<bool>(type: "bit", nullable: false),
                    Rs2Changed = table.Column<bool>(type: "bit", nullable: false),
                    Rs3Changed = table.Column<bool>(type: "bit", nullable: false),
                    Rs4Changed = table.Column<bool>(type: "bit", nullable: false),
                    IsObsolete = table.Column<bool>(type: "bit", nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "datetime2", nullable: false),
                    CreatedByUserId = table.Column<Guid>(type: "uniqueidentifier", nullable: true),
                    ModifiedDateTime = table.Column<DateTime>(type: "datetime2", nullable: true),
                    ModifiedByUserId = table.Column<Guid>(type: "uniqueidentifier", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_SerialNumbers", x => x.Id);
                    table.ForeignKey(
                        name: "FK_SerialNumbers_Products_ProductId",
                        column: x => x.ProductId,
                        principalTable: "Products",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_SerialNumbers_SerialNumbers_ReplaceBySNId",
                        column: x => x.ReplaceBySNId,
                        principalTable: "SerialNumbers",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_SerialNumbers_Users_CreatedByUserId",
                        column: x => x.CreatedByUserId,
                        principalTable: "Users",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_SerialNumbers_Users_ModifiedByUserId",
                        column: x => x.ModifiedByUserId,
                        principalTable: "Users",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_SerialNumbers_Vendors_HeadsVendorId",
                        column: x => x.HeadsVendorId,
                        principalTable: "Vendors",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_SerialNumbers_Vendors_MillVendorId",
                        column: x => x.MillVendorId,
                        principalTable: "Vendors",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_SerialNumbers_Vendors_ProcessorVendorId",
                        column: x => x.ProcessorVendorId,
                        principalTable: "Vendors",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "RoundSeamSetups",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    WorkCenterId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    TankSize = table.Column<int>(type: "int", nullable: false),
                    Rs1WelderId = table.Column<Guid>(type: "uniqueidentifier", nullable: true),
                    Rs2WelderId = table.Column<Guid>(type: "uniqueidentifier", nullable: true),
                    Rs3WelderId = table.Column<Guid>(type: "uniqueidentifier", nullable: true),
                    Rs4WelderId = table.Column<Guid>(type: "uniqueidentifier", nullable: true),
                    CreatedAt = table.Column<DateTime>(type: "datetime2", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_RoundSeamSetups", x => x.Id);
                    table.ForeignKey(
                        name: "FK_RoundSeamSetups_WorkCenters_WorkCenterId",
                        column: x => x.WorkCenterId,
                        principalTable: "WorkCenters",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "XrayQueueItems",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    WorkCenterId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    SerialNumber = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    OperatorId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "datetime2", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_XrayQueueItems", x => x.Id);
                    table.ForeignKey(
                        name: "FK_XrayQueueItems_Users_OperatorId",
                        column: x => x.OperatorId,
                        principalTable: "Users",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_XrayQueueItems_WorkCenters_WorkCenterId",
                        column: x => x.WorkCenterId,
                        principalTable: "WorkCenters",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "ProductionRecords",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    SerialNumberId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    WorkCenterId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    AssetId = table.Column<Guid>(type: "uniqueidentifier", nullable: true),
                    ProductionLineId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    OperatorId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    Timestamp = table.Column<DateTime>(type: "datetime2", nullable: false),
                    ProductInId = table.Column<Guid>(type: "uniqueidentifier", nullable: true),
                    ProductOutId = table.Column<Guid>(type: "uniqueidentifier", nullable: true),
                    PlantGearId = table.Column<Guid>(type: "uniqueidentifier", nullable: true),
                    InspectionResult = table.Column<string>(type: "nvarchar(max)", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_ProductionRecords", x => x.Id);
                    table.ForeignKey(
                        name: "FK_ProductionRecords_Assets_AssetId",
                        column: x => x.AssetId,
                        principalTable: "Assets",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_ProductionRecords_PlantGears_PlantGearId",
                        column: x => x.PlantGearId,
                        principalTable: "PlantGears",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_ProductionRecords_ProductionLines_ProductionLineId",
                        column: x => x.ProductionLineId,
                        principalTable: "ProductionLines",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_ProductionRecords_SerialNumbers_SerialNumberId",
                        column: x => x.SerialNumberId,
                        principalTable: "SerialNumbers",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_ProductionRecords_Users_OperatorId",
                        column: x => x.OperatorId,
                        principalTable: "Users",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_ProductionRecords_WorkCenters_WorkCenterId",
                        column: x => x.WorkCenterId,
                        principalTable: "WorkCenters",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "SpotXrayIncrements",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    ManufacturingLogId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    IncrementNo = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    OverallStatus = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    LaneNo = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    IsDraft = table.Column<bool>(type: "bit", nullable: false),
                    TankSize = table.Column<int>(type: "int", nullable: true),
                    InspectTank = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    InspectTankId = table.Column<Guid>(type: "uniqueidentifier", nullable: true),
                    Seam1ShotNo = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    Seam1ShotDateTime = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    Seam1InitialResult = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    Seam1FinalResult = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    Seam2ShotNo = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    Seam2ShotDateTime = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    Seam2InitialResult = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    Seam2FinalResult = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    Seam3ShotNo = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    Seam3ShotDateTime = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    Seam3InitialResult = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    Seam3FinalResult = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    Seam4ShotNo = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    Seam4ShotDateTime = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    Seam4InitialResult = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    Seam4FinalResult = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    Welder1 = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    Welder2 = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    Welder3 = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    Welder4 = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    CreatedByUserId = table.Column<Guid>(type: "uniqueidentifier", nullable: true),
                    CreatedDateTime = table.Column<DateTime>(type: "datetime2", nullable: true),
                    ModifiedByUserId = table.Column<Guid>(type: "uniqueidentifier", nullable: true),
                    ModifiedDateTime = table.Column<DateTime>(type: "datetime2", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_SpotXrayIncrements", x => x.Id);
                    table.ForeignKey(
                        name: "FK_SpotXrayIncrements_ProductionRecords_ManufacturingLogId",
                        column: x => x.ManufacturingLogId,
                        principalTable: "ProductionRecords",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_SpotXrayIncrements_Users_CreatedByUserId",
                        column: x => x.CreatedByUserId,
                        principalTable: "Users",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_SpotXrayIncrements_Users_ModifiedByUserId",
                        column: x => x.ModifiedByUserId,
                        principalTable: "Users",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "WelderLogs",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    ProductionRecordId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    UserId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    CharacteristicId = table.Column<Guid>(type: "uniqueidentifier", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_WelderLogs", x => x.Id);
                    table.ForeignKey(
                        name: "FK_WelderLogs_Characteristics_CharacteristicId",
                        column: x => x.CharacteristicId,
                        principalTable: "Characteristics",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_WelderLogs_ProductionRecords_ProductionRecordId",
                        column: x => x.ProductionRecordId,
                        principalTable: "ProductionRecords",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_WelderLogs_Users_UserId",
                        column: x => x.UserId,
                        principalTable: "Users",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateIndex(
                name: "IX_ActiveSessions_AssetId",
                table: "ActiveSessions",
                column: "AssetId");

            migrationBuilder.CreateIndex(
                name: "IX_ActiveSessions_ProductionLineId",
                table: "ActiveSessions",
                column: "ProductionLineId");

            migrationBuilder.CreateIndex(
                name: "IX_ActiveSessions_SiteCode",
                table: "ActiveSessions",
                column: "SiteCode");

            migrationBuilder.CreateIndex(
                name: "IX_ActiveSessions_UserId",
                table: "ActiveSessions",
                column: "UserId");

            migrationBuilder.CreateIndex(
                name: "IX_ActiveSessions_WorkCenterId",
                table: "ActiveSessions",
                column: "WorkCenterId");

            migrationBuilder.CreateIndex(
                name: "IX_Annotations_AnnotationTypeId",
                table: "Annotations",
                column: "AnnotationTypeId");

            migrationBuilder.CreateIndex(
                name: "IX_Annotations_InitiatedByUserId",
                table: "Annotations",
                column: "InitiatedByUserId");

            migrationBuilder.CreateIndex(
                name: "IX_Annotations_ProductionRecordId",
                table: "Annotations",
                column: "ProductionRecordId");

            migrationBuilder.CreateIndex(
                name: "IX_Annotations_ResolvedByUserId",
                table: "Annotations",
                column: "ResolvedByUserId");

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
                name: "IX_Assets_ProductionLineId",
                table: "Assets",
                column: "ProductionLineId");

            migrationBuilder.CreateIndex(
                name: "IX_Assets_WorkCenterId",
                table: "Assets",
                column: "WorkCenterId");

            migrationBuilder.CreateIndex(
                name: "IX_BarcodeCards_CardValue",
                table: "BarcodeCards",
                column: "CardValue");

            migrationBuilder.CreateIndex(
                name: "IX_ChangeLogs_ChangeByUserId",
                table: "ChangeLogs",
                column: "ChangeByUserId");

            migrationBuilder.CreateIndex(
                name: "IX_Characteristics_ProductTypeId",
                table: "Characteristics",
                column: "ProductTypeId");

            migrationBuilder.CreateIndex(
                name: "IX_CharacteristicWorkCenters_CharacteristicId",
                table: "CharacteristicWorkCenters",
                column: "CharacteristicId");

            migrationBuilder.CreateIndex(
                name: "IX_CharacteristicWorkCenters_WorkCenterId",
                table: "CharacteristicWorkCenters",
                column: "WorkCenterId");

            migrationBuilder.CreateIndex(
                name: "IX_ControlPlans_CharacteristicId",
                table: "ControlPlans",
                column: "CharacteristicId");

            migrationBuilder.CreateIndex(
                name: "IX_ControlPlans_WorkCenterId",
                table: "ControlPlans",
                column: "WorkCenterId");

            migrationBuilder.CreateIndex(
                name: "IX_DefectCodes_Code",
                table: "DefectCodes",
                column: "Code");

            migrationBuilder.CreateIndex(
                name: "IX_DefectLocations_CharacteristicId",
                table: "DefectLocations",
                column: "CharacteristicId");

            migrationBuilder.CreateIndex(
                name: "IX_DefectLogs_CharacteristicId",
                table: "DefectLogs",
                column: "CharacteristicId");

            migrationBuilder.CreateIndex(
                name: "IX_DefectLogs_DefectCodeId",
                table: "DefectLogs",
                column: "DefectCodeId");

            migrationBuilder.CreateIndex(
                name: "IX_DefectLogs_HydroRecordId",
                table: "DefectLogs",
                column: "HydroRecordId");

            migrationBuilder.CreateIndex(
                name: "IX_DefectLogs_InspectionRecordId",
                table: "DefectLogs",
                column: "InspectionRecordId");

            migrationBuilder.CreateIndex(
                name: "IX_DefectLogs_LocationId",
                table: "DefectLogs",
                column: "LocationId");

            migrationBuilder.CreateIndex(
                name: "IX_DefectLogs_ProductionRecordId",
                table: "DefectLogs",
                column: "ProductionRecordId");

            migrationBuilder.CreateIndex(
                name: "IX_DefectLogs_RepairedByUserId",
                table: "DefectLogs",
                column: "RepairedByUserId");

            migrationBuilder.CreateIndex(
                name: "IX_DefectWorkCenters_DefectCodeId",
                table: "DefectWorkCenters",
                column: "DefectCodeId");

            migrationBuilder.CreateIndex(
                name: "IX_DefectWorkCenters_EarliestDetectionWorkCenterId",
                table: "DefectWorkCenters",
                column: "EarliestDetectionWorkCenterId");

            migrationBuilder.CreateIndex(
                name: "IX_DefectWorkCenters_WorkCenterId",
                table: "DefectWorkCenters",
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
                name: "IX_InspectionRecords_ControlPlanId",
                table: "InspectionRecords",
                column: "ControlPlanId");

            migrationBuilder.CreateIndex(
                name: "IX_InspectionRecords_OperatorId",
                table: "InspectionRecords",
                column: "OperatorId");

            migrationBuilder.CreateIndex(
                name: "IX_InspectionRecords_WorkCenterId",
                table: "InspectionRecords",
                column: "WorkCenterId");

            migrationBuilder.CreateIndex(
                name: "IX_MaterialQueueItems_WorkCenterId_Status",
                table: "MaterialQueueItems",
                columns: new[] { "WorkCenterId", "Status" });

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

            migrationBuilder.CreateIndex(
                name: "IX_PlantGears_PlantId",
                table: "PlantGears",
                column: "PlantId");

            migrationBuilder.CreateIndex(
                name: "IX_Plants_Code",
                table: "Plants",
                column: "Code",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_Plants_CurrentPlantGearId",
                table: "Plants",
                column: "CurrentPlantGearId");

            migrationBuilder.CreateIndex(
                name: "IX_ProductionLines_PlantId",
                table: "ProductionLines",
                column: "PlantId");

            migrationBuilder.CreateIndex(
                name: "IX_ProductionRecords_AssetId",
                table: "ProductionRecords",
                column: "AssetId");

            migrationBuilder.CreateIndex(
                name: "IX_ProductionRecords_OperatorId",
                table: "ProductionRecords",
                column: "OperatorId");

            migrationBuilder.CreateIndex(
                name: "IX_ProductionRecords_PlantGearId",
                table: "ProductionRecords",
                column: "PlantGearId");

            migrationBuilder.CreateIndex(
                name: "IX_ProductionRecords_ProductionLineId",
                table: "ProductionRecords",
                column: "ProductionLineId");

            migrationBuilder.CreateIndex(
                name: "IX_ProductionRecords_SerialNumberId_WorkCenterId_Timestamp",
                table: "ProductionRecords",
                columns: new[] { "SerialNumberId", "WorkCenterId", "Timestamp" });

            migrationBuilder.CreateIndex(
                name: "IX_ProductionRecords_WorkCenterId",
                table: "ProductionRecords",
                column: "WorkCenterId");

            migrationBuilder.CreateIndex(
                name: "IX_Products_ProductTypeId",
                table: "Products",
                column: "ProductTypeId");

            migrationBuilder.CreateIndex(
                name: "IX_RoundSeamSetups_WorkCenterId_CreatedAt",
                table: "RoundSeamSetups",
                columns: new[] { "WorkCenterId", "CreatedAt" });

            migrationBuilder.CreateIndex(
                name: "IX_SerialNumbers_CreatedByUserId",
                table: "SerialNumbers",
                column: "CreatedByUserId");

            migrationBuilder.CreateIndex(
                name: "IX_SerialNumbers_HeadsVendorId",
                table: "SerialNumbers",
                column: "HeadsVendorId");

            migrationBuilder.CreateIndex(
                name: "IX_SerialNumbers_MillVendorId",
                table: "SerialNumbers",
                column: "MillVendorId");

            migrationBuilder.CreateIndex(
                name: "IX_SerialNumbers_ModifiedByUserId",
                table: "SerialNumbers",
                column: "ModifiedByUserId");

            migrationBuilder.CreateIndex(
                name: "IX_SerialNumbers_ProcessorVendorId",
                table: "SerialNumbers",
                column: "ProcessorVendorId");

            migrationBuilder.CreateIndex(
                name: "IX_SerialNumbers_ProductId",
                table: "SerialNumbers",
                column: "ProductId");

            migrationBuilder.CreateIndex(
                name: "IX_SerialNumbers_ReplaceBySNId",
                table: "SerialNumbers",
                column: "ReplaceBySNId");

            migrationBuilder.CreateIndex(
                name: "IX_SerialNumbers_Serial",
                table: "SerialNumbers",
                column: "Serial");

            migrationBuilder.CreateIndex(
                name: "IX_SerialNumbers_SiteCode",
                table: "SerialNumbers",
                column: "SiteCode");

            migrationBuilder.CreateIndex(
                name: "IX_SiteSchedules_SiteCode",
                table: "SiteSchedules",
                column: "SiteCode");

            migrationBuilder.CreateIndex(
                name: "IX_SpotXrayIncrements_CreatedByUserId",
                table: "SpotXrayIncrements",
                column: "CreatedByUserId");

            migrationBuilder.CreateIndex(
                name: "IX_SpotXrayIncrements_ManufacturingLogId",
                table: "SpotXrayIncrements",
                column: "ManufacturingLogId");

            migrationBuilder.CreateIndex(
                name: "IX_SpotXrayIncrements_ModifiedByUserId",
                table: "SpotXrayIncrements",
                column: "ModifiedByUserId");

            migrationBuilder.CreateIndex(
                name: "IX_TraceabilityLogs_Timestamp",
                table: "TraceabilityLogs",
                column: "Timestamp");

            migrationBuilder.CreateIndex(
                name: "IX_Users_DefaultSiteId",
                table: "Users",
                column: "DefaultSiteId");

            migrationBuilder.CreateIndex(
                name: "IX_Users_EmployeeNumber",
                table: "Users",
                column: "EmployeeNumber");

            migrationBuilder.CreateIndex(
                name: "IX_Vendors_VendorType_SiteCode",
                table: "Vendors",
                columns: new[] { "VendorType", "SiteCode" });

            migrationBuilder.CreateIndex(
                name: "IX_WelderLogs_CharacteristicId",
                table: "WelderLogs",
                column: "CharacteristicId");

            migrationBuilder.CreateIndex(
                name: "IX_WelderLogs_ProductionRecordId",
                table: "WelderLogs",
                column: "ProductionRecordId");

            migrationBuilder.CreateIndex(
                name: "IX_WelderLogs_UserId",
                table: "WelderLogs",
                column: "UserId");

            migrationBuilder.CreateIndex(
                name: "IX_WorkCenters_MaterialQueueForWCId",
                table: "WorkCenters",
                column: "MaterialQueueForWCId");

            migrationBuilder.CreateIndex(
                name: "IX_WorkCenters_PlantId",
                table: "WorkCenters",
                column: "PlantId");

            migrationBuilder.CreateIndex(
                name: "IX_WorkCenters_ProductionLineId",
                table: "WorkCenters",
                column: "ProductionLineId");

            migrationBuilder.CreateIndex(
                name: "IX_WorkCenters_WorkCenterTypeId",
                table: "WorkCenters",
                column: "WorkCenterTypeId");

            migrationBuilder.CreateIndex(
                name: "IX_XrayQueueItems_OperatorId",
                table: "XrayQueueItems",
                column: "OperatorId");

            migrationBuilder.CreateIndex(
                name: "IX_XrayQueueItems_WorkCenterId",
                table: "XrayQueueItems",
                column: "WorkCenterId");

            migrationBuilder.AddForeignKey(
                name: "FK_ActiveSessions_Assets_AssetId",
                table: "ActiveSessions",
                column: "AssetId",
                principalTable: "Assets",
                principalColumn: "Id",
                onDelete: ReferentialAction.Restrict);

            migrationBuilder.AddForeignKey(
                name: "FK_ActiveSessions_ProductionLines_ProductionLineId",
                table: "ActiveSessions",
                column: "ProductionLineId",
                principalTable: "ProductionLines",
                principalColumn: "Id",
                onDelete: ReferentialAction.Restrict);

            migrationBuilder.AddForeignKey(
                name: "FK_ActiveSessions_Users_UserId",
                table: "ActiveSessions",
                column: "UserId",
                principalTable: "Users",
                principalColumn: "Id",
                onDelete: ReferentialAction.Restrict);

            migrationBuilder.AddForeignKey(
                name: "FK_ActiveSessions_WorkCenters_WorkCenterId",
                table: "ActiveSessions",
                column: "WorkCenterId",
                principalTable: "WorkCenters",
                principalColumn: "Id",
                onDelete: ReferentialAction.Restrict);

            migrationBuilder.AddForeignKey(
                name: "FK_Annotations_ProductionRecords_ProductionRecordId",
                table: "Annotations",
                column: "ProductionRecordId",
                principalTable: "ProductionRecords",
                principalColumn: "Id",
                onDelete: ReferentialAction.Restrict);

            migrationBuilder.AddForeignKey(
                name: "FK_Annotations_Users_InitiatedByUserId",
                table: "Annotations",
                column: "InitiatedByUserId",
                principalTable: "Users",
                principalColumn: "Id",
                onDelete: ReferentialAction.Restrict);

            migrationBuilder.AddForeignKey(
                name: "FK_Annotations_Users_ResolvedByUserId",
                table: "Annotations",
                column: "ResolvedByUserId",
                principalTable: "Users",
                principalColumn: "Id",
                onDelete: ReferentialAction.Restrict);

            migrationBuilder.AddForeignKey(
                name: "FK_Assemblies_Assets_AssetId",
                table: "Assemblies",
                column: "AssetId",
                principalTable: "Assets",
                principalColumn: "Id",
                onDelete: ReferentialAction.Restrict);

            migrationBuilder.AddForeignKey(
                name: "FK_Assemblies_ProductionLines_ProductionLineId",
                table: "Assemblies",
                column: "ProductionLineId",
                principalTable: "ProductionLines",
                principalColumn: "Id",
                onDelete: ReferentialAction.Restrict);

            migrationBuilder.AddForeignKey(
                name: "FK_Assemblies_Users_OperatorId",
                table: "Assemblies",
                column: "OperatorId",
                principalTable: "Users",
                principalColumn: "Id",
                onDelete: ReferentialAction.Restrict);

            migrationBuilder.AddForeignKey(
                name: "FK_Assemblies_WorkCenters_WorkCenterId",
                table: "Assemblies",
                column: "WorkCenterId",
                principalTable: "WorkCenters",
                principalColumn: "Id",
                onDelete: ReferentialAction.Restrict);

            migrationBuilder.AddForeignKey(
                name: "FK_Assets_ProductionLines_ProductionLineId",
                table: "Assets",
                column: "ProductionLineId",
                principalTable: "ProductionLines",
                principalColumn: "Id",
                onDelete: ReferentialAction.Restrict);

            migrationBuilder.AddForeignKey(
                name: "FK_Assets_WorkCenters_WorkCenterId",
                table: "Assets",
                column: "WorkCenterId",
                principalTable: "WorkCenters",
                principalColumn: "Id",
                onDelete: ReferentialAction.Restrict);

            migrationBuilder.AddForeignKey(
                name: "FK_ChangeLogs_Users_ChangeByUserId",
                table: "ChangeLogs",
                column: "ChangeByUserId",
                principalTable: "Users",
                principalColumn: "Id",
                onDelete: ReferentialAction.Restrict);

            migrationBuilder.AddForeignKey(
                name: "FK_CharacteristicWorkCenters_WorkCenters_WorkCenterId",
                table: "CharacteristicWorkCenters",
                column: "WorkCenterId",
                principalTable: "WorkCenters",
                principalColumn: "Id",
                onDelete: ReferentialAction.Restrict);

            migrationBuilder.AddForeignKey(
                name: "FK_ControlPlans_WorkCenters_WorkCenterId",
                table: "ControlPlans",
                column: "WorkCenterId",
                principalTable: "WorkCenters",
                principalColumn: "Id",
                onDelete: ReferentialAction.Restrict);

            migrationBuilder.AddForeignKey(
                name: "FK_DefectLogs_HydroRecords_HydroRecordId",
                table: "DefectLogs",
                column: "HydroRecordId",
                principalTable: "HydroRecords",
                principalColumn: "Id",
                onDelete: ReferentialAction.Restrict);

            migrationBuilder.AddForeignKey(
                name: "FK_DefectLogs_InspectionRecords_InspectionRecordId",
                table: "DefectLogs",
                column: "InspectionRecordId",
                principalTable: "InspectionRecords",
                principalColumn: "Id",
                onDelete: ReferentialAction.Restrict);

            migrationBuilder.AddForeignKey(
                name: "FK_DefectLogs_ProductionRecords_ProductionRecordId",
                table: "DefectLogs",
                column: "ProductionRecordId",
                principalTable: "ProductionRecords",
                principalColumn: "Id",
                onDelete: ReferentialAction.Restrict);

            migrationBuilder.AddForeignKey(
                name: "FK_DefectLogs_Users_RepairedByUserId",
                table: "DefectLogs",
                column: "RepairedByUserId",
                principalTable: "Users",
                principalColumn: "Id",
                onDelete: ReferentialAction.Restrict);

            migrationBuilder.AddForeignKey(
                name: "FK_DefectWorkCenters_WorkCenters_EarliestDetectionWorkCenterId",
                table: "DefectWorkCenters",
                column: "EarliestDetectionWorkCenterId",
                principalTable: "WorkCenters",
                principalColumn: "Id",
                onDelete: ReferentialAction.Restrict);

            migrationBuilder.AddForeignKey(
                name: "FK_DefectWorkCenters_WorkCenters_WorkCenterId",
                table: "DefectWorkCenters",
                column: "WorkCenterId",
                principalTable: "WorkCenters",
                principalColumn: "Id",
                onDelete: ReferentialAction.Restrict);

            migrationBuilder.AddForeignKey(
                name: "FK_HydroRecords_Users_OperatorId",
                table: "HydroRecords",
                column: "OperatorId",
                principalTable: "Users",
                principalColumn: "Id",
                onDelete: ReferentialAction.Restrict);

            migrationBuilder.AddForeignKey(
                name: "FK_HydroRecords_WorkCenters_WorkCenterId",
                table: "HydroRecords",
                column: "WorkCenterId",
                principalTable: "WorkCenters",
                principalColumn: "Id",
                onDelete: ReferentialAction.Restrict);

            migrationBuilder.AddForeignKey(
                name: "FK_InspectionRecords_Users_OperatorId",
                table: "InspectionRecords",
                column: "OperatorId",
                principalTable: "Users",
                principalColumn: "Id",
                onDelete: ReferentialAction.Restrict);

            migrationBuilder.AddForeignKey(
                name: "FK_InspectionRecords_WorkCenters_WorkCenterId",
                table: "InspectionRecords",
                column: "WorkCenterId",
                principalTable: "WorkCenters",
                principalColumn: "Id",
                onDelete: ReferentialAction.Restrict);

            migrationBuilder.AddForeignKey(
                name: "FK_MaterialQueueItems_WorkCenters_WorkCenterId",
                table: "MaterialQueueItems",
                column: "WorkCenterId",
                principalTable: "WorkCenters",
                principalColumn: "Id",
                onDelete: ReferentialAction.Restrict);

            migrationBuilder.AddForeignKey(
                name: "FK_NameplateRecords_Users_OperatorId",
                table: "NameplateRecords",
                column: "OperatorId",
                principalTable: "Users",
                principalColumn: "Id",
                onDelete: ReferentialAction.Restrict);

            migrationBuilder.AddForeignKey(
                name: "FK_NameplateRecords_WorkCenters_WorkCenterId",
                table: "NameplateRecords",
                column: "WorkCenterId",
                principalTable: "WorkCenters",
                principalColumn: "Id",
                onDelete: ReferentialAction.Restrict);

            migrationBuilder.AddForeignKey(
                name: "FK_PlantGears_Plants_PlantId",
                table: "PlantGears",
                column: "PlantId",
                principalTable: "Plants",
                principalColumn: "Id",
                onDelete: ReferentialAction.Restrict);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_PlantGears_Plants_PlantId",
                table: "PlantGears");

            migrationBuilder.DropTable(
                name: "ActiveSessions");

            migrationBuilder.DropTable(
                name: "Annotations");

            migrationBuilder.DropTable(
                name: "Assemblies");

            migrationBuilder.DropTable(
                name: "BarcodeCards");

            migrationBuilder.DropTable(
                name: "ChangeLogs");

            migrationBuilder.DropTable(
                name: "CharacteristicWorkCenters");

            migrationBuilder.DropTable(
                name: "DefectLogs");

            migrationBuilder.DropTable(
                name: "DefectWorkCenters");

            migrationBuilder.DropTable(
                name: "MaterialQueueItems");

            migrationBuilder.DropTable(
                name: "NameplateRecords");

            migrationBuilder.DropTable(
                name: "RoundSeamSetups");

            migrationBuilder.DropTable(
                name: "SiteSchedules");

            migrationBuilder.DropTable(
                name: "SpotXrayIncrements");

            migrationBuilder.DropTable(
                name: "TraceabilityLogs");

            migrationBuilder.DropTable(
                name: "WelderLogs");

            migrationBuilder.DropTable(
                name: "XrayQueueItems");

            migrationBuilder.DropTable(
                name: "AnnotationTypes");

            migrationBuilder.DropTable(
                name: "DefectLocations");

            migrationBuilder.DropTable(
                name: "HydroRecords");

            migrationBuilder.DropTable(
                name: "InspectionRecords");

            migrationBuilder.DropTable(
                name: "DefectCodes");

            migrationBuilder.DropTable(
                name: "ProductionRecords");

            migrationBuilder.DropTable(
                name: "ControlPlans");

            migrationBuilder.DropTable(
                name: "Assets");

            migrationBuilder.DropTable(
                name: "SerialNumbers");

            migrationBuilder.DropTable(
                name: "Characteristics");

            migrationBuilder.DropTable(
                name: "WorkCenters");

            migrationBuilder.DropTable(
                name: "Products");

            migrationBuilder.DropTable(
                name: "Users");

            migrationBuilder.DropTable(
                name: "Vendors");

            migrationBuilder.DropTable(
                name: "ProductionLines");

            migrationBuilder.DropTable(
                name: "WorkCenterTypes");

            migrationBuilder.DropTable(
                name: "ProductTypes");

            migrationBuilder.DropTable(
                name: "Plants");

            migrationBuilder.DropTable(
                name: "PlantGears");
        }
    }
}
