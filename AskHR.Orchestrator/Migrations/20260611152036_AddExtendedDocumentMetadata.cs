using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace AskHR.Orchestrator.Migrations
{
    /// <inheritdoc />
    public partial class AddExtendedDocumentMetadata : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "ApplicableLevels",
                table: "Documents",
                type: "nvarchar(max)",
                nullable: false,
                defaultValue: "[]");

            migrationBuilder.AddColumn<string>(
                name: "Countries",
                table: "Documents",
                type: "nvarchar(max)",
                nullable: false,
                defaultValue: "[]");

            migrationBuilder.AddColumn<DateTime>(
                name: "EffectiveDate",
                table: "Documents",
                type: "datetime2",
                nullable: true);

            migrationBuilder.AddColumn<DateTime>(
                name: "ExpiredDate",
                table: "Documents",
                type: "datetime2",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "LegalEntities",
                table: "Documents",
                type: "nvarchar(max)",
                nullable: false,
                defaultValue: "[]");

            migrationBuilder.AddColumn<string>(
                name: "Owner",
                table: "Documents",
                type: "nvarchar(max)",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "Version",
                table: "Documents",
                type: "nvarchar(max)",
                nullable: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "ApplicableLevels",
                table: "Documents");

            migrationBuilder.DropColumn(
                name: "Countries",
                table: "Documents");

            migrationBuilder.DropColumn(
                name: "EffectiveDate",
                table: "Documents");

            migrationBuilder.DropColumn(
                name: "ExpiredDate",
                table: "Documents");

            migrationBuilder.DropColumn(
                name: "LegalEntities",
                table: "Documents");

            migrationBuilder.DropColumn(
                name: "Owner",
                table: "Documents");

            migrationBuilder.DropColumn(
                name: "Version",
                table: "Documents");
        }
    }
}
