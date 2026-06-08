using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace AskHR.Orchestrator.Migrations
{
    /// <inheritdoc />
    public partial class AddDocumentMetadataAndIngestStatus : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "BusinessUnits",
                table: "Documents",
                type: "nvarchar(max)",
                nullable: false,
                defaultValue: "[]");

            migrationBuilder.AddColumn<string>(
                name: "IngestErrorMessage",
                table: "Documents",
                type: "nvarchar(max)",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "IngestStatus",
                table: "Documents",
                type: "nvarchar(32)",
                maxLength: 32,
                nullable: false,
                defaultValue: "Success");

            migrationBuilder.AddColumn<string>(
                name: "Roles",
                table: "Documents",
                type: "nvarchar(max)",
                nullable: false,
                defaultValue: "[]");

            migrationBuilder.AddColumn<string>(
                name: "SensitivityLevel",
                table: "Documents",
                type: "nvarchar(max)",
                nullable: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "BusinessUnits",
                table: "Documents");

            migrationBuilder.DropColumn(
                name: "IngestErrorMessage",
                table: "Documents");

            migrationBuilder.DropColumn(
                name: "IngestStatus",
                table: "Documents");

            migrationBuilder.DropColumn(
                name: "Roles",
                table: "Documents");

            migrationBuilder.DropColumn(
                name: "SensitivityLevel",
                table: "Documents");
        }
    }
}
