using System;
using AskHR.Orchestrator.Data;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace AskHR.Orchestrator.Migrations
{
    /// <inheritdoc />
    [DbContext(typeof(AgentDbContext))]
    [Migration("20260611120000_AddDocumentApprovalFields")]
    public partial class AddDocumentApprovalFields : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "ApprovalStatus",
                table: "Documents",
                type: "nvarchar(32)",
                maxLength: 32,
                nullable: false,
                defaultValue: "Approved");

            migrationBuilder.AddColumn<Guid>(
                name: "ApprovedByUserId",
                table: "Documents",
                type: "uniqueidentifier",
                nullable: true);

            migrationBuilder.AddColumn<DateTime>(
                name: "NextReviewDate",
                table: "Documents",
                type: "datetime2",
                nullable: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "ApprovalStatus",
                table: "Documents");

            migrationBuilder.DropColumn(
                name: "ApprovedByUserId",
                table: "Documents");

            migrationBuilder.DropColumn(
                name: "NextReviewDate",
                table: "Documents");
        }
    }
}
