using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace DeathStar.Infrastructure.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class ExtendApplicationUserWithEntraProfile : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AlterColumn<DateTime>(
                name: "LastLoginAtUtc",
                table: "ApplicationUsers",
                type: "datetime2",
                nullable: true,
                oldClrType: typeof(DateTime),
                oldType: "datetime2");

            migrationBuilder.AddColumn<string>(
                name: "Department",
                table: "ApplicationUsers",
                type: "nvarchar(200)",
                maxLength: 200,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "JobTitle",
                table: "ApplicationUsers",
                type: "nvarchar(200)",
                maxLength: 200,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "Mail",
                table: "ApplicationUsers",
                type: "nvarchar(320)",
                maxLength: 320,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "MobilePhone",
                table: "ApplicationUsers",
                type: "nvarchar(50)",
                maxLength: 50,
                nullable: true);

            migrationBuilder.CreateIndex(
                name: "IX_Assets_AssignedVendorId",
                table: "Assets",
                column: "AssignedVendorId");

            migrationBuilder.AddForeignKey(
                name: "FK_Assets_Vendors_AssignedVendorId",
                table: "Assets",
                column: "AssignedVendorId",
                principalTable: "Vendors",
                principalColumn: "VendorId",
                onDelete: ReferentialAction.SetNull);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_Assets_Vendors_AssignedVendorId",
                table: "Assets");

            migrationBuilder.DropIndex(
                name: "IX_Assets_AssignedVendorId",
                table: "Assets");

            migrationBuilder.DropColumn(
                name: "Department",
                table: "ApplicationUsers");

            migrationBuilder.DropColumn(
                name: "JobTitle",
                table: "ApplicationUsers");

            migrationBuilder.DropColumn(
                name: "Mail",
                table: "ApplicationUsers");

            migrationBuilder.DropColumn(
                name: "MobilePhone",
                table: "ApplicationUsers");

            migrationBuilder.AlterColumn<DateTime>(
                name: "LastLoginAtUtc",
                table: "ApplicationUsers",
                type: "datetime2",
                nullable: false,
                defaultValue: new DateTime(1, 1, 1, 0, 0, 0, 0, DateTimeKind.Unspecified),
                oldClrType: typeof(DateTime),
                oldType: "datetime2",
                oldNullable: true);
        }
    }
}
