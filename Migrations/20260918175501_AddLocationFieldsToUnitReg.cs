using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace SmartProManWebAPI.Migrations
{
    /// <inheritdoc />
    public partial class AddLocationFieldsToUnitReg : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "Area",
                table: "UnitRegistrations",
                type: "nvarchar(max)",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "Street",
                table: "UnitRegistrations",
                type: "nvarchar(max)",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "Zone",
                table: "UnitRegistrations",
                type: "nvarchar(max)",
                nullable: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "Area",
                table: "UnitRegistrations");

            migrationBuilder.DropColumn(
                name: "Street",
                table: "UnitRegistrations");

            migrationBuilder.DropColumn(
                name: "Zone",
                table: "UnitRegistrations");
        }
    }
}
