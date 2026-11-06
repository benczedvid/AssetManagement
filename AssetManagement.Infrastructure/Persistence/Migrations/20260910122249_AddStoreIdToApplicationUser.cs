using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace DeathStar.Infrastructure.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class AddStoreIdToApplicationUser : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<Guid>(
                name: "StoreId",
                table: "ApplicationUsers",
                type: "uniqueidentifier",
                nullable: true);

            migrationBuilder.CreateIndex(
                name: "IX_ApplicationUsers_StoreId",
                table: "ApplicationUsers",
                column: "StoreId");

            migrationBuilder.AddForeignKey(
                name: "FK_ApplicationUsers_Stores_StoreId",
                table: "ApplicationUsers",
                column: "StoreId",
                principalTable: "Stores",
                principalColumn: "Id",
                onDelete: ReferentialAction.Restrict);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_ApplicationUsers_Stores_StoreId",
                table: "ApplicationUsers");

            migrationBuilder.DropIndex(
                name: "IX_ApplicationUsers_StoreId",
                table: "ApplicationUsers");

            migrationBuilder.DropColumn(
                name: "StoreId",
                table: "ApplicationUsers");
        }
    }
}
