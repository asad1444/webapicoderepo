using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace SmartProManWebAPI.Migrations
{
    /// <inheritdoc />
    public partial class AddUnitRegQrAndFields : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "Floor",
                table: "UnitRegistrations",
                type: "nvarchar(max)",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "FloorZone",
                table: "UnitRegistrations",
                type: "nvarchar(max)",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "QrCodeData",
                table: "UnitRegistrations",
                type: "nvarchar(max)",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "ServiceName",
                table: "UnitRegistrations",
                type: "nvarchar(max)",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "TopZone",
                table: "UnitRegistrations",
                type: "nvarchar(max)",
                nullable: true);

            migrationBuilder.AlterColumn<string>(
                name: "FilePath",
                table: "UnitRegistrationMedia",
                type: "nvarchar(max)",
                nullable: false,
                oldClrType: typeof(string),
                oldType: "nvarchar(500)",
                oldMaxLength: 500);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "Floor",
                table: "UnitRegistrations");

            migrationBuilder.DropColumn(
                name: "FloorZone",
                table: "UnitRegistrations");

            migrationBuilder.DropColumn(
                name: "QrCodeData",
                table: "UnitRegistrations");

            migrationBuilder.DropColumn(
                name: "ServiceName",
                table: "UnitRegistrations");

            migrationBuilder.DropColumn(
                name: "TopZone",
                table: "UnitRegistrations");

            migrationBuilder.AlterColumn<string>(
                name: "FilePath",
                table: "UnitRegistrationMedia",
                type: "nvarchar(500)",
                maxLength: 500,
                nullable: false,
                oldClrType: typeof(string),
                oldType: "nvarchar(max)");
        }
    }
}
