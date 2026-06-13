using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace AskHR.Orchestrator.Migrations
{
    /// <inheritdoc />
    public partial class AddAgentMaxTokens : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<int>(
                name: "MaxTokens",
                table: "Agents",
                type: "int",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.UpdateData(
                table: "Agents",
                keyColumn: "Id",
                keyValue: new Guid("31cf1546-e9c9-4d95-a8e5-3c7c7570fec5"),
                column: "MaxTokens",
                value: 2000);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "MaxTokens",
                table: "Agents");
        }
    }
}
