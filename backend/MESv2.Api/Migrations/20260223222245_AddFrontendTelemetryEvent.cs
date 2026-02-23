using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace MESv2.Api.Migrations
{
    /// <inheritdoc />
    public partial class AddFrontendTelemetryEvent : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "FrontendTelemetryEvents",
                columns: table => new
                {
                    Id = table.Column<long>(type: "bigint", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    OccurredAtUtc = table.Column<DateTime>(type: "datetime2", nullable: false),
                    ReceivedAtUtc = table.Column<DateTime>(type: "datetime2", nullable: false),
                    Category = table.Column<string>(type: "nvarchar(64)", maxLength: 64, nullable: false),
                    Source = table.Column<string>(type: "nvarchar(64)", maxLength: 64, nullable: false),
                    Severity = table.Column<string>(type: "nvarchar(32)", maxLength: 32, nullable: false),
                    IsReactRuntimeOverlayCandidate = table.Column<bool>(type: "bit", nullable: false),
                    Route = table.Column<string>(type: "nvarchar(256)", maxLength: 256, nullable: true),
                    Screen = table.Column<string>(type: "nvarchar(128)", maxLength: 128, nullable: true),
                    Message = table.Column<string>(type: "nvarchar(2048)", maxLength: 2048, nullable: false),
                    Stack = table.Column<string>(type: "nvarchar(max)", maxLength: 8000, nullable: true),
                    MetadataJson = table.Column<string>(type: "nvarchar(max)", maxLength: 8000, nullable: true),
                    SessionId = table.Column<string>(type: "nvarchar(128)", maxLength: 128, nullable: true),
                    CorrelationId = table.Column<string>(type: "nvarchar(128)", maxLength: 128, nullable: true),
                    ApiPath = table.Column<string>(type: "nvarchar(256)", maxLength: 256, nullable: true),
                    HttpMethod = table.Column<string>(type: "nvarchar(16)", maxLength: 16, nullable: true),
                    HttpStatus = table.Column<int>(type: "int", nullable: true),
                    UserId = table.Column<Guid>(type: "uniqueidentifier", nullable: true),
                    WorkCenterId = table.Column<Guid>(type: "uniqueidentifier", nullable: true),
                    ProductionLineId = table.Column<Guid>(type: "uniqueidentifier", nullable: true),
                    PlantId = table.Column<Guid>(type: "uniqueidentifier", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_FrontendTelemetryEvents", x => x.Id);
                    table.ForeignKey(
                        name: "FK_FrontendTelemetryEvents_Plants_PlantId",
                        column: x => x.PlantId,
                        principalTable: "Plants",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.SetNull);
                    table.ForeignKey(
                        name: "FK_FrontendTelemetryEvents_ProductionLines_ProductionLineId",
                        column: x => x.ProductionLineId,
                        principalTable: "ProductionLines",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.SetNull);
                    table.ForeignKey(
                        name: "FK_FrontendTelemetryEvents_Users_UserId",
                        column: x => x.UserId,
                        principalTable: "Users",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.SetNull);
                    table.ForeignKey(
                        name: "FK_FrontendTelemetryEvents_WorkCenters_WorkCenterId",
                        column: x => x.WorkCenterId,
                        principalTable: "WorkCenters",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.SetNull);
                });

            migrationBuilder.CreateIndex(
                name: "IX_FrontendTelemetryEvents_Category",
                table: "FrontendTelemetryEvents",
                column: "Category");

            migrationBuilder.CreateIndex(
                name: "IX_FrontendTelemetryEvents_OccurredAtUtc",
                table: "FrontendTelemetryEvents",
                column: "OccurredAtUtc");

            migrationBuilder.CreateIndex(
                name: "IX_FrontendTelemetryEvents_PlantId",
                table: "FrontendTelemetryEvents",
                column: "PlantId");

            migrationBuilder.CreateIndex(
                name: "IX_FrontendTelemetryEvents_ProductionLineId",
                table: "FrontendTelemetryEvents",
                column: "ProductionLineId");

            migrationBuilder.CreateIndex(
                name: "IX_FrontendTelemetryEvents_Source",
                table: "FrontendTelemetryEvents",
                column: "Source");

            migrationBuilder.CreateIndex(
                name: "IX_FrontendTelemetryEvents_UserId",
                table: "FrontendTelemetryEvents",
                column: "UserId");

            migrationBuilder.CreateIndex(
                name: "IX_FrontendTelemetryEvents_WorkCenterId",
                table: "FrontendTelemetryEvents",
                column: "WorkCenterId");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "FrontendTelemetryEvents");
        }
    }
}
