using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace DeathStar.Infrastructure.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class ModifyAssetWithVendor : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_Assets_Vendors_VendorId",
                table: "Assets");

            migrationBuilder.DropIndex(
                name: "IX_Assets_VendorId",
                table: "Assets");

            migrationBuilder.RenameColumn(
                name: "VendorId",
                table: "Assets",
                newName: "AssignedVendorId");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.RenameColumn(
                name: "AssignedVendorId",
                table: "Assets",
                newName: "VendorId");

            migrationBuilder.CreateIndex(
                name: "IX_Assets_VendorId",
                table: "Assets",
                column: "VendorId");

            migrationBuilder.AddForeignKey(
                name: "FK_Assets_Vendors_VendorId",
                table: "Assets",
                column: "VendorId",
                principalTable: "Vendors",
                principalColumn: "VendorId");
        }
    }
}
