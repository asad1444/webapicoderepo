using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace SmartProManWebAPI.Migrations
{
    /// <inheritdoc />
    public partial class AddJobAssignmentSource : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "AssignmentSource",
                table: "Jobs",
                type: "nvarchar(20)",
                maxLength: 20,
                nullable: false,
                defaultValue: "Company");

            migrationBuilder.Sql(@"
                UPDATE j
                SET AssignmentSource = 'Foreman'
                FROM Jobs j
                INNER JOIN Technicians t ON t.TechnicianID = j.TechnicianID
                WHERE t.Designation = 'Foreman';
            ");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "AssignmentSource",
                table: "Jobs");
        }
    }
}
