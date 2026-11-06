using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace DeathStar.Infrastructure.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class ExtendStore : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.RenameColumn(
                name: "TypeOfRoad",
                table: "Stores",
                newName: "PublicSpace");

            migrationBuilder.AlterColumn<string>(
                name: "StoreNumber",
                table: "Stores",
                type: "nvarchar(10)",
                maxLength: 10,
                nullable: false,
                oldClrType: typeof(int),
                oldType: "int");

            migrationBuilder.AlterColumn<string>(
                name: "HouseNumber",
                table: "Stores",
                type: "nvarchar(10)",
                maxLength: 10,
                nullable: false,
                oldClrType: typeof(int),
                oldType: "int");

            migrationBuilder.AddColumn<string>(
                name: "CountryCode",
                table: "Stores",
                type: "nvarchar(5)",
                maxLength: 5,
                nullable: false,
                defaultValue: "");

            migrationBuilder.AddColumn<string>(
                name: "PostalCode",
                table: "Stores",
                type: "nvarchar(max)",
                nullable: false,
                defaultValue: "");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "CountryCode",
                table: "Stores");

            migrationBuilder.DropColumn(
                name: "PostalCode",
                table: "Stores");

            migrationBuilder.RenameColumn(
                name: "PublicSpace",
                table: "Stores",
                newName: "TypeOfRoad");

            migrationBuilder.AlterColumn<int>(
                name: "StoreNumber",
                table: "Stores",
                type: "int",
                nullable: false,
                oldClrType: typeof(string),
                oldType: "nvarchar(10)",
                oldMaxLength: 10);

            migrationBuilder.AlterColumn<int>(
                name: "HouseNumber",
                table: "Stores",
                type: "int",
                nullable: false,
                oldClrType: typeof(string),
                oldType: "nvarchar(10)",
                oldMaxLength: 10);
        }
    }
}
