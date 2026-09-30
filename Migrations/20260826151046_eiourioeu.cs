using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace SmartProManWebAPI.Migrations
{
    /// <inheritdoc />
    public partial class eiourioeu : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<DateTime>(
                name: "BidOpenedAt",
                table: "Requests",
                type: "datetime2",
                nullable: true);

            migrationBuilder.AddColumn<int>(
                name: "BroadcastZoneLevel",
                table: "Requests",
                type: "int",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.AddColumn<int>(
                name: "CompanyID",
                table: "Requests",
                type: "int",
                nullable: true);

            migrationBuilder.AddColumn<int>(
                name: "JobID",
                table: "Requests",
                type: "int",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "Status",
                table: "Requests",
                type: "nvarchar(30)",
                maxLength: 30,
                nullable: false,
                defaultValue: "");

            migrationBuilder.AlterColumn<int>(
                name: "JobID",
                table: "DispatchBids",
                type: "int",
                nullable: true,
                oldClrType: typeof(int),
                oldType: "int");

            migrationBuilder.AddColumn<int>(
                name: "RequestID",
                table: "DispatchBids",
                type: "int",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.CreateIndex(
                name: "IX_Requests_CompanyID",
                table: "Requests",
                column: "CompanyID");

            migrationBuilder.CreateIndex(
                name: "IX_Requests_JobID",
                table: "Requests",
                column: "JobID");

            migrationBuilder.CreateIndex(
                name: "IX_DispatchBids_RequestID",
                table: "DispatchBids",
                column: "RequestID");

            migrationBuilder.AddForeignKey(
                name: "FK_DispatchBids_Requests_RequestID",
                table: "DispatchBids",
                column: "RequestID",
                principalTable: "Requests",
                principalColumn: "RequestID",
                onDelete: ReferentialAction.Cascade);

            migrationBuilder.AddForeignKey(
                name: "FK_Requests_Companies_CompanyID",
                table: "Requests",
                column: "CompanyID",
                principalTable: "Companies",
                principalColumn: "CompanyID",
                onDelete: ReferentialAction.Cascade);

            migrationBuilder.AddForeignKey(
                name: "FK_Requests_Jobs_JobID",
                table: "Requests",
                column: "JobID",
                principalTable: "Jobs",
                principalColumn: "JobID",
                onDelete: ReferentialAction.Cascade);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_DispatchBids_Requests_RequestID",
                table: "DispatchBids");

            migrationBuilder.DropForeignKey(
                name: "FK_Requests_Companies_CompanyID",
                table: "Requests");

            migrationBuilder.DropForeignKey(
                name: "FK_Requests_Jobs_JobID",
                table: "Requests");

            migrationBuilder.DropIndex(
                name: "IX_Requests_CompanyID",
                table: "Requests");

            migrationBuilder.DropIndex(
                name: "IX_Requests_JobID",
                table: "Requests");

            migrationBuilder.DropIndex(
                name: "IX_DispatchBids_RequestID",
                table: "DispatchBids");

            migrationBuilder.DropColumn(
                name: "BidOpenedAt",
                table: "Requests");

            migrationBuilder.DropColumn(
                name: "BroadcastZoneLevel",
                table: "Requests");

            migrationBuilder.DropColumn(
                name: "CompanyID",
                table: "Requests");

            migrationBuilder.DropColumn(
                name: "JobID",
                table: "Requests");

            migrationBuilder.DropColumn(
                name: "Status",
                table: "Requests");

            migrationBuilder.DropColumn(
                name: "RequestID",
                table: "DispatchBids");

            migrationBuilder.AlterColumn<int>(
                name: "JobID",
                table: "DispatchBids",
                type: "int",
                nullable: false,
                defaultValue: 0,
                oldClrType: typeof(int),
                oldType: "int",
                oldNullable: true);
        }
    }
}
