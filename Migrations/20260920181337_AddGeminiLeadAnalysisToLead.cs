using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace CRMSystem.Migrations
{
    /// <inheritdoc />
    public partial class AddGeminiLeadAnalysisToLead : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<DateTime>(
                name: "AiAnalyzedAt",
                table: "Leads",
                type: "datetime(6)",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "AiBuyingDecision",
                table: "Leads",
                type: "varchar(50)",
                maxLength: 50,
                nullable: true)
                .Annotation("MySql:CharSet", "utf8mb4");

            migrationBuilder.AddColumn<string>(
                name: "AiInterestLevel",
                table: "Leads",
                type: "varchar(50)",
                maxLength: 50,
                nullable: true)
                .Annotation("MySql:CharSet", "utf8mb4");

            migrationBuilder.AddColumn<string>(
                name: "AiLeadAuthenticity",
                table: "Leads",
                type: "varchar(50)",
                maxLength: 50,
                nullable: true)
                .Annotation("MySql:CharSet", "utf8mb4");

            migrationBuilder.AddColumn<string>(
                name: "AiSynthesisSummary",
                table: "Leads",
                type: "varchar(3000)",
                maxLength: 3000,
                nullable: true)
                .Annotation("MySql:CharSet", "utf8mb4");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "AiAnalyzedAt",
                table: "Leads");

            migrationBuilder.DropColumn(
                name: "AiBuyingDecision",
                table: "Leads");

            migrationBuilder.DropColumn(
                name: "AiInterestLevel",
                table: "Leads");

            migrationBuilder.DropColumn(
                name: "AiLeadAuthenticity",
                table: "Leads");

            migrationBuilder.DropColumn(
                name: "AiSynthesisSummary",
                table: "Leads");
        }
    }
}
