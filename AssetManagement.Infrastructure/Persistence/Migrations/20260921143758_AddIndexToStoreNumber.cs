using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace DeathStar.Infrastructure.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class AddIndexToStoreNumber : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.RenameColumn(
                name: "STREET",
                table: "Stores",
                newName: "Street");

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

            migrationBuilder.CreateIndex(
                name: "IX_Stores_StoreNumber",
                table: "Stores",
                column: "StoreNumber",
                unique: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "IX_Stores_StoreNumber",
                table: "Stores");

            migrationBuilder.RenameColumn(
                name: "Street",
                table: "Stores",
                newName: "STREET");

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
        }
    }
}
