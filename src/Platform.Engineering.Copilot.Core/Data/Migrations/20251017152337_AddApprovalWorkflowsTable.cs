using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Platform.Engineering.Copilot.Core.Data.Migrations
{
    /// <inheritdoc />
    public partial class AddApprovalWorkflowsTable : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "ApprovalWorkflows",
                columns: table => new
                {
                    Id = table.Column<string>(type: "TEXT", maxLength: 100, nullable: false),
                    ToolCallId = table.Column<string>(type: "TEXT", maxLength: 100, nullable: false),
                    Status = table.Column<string>(type: "TEXT", maxLength: 50, nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "TEXT", nullable: false, defaultValueSql: "datetime('now')"),
                    CompletedAt = table.Column<DateTime>(type: "TEXT", nullable: true),
                    Justification = table.Column<string>(type: "TEXT", maxLength: 1000, nullable: false),
                    Priority = table.Column<int>(type: "INTEGER", nullable: false, defaultValue: 1),
                    ResourceType = table.Column<string>(type: "TEXT", maxLength: 100, nullable: false),
                    ResourceName = table.Column<string>(type: "TEXT", maxLength: 200, nullable: false),
                    ResourceGroupName = table.Column<string>(type: "TEXT", maxLength: 200, nullable: false),
                    Location = table.Column<string>(type: "TEXT", maxLength: 100, nullable: false),
                    Environment = table.Column<string>(type: "TEXT", maxLength: 50, nullable: false),
                    RequestedBy = table.Column<string>(type: "TEXT", maxLength: 200, nullable: false),
                    RequestedAt = table.Column<DateTime>(type: "TEXT", nullable: false),
                    ExpiresAt = table.Column<DateTime>(type: "TEXT", nullable: false),
                    Reason = table.Column<string>(type: "TEXT", maxLength: 1000, nullable: false),
                    ApprovedBy = table.Column<string>(type: "TEXT", maxLength: 200, nullable: true),
                    ApprovedAt = table.Column<DateTime>(type: "TEXT", nullable: true),
                    ApprovalComments = table.Column<string>(type: "TEXT", maxLength: 1000, nullable: true),
                    RejectedBy = table.Column<string>(type: "TEXT", maxLength: 200, nullable: true),
                    RejectedAt = table.Column<DateTime>(type: "TEXT", nullable: true),
                    RejectionReason = table.Column<string>(type: "TEXT", maxLength: 1000, nullable: true),
                    RequiredApproversJson = table.Column<string>(type: "TEXT", nullable: false),
                    PolicyViolationsJson = table.Column<string>(type: "TEXT", nullable: false),
                    OriginalToolCallJson = table.Column<string>(type: "TEXT", nullable: false),
                    DecisionsJson = table.Column<string>(type: "TEXT", nullable: false),
                    RequestPayload = table.Column<string>(type: "TEXT", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_ApprovalWorkflows", x => x.Id);
                });

            migrationBuilder.CreateIndex(
                name: "IX_ApprovalWorkflows_CreatedAt",
                table: "ApprovalWorkflows",
                column: "CreatedAt");

            migrationBuilder.CreateIndex(
                name: "IX_ApprovalWorkflows_Environment",
                table: "ApprovalWorkflows",
                column: "Environment");

            migrationBuilder.CreateIndex(
                name: "IX_ApprovalWorkflows_ExpiresAt",
                table: "ApprovalWorkflows",
                column: "ExpiresAt");

            migrationBuilder.CreateIndex(
                name: "IX_ApprovalWorkflows_RequestedBy",
                table: "ApprovalWorkflows",
                column: "RequestedBy");

            migrationBuilder.CreateIndex(
                name: "IX_ApprovalWorkflows_ResourceGroup_Environment_Status",
                table: "ApprovalWorkflows",
                columns: new[] { "ResourceGroupName", "Environment", "Status" });

            migrationBuilder.CreateIndex(
                name: "IX_ApprovalWorkflows_ResourceType",
                table: "ApprovalWorkflows",
                column: "ResourceType");

            migrationBuilder.CreateIndex(
                name: "IX_ApprovalWorkflows_Status",
                table: "ApprovalWorkflows",
                column: "Status");

            migrationBuilder.CreateIndex(
                name: "IX_ApprovalWorkflows_Status_Priority_CreatedAt",
                table: "ApprovalWorkflows",
                columns: new[] { "Status", "Priority", "CreatedAt" });
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "ApprovalWorkflows");
        }
    }
}
