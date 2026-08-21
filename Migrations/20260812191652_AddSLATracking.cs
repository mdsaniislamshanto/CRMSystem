using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace CRMSystem.Migrations
{
    /// <inheritdoc />
    public partial class AddSLATracking : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<bool>(
                name: "AcceptanceSLAMissed",
                table: "LeadAssignments",
                type: "tinyint(1)",
                nullable: false,
                defaultValue: false);

            migrationBuilder.AddColumn<bool>(
                name: "FirstFeedbackSLAMissed",
                table: "LeadAssignments",
                type: "tinyint(1)",
                nullable: false,
                defaultValue: false);

            migrationBuilder.AddColumn<bool>(
                name: "NextFeedbackSLAMissed",
                table: "Feedbacks",
                type: "tinyint(1)",
                nullable: false,
                defaultValue: false);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "AcceptanceSLAMissed",
                table: "LeadAssignments");

            migrationBuilder.DropColumn(
                name: "FirstFeedbackSLAMissed",
                table: "LeadAssignments");

            migrationBuilder.DropColumn(
                name: "NextFeedbackSLAMissed",
                table: "Feedbacks");
        }
    }
}
