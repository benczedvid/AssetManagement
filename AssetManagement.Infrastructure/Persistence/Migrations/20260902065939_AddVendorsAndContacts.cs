using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace DeathStar.Infrastructure.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class AddVendorsAndContacts : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "IX_Stores_StoreNumber",
                table: "Stores");

            migrationBuilder.RenameColumn(
                name: "CountryCode",
                table: "Stores",
                newName: "COUNTRYCODE");

            migrationBuilder.RenameColumn(
                name: "City",
                table: "Stores",
                newName: "CITY");

            migrationBuilder.RenameColumn(
                name: "PublicSpace",
                table: "Stores",
                newName: "PUBLIC_SPACE");

            migrationBuilder.RenameColumn(
                name: "PostalCode",
                table: "Stores",
                newName: "POSTAL_CODE");

            migrationBuilder.RenameColumn(
                name: "HouseNumber",
                table: "Stores",
                newName: "HOUSE_NUMBER");

            migrationBuilder.RenameColumn(
                name: "Address",
                table: "Stores",
                newName: "STREET");

            migrationBuilder.AlterColumn<string>(
                name: "COUNTRYCODE",
                table: "Stores",
                type: "nvarchar(5)",
                maxLength: 5,
                nullable: false,
                oldClrType: typeof(string),
                oldType: "nvarchar(3)",
                oldMaxLength: 3);

            migrationBuilder.AlterColumn<string>(
                name: "PUBLIC_SPACE",
                table: "Stores",
                type: "nvarchar(20)",
                maxLength: 20,
                nullable: false,
                oldClrType: typeof(string),
                oldType: "nvarchar(50)",
                oldMaxLength: 50);

            migrationBuilder.AlterColumn<string>(
                name: "POSTAL_CODE",
                table: "Stores",
                type: "nvarchar(5)",
                maxLength: 5,
                nullable: false,
                oldClrType: typeof(string),
                oldType: "nvarchar(max)");

            migrationBuilder.AddColumn<Guid>(
                name: "VendorId",
                table: "Assets",
                type: "uniqueidentifier",
                nullable: true);

            migrationBuilder.CreateTable(
                name: "Vendors",
                columns: table => new
                {
                    VendorId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    Name = table.Column<string>(type: "nvarchar(200)", maxLength: 200, nullable: false),
                    Webpage = table.Column<string>(type: "nvarchar(2048)", maxLength: 2048, nullable: true),
                    City = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: false),
                    CountryCode = table.Column<string>(type: "nvarchar(3)", maxLength: 3, nullable: false),
                    HouseNumber = table.Column<string>(type: "nvarchar(10)", maxLength: 10, nullable: false),
                    PostalCode = table.Column<string>(type: "nvarchar(10)", maxLength: 10, nullable: false),
                    PublicSpace = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: false),
                    Street = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Vendors", x => x.VendorId);
                });

            migrationBuilder.CreateTable(
                name: "Contacts",
                columns: table => new
                {
                    ContactId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    VendorId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    FirstName = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: false),
                    LastName = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: false),
                    JobTitle = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: false),
                    EmailAddress = table.Column<string>(type: "nvarchar(200)", maxLength: 200, nullable: false),
                    PhoneNumber = table.Column<string>(type: "nvarchar(30)", maxLength: 30, nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Contacts", x => x.ContactId);
                    table.ForeignKey(
                        name: "FK_Contacts_Vendors_ContactId",
                        column: x => x.ContactId,
                        principalTable: "Vendors",
                        principalColumn: "VendorId",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateIndex(
                name: "IX_Assets_VendorId",
                table: "Assets",
                column: "VendorId");

            migrationBuilder.CreateIndex(
                name: "IX_Contacts_VendorId_PhoneNumber",
                table: "Contacts",
                columns: new[] { "VendorId", "PhoneNumber" },
                unique: true);

            migrationBuilder.AddForeignKey(
                name: "FK_Assets_Vendors_VendorId",
                table: "Assets",
                column: "VendorId",
                principalTable: "Vendors",
                principalColumn: "VendorId");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_Assets_Vendors_VendorId",
                table: "Assets");

            migrationBuilder.DropTable(
                name: "Contacts");

            migrationBuilder.DropTable(
                name: "Vendors");

            migrationBuilder.DropIndex(
                name: "IX_Assets_VendorId",
                table: "Assets");

            migrationBuilder.DropColumn(
                name: "VendorId",
                table: "Assets");

            migrationBuilder.RenameColumn(
                name: "COUNTRYCODE",
                table: "Stores",
                newName: "CountryCode");

            migrationBuilder.RenameColumn(
                name: "CITY",
                table: "Stores",
                newName: "City");

            migrationBuilder.RenameColumn(
                name: "PUBLIC_SPACE",
                table: "Stores",
                newName: "PublicSpace");

            migrationBuilder.RenameColumn(
                name: "POSTAL_CODE",
                table: "Stores",
                newName: "PostalCode");

            migrationBuilder.RenameColumn(
                name: "HOUSE_NUMBER",
                table: "Stores",
                newName: "HouseNumber");

            migrationBuilder.RenameColumn(
                name: "STREET",
                table: "Stores",
                newName: "Address");

            migrationBuilder.AlterColumn<string>(
                name: "CountryCode",
                table: "Stores",
                type: "nvarchar(3)",
                maxLength: 3,
                nullable: false,
                oldClrType: typeof(string),
                oldType: "nvarchar(5)",
                oldMaxLength: 5);

            migrationBuilder.AlterColumn<string>(
                name: "PublicSpace",
                table: "Stores",
                type: "nvarchar(50)",
                maxLength: 50,
                nullable: false,
                oldClrType: typeof(string),
                oldType: "nvarchar(20)",
                oldMaxLength: 20);

            migrationBuilder.AlterColumn<string>(
                name: "PostalCode",
                table: "Stores",
                type: "nvarchar(max)",
                nullable: false,
                oldClrType: typeof(string),
                oldType: "nvarchar(5)",
                oldMaxLength: 5);

            migrationBuilder.CreateIndex(
                name: "IX_Stores_StoreNumber",
                table: "Stores",
                column: "StoreNumber",
                unique: true);
        }
    }
}
