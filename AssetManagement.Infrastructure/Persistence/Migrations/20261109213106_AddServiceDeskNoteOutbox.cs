using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace DeathStar.Infrastructure.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class AddServiceDeskNoteOutbox : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "ServiceDeskNoteOutbox",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    ServiceCaseId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    ServiceCaseEventId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    ServiceDeskNoteId = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: true),
                    Description = table.Column<string>(type: "nvarchar(4000)", maxLength: 4000, nullable: false),
                    Status = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: false),
                    CreatedAtUtc = table.Column<DateTime>(type: "datetime2", nullable: false),
                    ProcessingStartedAtUtc = table.Column<DateTime>(type: "datetime2", nullable: true),
                    ProcessingOperationId = table.Column<Guid>(type: "uniqueidentifier", nullable: true),
                    ProcessedAtUtc = table.Column<DateTime>(type: "datetime2", nullable: true),
                    Error = table.Column<string>(type: "nvarchar(1000)", maxLength: 1000, nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_ServiceDeskNoteOutbox", x => x.Id);
                    table.ForeignKey(
                        name: "FK_ServiceDeskNoteOutbox_ServiceCaseEvents_ServiceCaseEventId",
                        column: x => x.ServiceCaseEventId,
                        principalTable: "ServiceCaseEvents",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_ServiceDeskNoteOutbox_ServiceCases_ServiceCaseId",
                        column: x => x.ServiceCaseId,
                        principalTable: "ServiceCases",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateIndex(
                name: "IX_ServiceDeskNoteOutbox_ServiceCaseId",
                table: "ServiceDeskNoteOutbox",
                column: "ServiceCaseId");

            migrationBuilder.CreateIndex(
                name: "IX_ServiceDeskNoteOutbox_Status_CreatedAtUtc",
                table: "ServiceDeskNoteOutbox",
                columns: new[] { "Status", "CreatedAtUtc" });

            migrationBuilder.CreateIndex(
                name: "IX_ServiceDeskNoteOutbox_Status_ProcessingStartedAtUtc",
                table: "ServiceDeskNoteOutbox",
                columns: new[] { "Status", "ProcessingStartedAtUtc" });

            migrationBuilder.CreateIndex(
                name: "UX_ServiceDeskNoteOutbox_ServiceCaseEventId",
                table: "ServiceDeskNoteOutbox",
                column: "ServiceCaseEventId",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "UX_ServiceDeskNoteOutbox_ServiceDeskNoteId",
                table: "ServiceDeskNoteOutbox",
                column: "ServiceDeskNoteId",
                unique: true,
                filter: "[ServiceDeskNoteId] IS NOT NULL");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "ServiceDeskNoteOutbox");
        }
    }
}
