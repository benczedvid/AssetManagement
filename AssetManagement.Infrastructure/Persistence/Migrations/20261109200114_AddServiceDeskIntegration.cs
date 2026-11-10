using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace DeathStar.Infrastructure.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class AddServiceDeskIntegration : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<Guid>(
                name: "ServiceDeskOperationId",
                table: "ServiceCases",
                type: "uniqueidentifier",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "ServiceDeskRequestId",
                table: "ServiceCases",
                type: "nvarchar(100)",
                maxLength: 100,
                nullable: true);

            migrationBuilder.AddColumn<DateTime>(
                name: "ServiceDeskSyncCompletedAtUtc",
                table: "ServiceCases",
                type: "datetime2",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "ServiceDeskSyncError",
                table: "ServiceCases",
                type: "nvarchar(1000)",
                maxLength: 1000,
                nullable: true);

            migrationBuilder.AddColumn<DateTime>(
                name: "ServiceDeskSyncStartedAtUtc",
                table: "ServiceCases",
                type: "datetime2",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "ServiceDeskSyncStatus",
                table: "ServiceCases",
                type: "nvarchar(50)",
                maxLength: 50,
                nullable: false,
                defaultValue: "NotStarted");

            migrationBuilder.CreateTable(
                name: "ServiceDeskReconciliationAudits",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    Operation = table.Column<int>(type: "int", nullable: false),
                    OutboxId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    ServiceCaseId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    ProcessingOperationId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    Outcome = table.Column<int>(type: "int", nullable: false),
                    ExternalIdentifier = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    VerifiedByUserId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    VerifiedByUserName = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    VerifiedAtUtc = table.Column<DateTime>(type: "datetime2", nullable: false),
                    Comment = table.Column<string>(type: "nvarchar(max)", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_ServiceDeskReconciliationAudits", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "ServiceDeskRequestClosureOutbox",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    ServiceCaseId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    ServiceCaseEventId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    Status = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: false),
                    ProcessingOperationId = table.Column<Guid>(type: "uniqueidentifier", nullable: true),
                    CreatedAtUtc = table.Column<DateTime>(type: "datetime2", nullable: false),
                    ProcessingStartedAtUtc = table.Column<DateTime>(type: "datetime2", nullable: true),
                    ProcessedAtUtc = table.Column<DateTime>(type: "datetime2", nullable: true),
                    Error = table.Column<string>(type: "nvarchar(1000)", maxLength: 1000, nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_ServiceDeskRequestClosureOutbox", x => x.Id);
                    table.ForeignKey(
                        name: "FK_ServiceDeskRequestClosureOutbox_ServiceCaseEvents_ServiceCaseEventId",
                        column: x => x.ServiceCaseEventId,
                        principalTable: "ServiceCaseEvents",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_ServiceDeskRequestClosureOutbox_ServiceCases_ServiceCaseId",
                        column: x => x.ServiceCaseId,
                        principalTable: "ServiceCases",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "ServiceDeskRequestOutbox",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    ServiceCaseId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    ServiceCaseEventId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    Subject = table.Column<string>(type: "nvarchar(250)", maxLength: 250, nullable: false),
                    Description = table.Column<string>(type: "nvarchar(2000)", maxLength: 2000, nullable: false),
                    ServiceDeskRequestId = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: true),
                    Status = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: false),
                    ProcessingOperationId = table.Column<Guid>(type: "uniqueidentifier", nullable: true),
                    CreatedAtUtc = table.Column<DateTime>(type: "datetime2", nullable: false),
                    ProcessingStartedAtUtc = table.Column<DateTime>(type: "datetime2", nullable: true),
                    ProcessedAtUtc = table.Column<DateTime>(type: "datetime2", nullable: true),
                    Error = table.Column<string>(type: "nvarchar(1000)", maxLength: 1000, nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_ServiceDeskRequestOutbox", x => x.Id);
                    table.ForeignKey(
                        name: "FK_ServiceDeskRequestOutbox_ServiceCaseEvents_ServiceCaseEventId",
                        column: x => x.ServiceCaseEventId,
                        principalTable: "ServiceCaseEvents",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_ServiceDeskRequestOutbox_ServiceCases_ServiceCaseId",
                        column: x => x.ServiceCaseId,
                        principalTable: "ServiceCases",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateIndex(
                name: "IX_ServiceCases_ServiceDeskRequestId",
                table: "ServiceCases",
                column: "ServiceDeskRequestId",
                unique: true,
                filter: "[ServiceDeskRequestId] IS NOT NULL");

            migrationBuilder.CreateIndex(
                name: "IX_ServiceDeskRequestClosureOutbox_Status_CreatedAtUtc",
                table: "ServiceDeskRequestClosureOutbox",
                columns: new[] { "Status", "CreatedAtUtc" });

            migrationBuilder.CreateIndex(
                name: "IX_ServiceDeskRequestClosureOutbox_Status_ProcessingStartedAtUtc",
                table: "ServiceDeskRequestClosureOutbox",
                columns: new[] { "Status", "ProcessingStartedAtUtc" });

            migrationBuilder.CreateIndex(
                name: "UX_ServiceDeskRequestClosureOutbox_ServiceCaseEventId",
                table: "ServiceDeskRequestClosureOutbox",
                column: "ServiceCaseEventId",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "UX_ServiceDeskRequestClosureOutbox_ServiceCaseId",
                table: "ServiceDeskRequestClosureOutbox",
                column: "ServiceCaseId",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_ServiceDeskRequestOutbox_Status_CreatedAtUtc",
                table: "ServiceDeskRequestOutbox",
                columns: new[] { "Status", "CreatedAtUtc" });

            migrationBuilder.CreateIndex(
                name: "IX_ServiceDeskRequestOutbox_Status_ProcessingStartedAtUtc",
                table: "ServiceDeskRequestOutbox",
                columns: new[] { "Status", "ProcessingStartedAtUtc" });

            migrationBuilder.CreateIndex(
                name: "UX_ServiceDeskRequestOutbox_ServiceCaseEventId",
                table: "ServiceDeskRequestOutbox",
                column: "ServiceCaseEventId",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "UX_ServiceDeskRequestOutbox_ServiceCaseId",
                table: "ServiceDeskRequestOutbox",
                column: "ServiceCaseId",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "UX_ServiceDeskRequestOutbox_ServiceDeskRequestId",
                table: "ServiceDeskRequestOutbox",
                column: "ServiceDeskRequestId",
                unique: true,
                filter: "[ServiceDeskRequestId] IS NOT NULL");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "ServiceDeskReconciliationAudits");

            migrationBuilder.DropTable(
                name: "ServiceDeskRequestClosureOutbox");

            migrationBuilder.DropTable(
                name: "ServiceDeskRequestOutbox");

            migrationBuilder.DropIndex(
                name: "IX_ServiceCases_ServiceDeskRequestId",
                table: "ServiceCases");

            migrationBuilder.DropColumn(
                name: "ServiceDeskOperationId",
                table: "ServiceCases");

            migrationBuilder.DropColumn(
                name: "ServiceDeskRequestId",
                table: "ServiceCases");

            migrationBuilder.DropColumn(
                name: "ServiceDeskSyncCompletedAtUtc",
                table: "ServiceCases");

            migrationBuilder.DropColumn(
                name: "ServiceDeskSyncError",
                table: "ServiceCases");

            migrationBuilder.DropColumn(
                name: "ServiceDeskSyncStartedAtUtc",
                table: "ServiceCases");

            migrationBuilder.DropColumn(
                name: "ServiceDeskSyncStatus",
                table: "ServiceCases");
        }
    }
}
