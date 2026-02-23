using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace MESv2.Api.Migrations
{
    /// <inheritdoc />
    public partial class AddProductionLineIdToMaterialQueueItem : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<Guid>(
                name: "ProductionLineId",
                table: "MaterialQueueItems",
                type: "uniqueidentifier",
                nullable: true);

            migrationBuilder.CreateIndex(
                name: "IX_MaterialQueueItems_ProductionLineId",
                table: "MaterialQueueItems",
                column: "ProductionLineId");

            migrationBuilder.AddForeignKey(
                name: "FK_MaterialQueueItems_ProductionLines_ProductionLineId",
                table: "MaterialQueueItems",
                column: "ProductionLineId",
                principalTable: "ProductionLines",
                principalColumn: "Id",
                onDelete: ReferentialAction.SetNull);

            // Backfill: assign existing items to the first production line linked to their WC
            migrationBuilder.Sql(@"
                UPDATE mqi
                SET mqi.ProductionLineId = sub.ProductionLineId
                FROM MaterialQueueItems mqi
                CROSS APPLY (
                    SELECT TOP 1 wcpl.ProductionLineId
                    FROM WorkCenterProductionLines wcpl
                    WHERE wcpl.WorkCenterId = mqi.WorkCenterId
                    ORDER BY wcpl.ProductionLineId
                ) sub
                WHERE mqi.ProductionLineId IS NULL");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_MaterialQueueItems_ProductionLines_ProductionLineId",
                table: "MaterialQueueItems");

            migrationBuilder.DropIndex(
                name: "IX_MaterialQueueItems_ProductionLineId",
                table: "MaterialQueueItems");

            migrationBuilder.DropColumn(
                name: "ProductionLineId",
                table: "MaterialQueueItems");
        }
    }
}
