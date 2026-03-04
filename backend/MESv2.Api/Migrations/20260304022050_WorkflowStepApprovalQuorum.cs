using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace MESv2.Api.Migrations
{
    /// <inheritdoc />
    public partial class WorkflowStepApprovalQuorum : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "WorkflowStepApprovals",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    WorkflowStepInstanceId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    AssignmentType = table.Column<string>(type: "nvarchar(450)", nullable: false),
                    AssignedUserId = table.Column<Guid>(type: "uniqueidentifier", nullable: true),
                    AssignedRoleTier = table.Column<decimal>(type: "decimal(18,2)", nullable: true),
                    Status = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    ApprovedByUserId = table.Column<Guid>(type: "uniqueidentifier", nullable: true),
                    ApprovedAtUtc = table.Column<DateTime>(type: "datetime2", nullable: true),
                    Comments = table.Column<string>(type: "nvarchar(max)", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_WorkflowStepApprovals", x => x.Id);
                    table.CheckConstraint("CK_WorkflowStepApprovals_Assignee", "([AssignedUserId] IS NOT NULL AND [AssignedRoleTier] IS NULL) OR ([AssignedUserId] IS NULL AND [AssignedRoleTier] IS NOT NULL)");
                    table.ForeignKey(
                        name: "FK_WorkflowStepApprovals_WorkflowStepInstances_WorkflowStepInstanceId",
                        column: x => x.WorkflowStepInstanceId,
                        principalTable: "WorkflowStepInstances",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "IX_WorkflowStepApprovals_WorkflowStepInstanceId_AssignmentType_AssignedUserId_AssignedRoleTier",
                table: "WorkflowStepApprovals",
                columns: new[] { "WorkflowStepInstanceId", "AssignmentType", "AssignedUserId", "AssignedRoleTier" },
                unique: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "WorkflowStepApprovals");
        }
    }
}
