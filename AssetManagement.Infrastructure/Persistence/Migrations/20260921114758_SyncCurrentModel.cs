using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace DeathStar.Infrastructure.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class SyncCurrentModel : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_Contacts_Vendors_ContactId",
                table: "Contacts");

            migrationBuilder.AddForeignKey(
                name: "FK_Contacts_Vendors_VendorId",
                table: "Contacts",
                column: "VendorId",
                principalTable: "Vendors",
                principalColumn: "VendorId",
                onDelete: ReferentialAction.Restrict);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_Contacts_Vendors_VendorId",
                table: "Contacts");

            migrationBuilder.AddForeignKey(
                name: "FK_Contacts_Vendors_ContactId",
                table: "Contacts",
                column: "ContactId",
                principalTable: "Vendors",
                principalColumn: "VendorId",
                onDelete: ReferentialAction.Restrict);
        }
    }
}
