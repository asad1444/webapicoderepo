using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace SmartProManWebAPI.Migrations
{
    /// <inheritdoc />
    public partial class AddPriorityToRequest : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "Priority",
                table: "Requests",
                type: "nvarchar(20)",
                maxLength: 20,
                nullable: false,
                defaultValue: "");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "Priority",
                table: "Requests");
        }
    }
}
