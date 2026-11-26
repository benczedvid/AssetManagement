using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace DeathStar.Infrastructure.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class MakeContactStandalone : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_Contacts_Vendors_VendorId",
                table: "Contacts");

            migrationBuilder.DropIndex(
                name: "IX_Contacts_VendorId_PhoneNumber",
                table: "Contacts");

            migrationBuilder.AlterColumn<Guid>(
                name: "VendorId",
                table: "Contacts",
                type: "uniqueidentifier",
                nullable: true,
                oldClrType: typeof(Guid),
                oldType: "uniqueidentifier");

            migrationBuilder.CreateIndex(
                name: "IX_Contacts_VendorId_PhoneNumber",
                table: "Contacts",
                columns: new[] { "VendorId", "PhoneNumber" },
                unique: true,
                filter: "[VendorId] IS NOT NULL");

            migrationBuilder.AddForeignKey(
                name: "FK_Contacts_Vendors_VendorId",
                table: "Contacts",
                column: "VendorId",
                principalTable: "Vendors",
                principalColumn: "VendorId",
                onDelete: ReferentialAction.SetNull);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            // Contacts that are not attached to any vendor cannot be represented in the
            // previous schema (VendorId was required), so they are removed on rollback.
            migrationBuilder.Sql("DELETE FROM [Contacts] WHERE [VendorId] IS NULL;");

            migrationBuilder.DropForeignKey(
                name: "FK_Contacts_Vendors_VendorId",
                table: "Contacts");

            migrationBuilder.DropIndex(
                name: "IX_Contacts_VendorId_PhoneNumber",
                table: "Contacts");

            migrationBuilder.AlterColumn<Guid>(
                name: "VendorId",
                table: "Contacts",
                type: "uniqueidentifier",
                nullable: false,
                defaultValue: new Guid("00000000-0000-0000-0000-000000000000"),
                oldClrType: typeof(Guid),
                oldType: "uniqueidentifier",
                oldNullable: true);

            migrationBuilder.CreateIndex(
                name: "IX_Contacts_VendorId_PhoneNumber",
                table: "Contacts",
                columns: new[] { "VendorId", "PhoneNumber" },
                unique: true);

            migrationBuilder.AddForeignKey(
                name: "FK_Contacts_Vendors_VendorId",
                table: "Contacts",
                column: "VendorId",
                principalTable: "Vendors",
                principalColumn: "VendorId",
                onDelete: ReferentialAction.Restrict);
        }
    }
}
