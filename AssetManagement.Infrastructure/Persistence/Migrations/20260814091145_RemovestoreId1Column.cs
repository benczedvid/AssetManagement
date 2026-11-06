using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace DeathStar.Infrastructure.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class RemovestoreId1Column : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_Employees_Stores_StoreId1",
                table: "Employees");

            migrationBuilder.DropIndex(
                name: "IX_Employees_EmployeeNumber",
                table: "Employees");

            migrationBuilder.DropIndex(
                name: "IX_Employees_StoreId1",
                table: "Employees");

            migrationBuilder.DropColumn(
                name: "StoreId1",
                table: "Employees");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<Guid>(
                name: "StoreId1",
                table: "Employees",
                type: "uniqueidentifier",
                nullable: false,
                defaultValue: new Guid("00000000-0000-0000-0000-000000000000"));

            migrationBuilder.CreateIndex(
                name: "IX_Employees_EmployeeNumber",
                table: "Employees",
                column: "EmployeeNumber");

            migrationBuilder.CreateIndex(
                name: "IX_Employees_StoreId1",
                table: "Employees",
                column: "StoreId1");

            migrationBuilder.AddForeignKey(
                name: "FK_Employees_Stores_StoreId1",
                table: "Employees",
                column: "StoreId1",
                principalTable: "Stores",
                principalColumn: "Id",
                onDelete: ReferentialAction.Cascade);
        }
    }
}
