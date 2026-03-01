using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace MESv2.Api.Migrations
{
    /// <inheritdoc />
    public partial class AddDefectLocationCharacteristicLinks : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "DefectLocationCharacteristics",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    DefectLocationId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    CharacteristicId = table.Column<Guid>(type: "uniqueidentifier", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_DefectLocationCharacteristics", x => x.Id);
                    table.ForeignKey(
                        name: "FK_DefectLocationCharacteristics_Characteristics_CharacteristicId",
                        column: x => x.CharacteristicId,
                        principalTable: "Characteristics",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_DefectLocationCharacteristics_DefectLocations_DefectLocationId",
                        column: x => x.DefectLocationId,
                        principalTable: "DefectLocations",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "IX_DefectLocationCharacteristics_CharacteristicId",
                table: "DefectLocationCharacteristics",
                column: "CharacteristicId");

            migrationBuilder.CreateIndex(
                name: "IX_DefectLocationCharacteristics_DefectLocationId_CharacteristicId",
                table: "DefectLocationCharacteristics",
                columns: new[] { "DefectLocationId", "CharacteristicId" },
                unique: true);

            migrationBuilder.Sql(
                """
                INSERT INTO DefectLocationCharacteristics (Id, DefectLocationId, CharacteristicId)
                SELECT NEWID(), dl.Id, dl.CharacteristicId
                FROM DefectLocations AS dl
                WHERE dl.CharacteristicId IS NOT NULL
                """);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "DefectLocationCharacteristics");
        }
    }
}
