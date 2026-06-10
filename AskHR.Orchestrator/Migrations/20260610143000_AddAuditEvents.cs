using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace AskHR.Orchestrator.Migrations
{
    /// <inheritdoc />
    public partial class AddAuditEvents : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "AuditEvents",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    EventType = table.Column<string>(type: "nvarchar(128)", maxLength: 128, nullable: false),
                    AgentId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    UserId = table.Column<Guid>(type: "uniqueidentifier", nullable: true),
                    IsAnonymous = table.Column<bool>(type: "bit", nullable: false),
                    Channel = table.Column<string>(type: "nvarchar(64)", maxLength: 64, nullable: false),
                    MaskedText = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    TextHash = table.Column<string>(type: "nvarchar(64)", maxLength: 64, nullable: false),
                    Provider = table.Column<string>(type: "nvarchar(128)", maxLength: 128, nullable: true),
                    Model = table.Column<string>(type: "nvarchar(256)", maxLength: 256, nullable: true),
                    FallbackReason = table.Column<string>(type: "nvarchar(512)", maxLength: 512, nullable: true),
                    CitationCount = table.Column<int>(type: "int", nullable: false),
                    CreatedAt = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_AuditEvents", x => x.Id);
                });

            migrationBuilder.CreateIndex(
                name: "IX_AuditEvents_AgentId_Channel_CreatedAt",
                table: "AuditEvents",
                columns: new[] { "AgentId", "Channel", "CreatedAt" });

            migrationBuilder.CreateIndex(
                name: "IX_AuditEvents_CreatedAt",
                table: "AuditEvents",
                column: "CreatedAt");

            migrationBuilder.CreateIndex(
                name: "IX_AuditEvents_TextHash",
                table: "AuditEvents",
                column: "TextHash");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "AuditEvents");
        }
    }
}
