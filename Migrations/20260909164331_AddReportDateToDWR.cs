using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace SmartProManWebAPI.Migrations
{
    /// <inheritdoc />
    public partial class AddReportDateToDWR : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<DateTime>(
                name: "ReportDate",
                table: "DailyWorkReports",
                type: "datetime2",
                nullable: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "ReportDate",
                table: "DailyWorkReports");
        }
    }
}
