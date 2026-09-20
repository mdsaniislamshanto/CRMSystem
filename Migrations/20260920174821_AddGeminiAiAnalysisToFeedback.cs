using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace CRMSystem.Migrations
{
    /// <inheritdoc />
    public partial class AddGeminiAiAnalysisToFeedback : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<DateTime>(
                name: "AiAnalyzedAt",
                table: "Feedbacks",
                type: "datetime(6)",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "AiBuyingIntent",
                table: "Feedbacks",
                type: "varchar(50)",
                maxLength: 50,
                nullable: true)
                .Annotation("MySql:CharSet", "utf8mb4");

            migrationBuilder.AddColumn<bool>(
                name: "AiCustomerInterested",
                table: "Feedbacks",
                type: "tinyint(1)",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "AiSentiment",
                table: "Feedbacks",
                type: "varchar(50)",
                maxLength: 50,
                nullable: true)
                .Annotation("MySql:CharSet", "utf8mb4");

            migrationBuilder.AddColumn<bool>(
                name: "AiSiteVisitInterested",
                table: "Feedbacks",
                type: "tinyint(1)",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "AiSummary",
                table: "Feedbacks",
                type: "varchar(2000)",
                maxLength: 2000,
                nullable: true)
                .Annotation("MySql:CharSet", "utf8mb4");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "AiAnalyzedAt",
                table: "Feedbacks");

            migrationBuilder.DropColumn(
                name: "AiBuyingIntent",
                table: "Feedbacks");

            migrationBuilder.DropColumn(
                name: "AiCustomerInterested",
                table: "Feedbacks");

            migrationBuilder.DropColumn(
                name: "AiSentiment",
                table: "Feedbacks");

            migrationBuilder.DropColumn(
                name: "AiSiteVisitInterested",
                table: "Feedbacks");

            migrationBuilder.DropColumn(
                name: "AiSummary",
                table: "Feedbacks");
        }
    }
}
