using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace DeathStar.Infrastructure.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class AddStoreIdToAssetMovement : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "IX_AssetMovements_AssetId",
                table: "AssetMovements");

            migrationBuilder.AlterColumn<string>(
                name: "Type",
                table: "AssetMovements",
                type: "nvarchar(20)",
                maxLength: 20,
                nullable: false,
                oldClrType: typeof(string),
                oldType: "nvarchar(50)",
                oldMaxLength: 50);

            migrationBuilder.AlterColumn<string>(
                name: "EmployeeNumber",
                table: "AssetMovements",
                type: "nvarchar(10)",
                maxLength: 10,
                nullable: false,
                oldClrType: typeof(string),
                oldType: "nvarchar(50)",
                oldMaxLength: 50);

            migrationBuilder.AddColumn<Guid>(
                name: "StoreId",
                table: "AssetMovements",
                type: "uniqueidentifier",
                nullable: false,
                defaultValue: new Guid("00000000-0000-0000-0000-000000000000"));

            migrationBuilder.CreateIndex(
                name: "IX_AssetMovements_EmployeeNumber_CreatedAtUtc",
                table: "AssetMovements",
                columns: new[] { "EmployeeNumber", "CreatedAtUtc" });

            migrationBuilder.CreateIndex(
                name: "IX_AssetMovements_StoreId_CreatedAtUtc",
                table: "AssetMovements",
                columns: new[] { "StoreId", "CreatedAtUtc" });

            migrationBuilder.AddForeignKey(
                name: "FK_AssetMovements_Assets_AssetId",
                table: "AssetMovements",
                column: "AssetId",
                principalTable: "Assets",
                principalColumn: "Id",
                onDelete: ReferentialAction.Restrict);

            migrationBuilder.AddForeignKey(
                name: "FK_AssetMovements_Stores_StoreId",
                table: "AssetMovements",
                column: "StoreId",
                principalTable: "Stores",
                principalColumn: "Id",
                onDelete: ReferentialAction.Restrict);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_AssetMovements_Assets_AssetId",
                table: "AssetMovements");

            migrationBuilder.DropForeignKey(
                name: "FK_AssetMovements_Stores_StoreId",
                table: "AssetMovements");

            migrationBuilder.DropIndex(
                name: "IX_AssetMovements_EmployeeNumber_CreatedAtUtc",
                table: "AssetMovements");

            migrationBuilder.DropIndex(
                name: "IX_AssetMovements_StoreId_CreatedAtUtc",
                table: "AssetMovements");

            migrationBuilder.DropColumn(
                name: "StoreId",
                table: "AssetMovements");

            migrationBuilder.AlterColumn<string>(
                name: "Type",
                table: "AssetMovements",
                type: "nvarchar(50)",
                maxLength: 50,
                nullable: false,
                oldClrType: typeof(string),
                oldType: "nvarchar(20)",
                oldMaxLength: 20);

            migrationBuilder.AlterColumn<string>(
                name: "EmployeeNumber",
                table: "AssetMovements",
                type: "nvarchar(50)",
                maxLength: 50,
                nullable: false,
                oldClrType: typeof(string),
                oldType: "nvarchar(10)",
                oldMaxLength: 10);

            migrationBuilder.CreateIndex(
                name: "IX_AssetMovements_AssetId",
                table: "AssetMovements",
                column: "AssetId");
        }
    }
}
