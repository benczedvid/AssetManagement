using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace DeathStar.Infrastructure.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class AddServiceCases : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "ServiceCases",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    CaseNumber = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: false),
                    AssetId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    Status = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: false),
                    IssueType = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: false),
                    OtherIssueDescription = table.Column<string>(type: "nvarchar(250)", maxLength: 250, nullable: true),
                    Description = table.Column<string>(type: "nvarchar(2000)", maxLength: 2000, nullable: false),
                    OriginStoreId = table.Column<Guid>(type: "uniqueidentifier", nullable: true),
                    VendorId = table.Column<Guid>(type: "uniqueidentifier", nullable: true),
                    CreatedByUserId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    CreatedByUserName = table.Column<string>(type: "nvarchar(250)", maxLength: 250, nullable: false),
                    CreatedAtUtc = table.Column<DateTime>(type: "datetime2", nullable: false),
                    ClosedByUserId = table.Column<Guid>(type: "uniqueidentifier", nullable: true),
                    ClosedAtUtc = table.Column<DateTime>(type: "datetime2", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_ServiceCases", x => x.Id);
                    table.ForeignKey(
                        name: "FK_ServiceCases_ApplicationUsers_ClosedByUserId",
                        column: x => x.ClosedByUserId,
                        principalTable: "ApplicationUsers",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_ServiceCases_ApplicationUsers_CreatedByUserId",
                        column: x => x.CreatedByUserId,
                        principalTable: "ApplicationUsers",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_ServiceCases_Assets_AssetId",
                        column: x => x.AssetId,
                        principalTable: "Assets",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_ServiceCases_Stores_OriginStoreId",
                        column: x => x.OriginStoreId,
                        principalTable: "Stores",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_ServiceCases_Vendors_VendorId",
                        column: x => x.VendorId,
                        principalTable: "Vendors",
                        principalColumn: "VendorId",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "ServiceCaseEvents",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    ServiceCaseId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    Type = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: false),
                    PreviousStatus = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: true),
                    NewStatus = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: false),
                    Comment = table.Column<string>(type: "nvarchar(2000)", maxLength: 2000, nullable: true),
                    PerformedByUserId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    PerformedByUserName = table.Column<string>(type: "nvarchar(250)", maxLength: 250, nullable: false),
                    OccurredAtUtc = table.Column<DateTime>(type: "datetime2", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_ServiceCaseEvents", x => x.Id);
                    table.ForeignKey(
                        name: "FK_ServiceCaseEvents_ApplicationUsers_PerformedByUserId",
                        column: x => x.PerformedByUserId,
                        principalTable: "ApplicationUsers",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_ServiceCaseEvents_ServiceCases_ServiceCaseId",
                        column: x => x.ServiceCaseId,
                        principalTable: "ServiceCases",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "IX_ServiceCaseEvents_PerformedByUserId",
                table: "ServiceCaseEvents",
                column: "PerformedByUserId");

            migrationBuilder.CreateIndex(
                name: "IX_ServiceCaseEvents_ServiceCaseId",
                table: "ServiceCaseEvents",
                column: "ServiceCaseId");

            migrationBuilder.CreateIndex(
                name: "IX_ServiceCaseEvents_ServiceCaseId_OccurredAtUtc",
                table: "ServiceCaseEvents",
                columns: new[] { "ServiceCaseId", "OccurredAtUtc" });

            migrationBuilder.CreateIndex(
                name: "IX_ServiceCaseEvents_Type",
                table: "ServiceCaseEvents",
                column: "Type");

            migrationBuilder.CreateIndex(
                name: "IX_ServiceCases_AssetId",
                table: "ServiceCases",
                column: "AssetId");

            migrationBuilder.CreateIndex(
                name: "IX_ServiceCases_AssetId_Status",
                table: "ServiceCases",
                columns: new[] { "AssetId", "Status" });

            migrationBuilder.CreateIndex(
                name: "IX_ServiceCases_ClosedByUserId",
                table: "ServiceCases",
                column: "ClosedByUserId");

            migrationBuilder.CreateIndex(
                name: "IX_ServiceCases_CreatedByUserId",
                table: "ServiceCases",
                column: "CreatedByUserId");

            migrationBuilder.CreateIndex(
                name: "IX_ServiceCases_OriginStoreId",
                table: "ServiceCases",
                column: "OriginStoreId");

            migrationBuilder.CreateIndex(
                name: "IX_ServiceCases_Status",
                table: "ServiceCases",
                column: "Status");

            migrationBuilder.CreateIndex(
                name: "IX_ServiceCases_VendorId",
                table: "ServiceCases",
                column: "VendorId");

            migrationBuilder.CreateIndex(
                name: "UX_ServiceCases_CaseNumber",
                table: "ServiceCases",
                column: "CaseNumber",
                unique: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "ServiceCaseEvents");

            migrationBuilder.DropTable(
                name: "ServiceCases");
        }
    }
}
