using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace DeathStar.Infrastructure.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class ExtendAssetWithWarranty : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<DateOnly>(
                name: "WarrantyEndDate",
                table: "Assets",
                type: "date",
                nullable: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "WarrantyEndDate",
                table: "Assets");
        }
    }
}
