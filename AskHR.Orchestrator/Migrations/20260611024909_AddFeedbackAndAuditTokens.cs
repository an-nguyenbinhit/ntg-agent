using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace AskHR.Orchestrator.Migrations
{
    /// <inheritdoc />
    public partial class AddFeedbackAndAuditTokens : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<int>(
                name: "CompletionTokens",
                table: "AuditEvents",
                type: "int",
                nullable: true);

            migrationBuilder.AddColumn<long>(
                name: "LatencyMs",
                table: "AuditEvents",
                type: "bigint",
                nullable: false,
                defaultValue: 0L);

            migrationBuilder.AddColumn<int>(
                name: "PromptTokens",
                table: "AuditEvents",
                type: "int",
                nullable: true);

            migrationBuilder.AddColumn<int>(
                name: "TotalTokens",
                table: "AuditEvents",
                type: "int",
                nullable: true);

            migrationBuilder.CreateTable(
                name: "FeedbackEvents",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    MessageId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    UserId = table.Column<Guid>(type: "uniqueidentifier", nullable: true),
                    IsAnonymous = table.Column<bool>(type: "bit", nullable: false),
                    Rating = table.Column<string>(type: "nvarchar(32)", maxLength: 32, nullable: false),
                    CommentMasked = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    Topic = table.Column<string>(type: "nvarchar(128)", maxLength: 128, nullable: true),
                    SeverityCandidate = table.Column<string>(type: "nvarchar(32)", maxLength: 32, nullable: true),
                    Status = table.Column<string>(type: "nvarchar(32)", maxLength: 32, nullable: false),
                    CreatedAt = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_FeedbackEvents", x => x.Id);
                });

            migrationBuilder.CreateIndex(
                name: "IX_FeedbackEvents_CreatedAt",
                table: "FeedbackEvents",
                column: "CreatedAt");

            migrationBuilder.CreateIndex(
                name: "IX_FeedbackEvents_MessageId",
                table: "FeedbackEvents",
                column: "MessageId");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "FeedbackEvents");

            migrationBuilder.DropColumn(
                name: "CompletionTokens",
                table: "AuditEvents");

            migrationBuilder.DropColumn(
                name: "LatencyMs",
                table: "AuditEvents");

            migrationBuilder.DropColumn(
                name: "PromptTokens",
                table: "AuditEvents");

            migrationBuilder.DropColumn(
                name: "TotalTokens",
                table: "AuditEvents");
        }
    }
}
