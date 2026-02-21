using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace MESv2.Api.Migrations
{
    /// <inheritdoc />
    public partial class MaterialQueueItemCleanup : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.Sql("DELETE FROM MaterialQueueItems");

            migrationBuilder.DropColumn(
                name: "CoilNumber",
                table: "MaterialQueueItems");

            migrationBuilder.DropColumn(
                name: "CoilSlabNumber",
                table: "MaterialQueueItems");

            migrationBuilder.DropColumn(
                name: "HeatNumber",
                table: "MaterialQueueItems");

            migrationBuilder.DropColumn(
                name: "LotNumber",
                table: "MaterialQueueItems");

            migrationBuilder.DropColumn(
                name: "ProductDescription",
                table: "MaterialQueueItems");

            migrationBuilder.DropColumn(
                name: "ProductId",
                table: "MaterialQueueItems");

            migrationBuilder.DropColumn(
                name: "ShellSize",
                table: "MaterialQueueItems");

            migrationBuilder.DropColumn(
                name: "VendorHeadId",
                table: "MaterialQueueItems");

            migrationBuilder.DropColumn(
                name: "VendorMillId",
                table: "MaterialQueueItems");

            migrationBuilder.DropColumn(
                name: "VendorProcessorId",
                table: "MaterialQueueItems");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "CoilNumber",
                table: "MaterialQueueItems",
                type: "nvarchar(max)",
                nullable: false,
                defaultValue: "");

            migrationBuilder.AddColumn<string>(
                name: "CoilSlabNumber",
                table: "MaterialQueueItems",
                type: "nvarchar(max)",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "HeatNumber",
                table: "MaterialQueueItems",
                type: "nvarchar(max)",
                nullable: false,
                defaultValue: "");

            migrationBuilder.AddColumn<string>(
                name: "LotNumber",
                table: "MaterialQueueItems",
                type: "nvarchar(max)",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "ProductDescription",
                table: "MaterialQueueItems",
                type: "nvarchar(max)",
                nullable: false,
                defaultValue: "");

            migrationBuilder.AddColumn<Guid>(
                name: "ProductId",
                table: "MaterialQueueItems",
                type: "uniqueidentifier",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "ShellSize",
                table: "MaterialQueueItems",
                type: "nvarchar(max)",
                nullable: true);

            migrationBuilder.AddColumn<Guid>(
                name: "VendorHeadId",
                table: "MaterialQueueItems",
                type: "uniqueidentifier",
                nullable: true);

            migrationBuilder.AddColumn<Guid>(
                name: "VendorMillId",
                table: "MaterialQueueItems",
                type: "uniqueidentifier",
                nullable: true);

            migrationBuilder.AddColumn<Guid>(
                name: "VendorProcessorId",
                table: "MaterialQueueItems",
                type: "uniqueidentifier",
                nullable: true);
        }
    }
}
