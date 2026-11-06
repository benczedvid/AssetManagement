using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace DeathStar.Infrastructure.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class AddAssetHistoryAudit : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "AssetHistories",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    AssetId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    Action = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: false),
                    PropertyName = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: false),
                    OldValue = table.Column<string>(type: "nvarchar(2000)", maxLength: 2000, nullable: true),
                    NewValue = table.Column<string>(type: "nvarchar(2000)", maxLength: 2000, nullable: true),
                    ChangedByUserId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    ChangedByUserName = table.Column<string>(type: "nvarchar(256)", maxLength: 256, nullable: false),
                    ChangedAtUtc = table.Column<DateTime>(type: "datetime2", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_AssetHistories", x => x.Id);
                });

            migrationBuilder.CreateIndex(
                name: "IX_AssetHistories_AssetId",
                table: "AssetHistories",
                column: "AssetId");

            migrationBuilder.CreateIndex(
                name: "IX_AssetHistories_AssetId_ChangedAtUtc",
                table: "AssetHistories",
                columns: new[] { "AssetId", "ChangedAtUtc" });
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "AssetHistories");
        }
    }
}
