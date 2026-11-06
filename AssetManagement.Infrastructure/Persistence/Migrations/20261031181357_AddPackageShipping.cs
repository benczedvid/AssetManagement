using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace DeathStar.Infrastructure.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class AddPackageShipping : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "Packages",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    PackageNumber = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: false),
                    SourceStoreId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    DestinationStoreId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    Status = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: false),
                    CourierUserId = table.Column<Guid>(type: "uniqueidentifier", nullable: true),
                    Comment = table.Column<string>(type: "nvarchar(2000)", maxLength: 2000, nullable: true),
                    CreatedAtUtc = table.Column<DateTime>(type: "datetime2", nullable: false),
                    CreatedByUserId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    DispatchedAtUtc = table.Column<DateTime>(type: "datetime2", nullable: true),
                    DispatchedByUserId = table.Column<Guid>(type: "uniqueidentifier", nullable: true),
                    ReceivedAtUtc = table.Column<DateTime>(type: "datetime2", nullable: true),
                    ReceivedByUserId = table.Column<Guid>(type: "uniqueidentifier", nullable: true),
                    CancelledAtUtc = table.Column<DateTime>(type: "datetime2", nullable: true),
                    CancelledByUserId = table.Column<Guid>(type: "uniqueidentifier", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Packages", x => x.Id);
                    table.ForeignKey(
                        name: "FK_Packages_ApplicationUsers_CancelledByUserId",
                        column: x => x.CancelledByUserId,
                        principalTable: "ApplicationUsers",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_Packages_ApplicationUsers_CourierUserId",
                        column: x => x.CourierUserId,
                        principalTable: "ApplicationUsers",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_Packages_ApplicationUsers_CreatedByUserId",
                        column: x => x.CreatedByUserId,
                        principalTable: "ApplicationUsers",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_Packages_ApplicationUsers_DispatchedByUserId",
                        column: x => x.DispatchedByUserId,
                        principalTable: "ApplicationUsers",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_Packages_ApplicationUsers_ReceivedByUserId",
                        column: x => x.ReceivedByUserId,
                        principalTable: "ApplicationUsers",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_Packages_Stores_DestinationStoreId",
                        column: x => x.DestinationStoreId,
                        principalTable: "Stores",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_Packages_Stores_SourceStoreId",
                        column: x => x.SourceStoreId,
                        principalTable: "Stores",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "PackageEvents",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    PackageId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    Type = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: false),
                    PreviousStatus = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: true),
                    NewStatus = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: false),
                    Comment = table.Column<string>(type: "nvarchar(2000)", maxLength: 2000, nullable: true),
                    PerformedByUserId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    PerformedByUserName = table.Column<string>(type: "nvarchar(250)", maxLength: 250, nullable: false),
                    OccurredAtUtc = table.Column<DateTime>(type: "datetime2", nullable: false),
                    RelatedAssetId = table.Column<Guid>(type: "uniqueidentifier", nullable: true),
                    RelatedUserId = table.Column<Guid>(type: "uniqueidentifier", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_PackageEvents", x => x.Id);
                    table.ForeignKey(
                        name: "FK_PackageEvents_ApplicationUsers_PerformedByUserId",
                        column: x => x.PerformedByUserId,
                        principalTable: "ApplicationUsers",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_PackageEvents_ApplicationUsers_RelatedUserId",
                        column: x => x.RelatedUserId,
                        principalTable: "ApplicationUsers",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_PackageEvents_Assets_RelatedAssetId",
                        column: x => x.RelatedAssetId,
                        principalTable: "Assets",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_PackageEvents_Packages_PackageId",
                        column: x => x.PackageId,
                        principalTable: "Packages",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "PackageItems",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    PackageId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    AssetId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    AddedAtUtc = table.Column<DateTime>(type: "datetime2", nullable: false),
                    AddedByUserId = table.Column<Guid>(type: "uniqueidentifier", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_PackageItems", x => x.Id);
                    table.ForeignKey(
                        name: "FK_PackageItems_ApplicationUsers_AddedByUserId",
                        column: x => x.AddedByUserId,
                        principalTable: "ApplicationUsers",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_PackageItems_Assets_AssetId",
                        column: x => x.AssetId,
                        principalTable: "Assets",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_PackageItems_Packages_PackageId",
                        column: x => x.PackageId,
                        principalTable: "Packages",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "IX_PackageEvents_OccurredAtUtc",
                table: "PackageEvents",
                column: "OccurredAtUtc");

            migrationBuilder.CreateIndex(
                name: "IX_PackageEvents_PackageId",
                table: "PackageEvents",
                column: "PackageId");

            migrationBuilder.CreateIndex(
                name: "IX_PackageEvents_PackageId_OccurredAtUtc",
                table: "PackageEvents",
                columns: new[] { "PackageId", "OccurredAtUtc" });

            migrationBuilder.CreateIndex(
                name: "IX_PackageEvents_PerformedByUserId",
                table: "PackageEvents",
                column: "PerformedByUserId");

            migrationBuilder.CreateIndex(
                name: "IX_PackageEvents_RelatedAssetId",
                table: "PackageEvents",
                column: "RelatedAssetId");

            migrationBuilder.CreateIndex(
                name: "IX_PackageEvents_RelatedUserId",
                table: "PackageEvents",
                column: "RelatedUserId");

            migrationBuilder.CreateIndex(
                name: "IX_PackageEvents_Type",
                table: "PackageEvents",
                column: "Type");

            migrationBuilder.CreateIndex(
                name: "IX_PackageItems_AddedByUserId",
                table: "PackageItems",
                column: "AddedByUserId");

            migrationBuilder.CreateIndex(
                name: "IX_PackageItems_AssetId",
                table: "PackageItems",
                column: "AssetId");

            migrationBuilder.CreateIndex(
                name: "IX_PackageItems_PackageId",
                table: "PackageItems",
                column: "PackageId");

            migrationBuilder.CreateIndex(
                name: "UX_PackageItems_PackageId_AssetId",
                table: "PackageItems",
                columns: new[] { "PackageId", "AssetId" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_Packages_CancelledByUserId",
                table: "Packages",
                column: "CancelledByUserId");

            migrationBuilder.CreateIndex(
                name: "IX_Packages_CourierUserId",
                table: "Packages",
                column: "CourierUserId");

            migrationBuilder.CreateIndex(
                name: "IX_Packages_CreatedByUserId",
                table: "Packages",
                column: "CreatedByUserId");

            migrationBuilder.CreateIndex(
                name: "IX_Packages_DestinationStoreId",
                table: "Packages",
                column: "DestinationStoreId");

            migrationBuilder.CreateIndex(
                name: "IX_Packages_DestinationStoreId_Status",
                table: "Packages",
                columns: new[] { "DestinationStoreId", "Status" });

            migrationBuilder.CreateIndex(
                name: "IX_Packages_DispatchedByUserId",
                table: "Packages",
                column: "DispatchedByUserId");

            migrationBuilder.CreateIndex(
                name: "IX_Packages_ReceivedByUserId",
                table: "Packages",
                column: "ReceivedByUserId");

            migrationBuilder.CreateIndex(
                name: "IX_Packages_SourceStoreId",
                table: "Packages",
                column: "SourceStoreId");

            migrationBuilder.CreateIndex(
                name: "IX_Packages_SourceStoreId_Status",
                table: "Packages",
                columns: new[] { "SourceStoreId", "Status" });

            migrationBuilder.CreateIndex(
                name: "IX_Packages_Status",
                table: "Packages",
                column: "Status");

            migrationBuilder.CreateIndex(
                name: "UX_Packages_PackageNumber",
                table: "Packages",
                column: "PackageNumber",
                unique: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "PackageEvents");

            migrationBuilder.DropTable(
                name: "PackageItems");

            migrationBuilder.DropTable(
                name: "Packages");
        }
    }
}
