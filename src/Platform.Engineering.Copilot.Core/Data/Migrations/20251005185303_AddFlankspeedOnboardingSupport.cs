using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Platform.Engineering.Copilot.Core.Data.Migrations
{
    /// <inheritdoc />
    public partial class AddFlankspeedServiceCreationSupport : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "ServiceCreationRequests",
                columns: table => new
                {
                    Id = table.Column<string>(type: "TEXT", nullable: false),
                    MissionName = table.Column<string>(type: "TEXT", maxLength: 200, nullable: false),
                    MissionOwner = table.Column<string>(type: "TEXT", maxLength: 200, nullable: false),
                    MissionOwnerEmail = table.Column<string>(type: "TEXT", maxLength: 200, nullable: false),
                    MissionOwnerRank = table.Column<string>(type: "TEXT", maxLength: 50, nullable: false),
                    Command = table.Column<string>(type: "TEXT", maxLength: 200, nullable: false),
                    ClassificationLevel = table.Column<string>(type: "TEXT", maxLength: 20, nullable: false, defaultValue: "UNCLASS"),
                    RequestedSubscriptionName = table.Column<string>(type: "TEXT", maxLength: 200, nullable: false),
                    RequestedVNetCidr = table.Column<string>(type: "TEXT", maxLength: 50, nullable: false),
                    RequiredServicesJson = table.Column<string>(type: "TEXT", nullable: false),
                    Region = table.Column<string>(type: "TEXT", maxLength: 50, nullable: false, defaultValue: "usgovvirginia"),
                    EstimatedUserCount = table.Column<int>(type: "INTEGER", nullable: false),
                    DataResidency = table.Column<string>(type: "TEXT", maxLength: 50, nullable: false, defaultValue: "US"),
                    EstimatedDataVolumeTB = table.Column<decimal>(type: "TEXT", nullable: false),
                    RequiresPki = table.Column<bool>(type: "INTEGER", nullable: false),
                    RequiresCac = table.Column<bool>(type: "INTEGER", nullable: false, defaultValue: true),
                    RequiresAto = table.Column<bool>(type: "INTEGER", nullable: false),
                    SecurityContactEmail = table.Column<string>(type: "TEXT", maxLength: 200, nullable: false),
                    ComplianceFrameworksJson = table.Column<string>(type: "TEXT", nullable: false),
                    BusinessJustification = table.Column<string>(type: "TEXT", nullable: false),
                    UseCase = table.Column<string>(type: "TEXT", nullable: false),
                    RequestedStartDate = table.Column<DateTime>(type: "TEXT", nullable: false),
                    FundingSource = table.Column<string>(type: "TEXT", maxLength: 100, nullable: false),
                    EstimatedMonthlyCost = table.Column<decimal>(type: "TEXT", nullable: false),
                    MissionDurationMonths = table.Column<int>(type: "INTEGER", nullable: false),
                    Status = table.Column<string>(type: "TEXT", maxLength: 50, nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "TEXT", nullable: false, defaultValueSql: "datetime('now')"),
                    ReviewedAt = table.Column<DateTime>(type: "TEXT", nullable: true),
                    ProvisionedAt = table.Column<DateTime>(type: "TEXT", nullable: true),
                    CompletedAt = table.Column<DateTime>(type: "TEXT", nullable: true),
                    LastUpdatedAt = table.Column<DateTime>(type: "TEXT", nullable: false, defaultValueSql: "datetime('now')"),
                    ApprovedBy = table.Column<string>(type: "TEXT", maxLength: 200, nullable: true),
                    ApprovalComments = table.Column<string>(type: "TEXT", nullable: true),
                    RejectedBy = table.Column<string>(type: "TEXT", maxLength: 200, nullable: true),
                    RejectionReason = table.Column<string>(type: "TEXT", nullable: true),
                    Priority = table.Column<int>(type: "INTEGER", nullable: false, defaultValue: 3),
                    ProvisionedSubscriptionId = table.Column<string>(type: "TEXT", maxLength: 100, nullable: true),
                    ProvisionedVNetId = table.Column<string>(type: "TEXT", maxLength: 500, nullable: true),
                    ProvisionedResourceGroupId = table.Column<string>(type: "TEXT", maxLength: 500, nullable: true),
                    ProvisionedResourcesJson = table.Column<string>(type: "TEXT", nullable: false),
                    ProvisioningJobId = table.Column<string>(type: "TEXT", maxLength: 100, nullable: true),
                    ProvisioningError = table.Column<string>(type: "TEXT", nullable: true),
                    NotificationSent = table.Column<bool>(type: "INTEGER", nullable: false),
                    NotificationSentAt = table.Column<DateTime>(type: "TEXT", nullable: true),
                    NotificationHistoryJson = table.Column<string>(type: "TEXT", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_ServiceCreationRequests", x => x.Id);
                });

            migrationBuilder.CreateIndex(
                name: "IX_ServiceCreationRequests_ClassificationLevel",
                table: "ServiceCreationRequests",
                column: "ClassificationLevel");

            migrationBuilder.CreateIndex(
                name: "IX_ServiceCreationRequests_Command",
                table: "ServiceCreationRequests",
                column: "Command");

            migrationBuilder.CreateIndex(
                name: "IX_ServiceCreationRequests_CreatedAt",
                table: "ServiceCreationRequests",
                column: "CreatedAt");

            migrationBuilder.CreateIndex(
                name: "IX_ServiceCreationRequests_MissionOwnerEmail",
                table: "ServiceCreationRequests",
                column: "MissionOwnerEmail");

            migrationBuilder.CreateIndex(
                name: "IX_ServiceCreationRequests_Status",
                table: "ServiceCreationRequests",
                column: "Status");

            migrationBuilder.CreateIndex(
                name: "IX_ServiceCreationRequests_Status_Priority_CreatedAt",
                table: "ServiceCreationRequests",
                columns: new[] { "Status", "Priority", "CreatedAt" });
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "ServiceCreationRequests");
        }
    }
}
