using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace SmartProManWebAPI.Migrations
{
    /// <inheritdoc />
    public partial class AddRequestLinkAndNewFields : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "ServiceAddress",
                table: "Requests",
                type: "nvarchar(max)",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "SpecialInstructions",
                table: "Requests",
                type: "nvarchar(max)",
                nullable: true);

            migrationBuilder.AlterColumn<int>(
                name: "ClientID",
                table: "Jobs",
                type: "int",
                nullable: true,
                oldClrType: typeof(int),
                oldType: "int");

            migrationBuilder.AddColumn<int>(
                name: "RequestID",
                table: "Jobs",
                type: "int",
                nullable: true);

            migrationBuilder.CreateIndex(
                name: "IX_Jobs_RequestID",
                table: "Jobs",
                column: "RequestID");

            migrationBuilder.AddForeignKey(
                name: "FK_Jobs_Requests_RequestID",
                table: "Jobs",
                column: "RequestID",
                principalTable: "Requests",
                principalColumn: "RequestID",
                onDelete: ReferentialAction.NoAction);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_Jobs_Requests_RequestID",
                table: "Jobs");

            migrationBuilder.DropIndex(
                name: "IX_Jobs_RequestID",
                table: "Jobs");

            migrationBuilder.DropColumn(
                name: "ServiceAddress",
                table: "Requests");

            migrationBuilder.DropColumn(
                name: "SpecialInstructions",
                table: "Requests");

            migrationBuilder.DropColumn(
                name: "RequestID",
                table: "Jobs");

            migrationBuilder.AlterColumn<int>(
                name: "ClientID",
                table: "Jobs",
                type: "int",
                nullable: false,
                defaultValue: 0,
                oldClrType: typeof(int),
                oldType: "int",
                oldNullable: true);
        }
    }
}
