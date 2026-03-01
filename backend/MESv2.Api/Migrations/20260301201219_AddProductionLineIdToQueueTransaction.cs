using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace MESv2.Api.Migrations
{
    /// <inheritdoc />
    public partial class AddProductionLineIdToQueueTransaction : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "IX_QueueTransactions_WorkCenterId_Timestamp",
                table: "QueueTransactions");

            migrationBuilder.AddColumn<Guid>(
                name: "ProductionLineId",
                table: "QueueTransactions",
                type: "uniqueidentifier",
                nullable: true);

            migrationBuilder.CreateIndex(
                name: "IX_QueueTransactions_ProductionLineId",
                table: "QueueTransactions",
                column: "ProductionLineId");

            migrationBuilder.CreateIndex(
                name: "IX_QueueTransactions_WorkCenterId_ProductionLineId_Timestamp",
                table: "QueueTransactions",
                columns: new[] { "WorkCenterId", "ProductionLineId", "Timestamp" });

            migrationBuilder.AddForeignKey(
                name: "FK_QueueTransactions_ProductionLines_ProductionLineId",
                table: "QueueTransactions",
                column: "ProductionLineId",
                principalTable: "ProductionLines",
                principalColumn: "Id",
                onDelete: ReferentialAction.Restrict);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_QueueTransactions_ProductionLines_ProductionLineId",
                table: "QueueTransactions");

            migrationBuilder.DropIndex(
                name: "IX_QueueTransactions_ProductionLineId",
                table: "QueueTransactions");

            migrationBuilder.DropIndex(
                name: "IX_QueueTransactions_WorkCenterId_ProductionLineId_Timestamp",
                table: "QueueTransactions");

            migrationBuilder.DropColumn(
                name: "ProductionLineId",
                table: "QueueTransactions");

            migrationBuilder.CreateIndex(
                name: "IX_QueueTransactions_WorkCenterId_Timestamp",
                table: "QueueTransactions",
                columns: new[] { "WorkCenterId", "Timestamp" });
        }
    }
}
