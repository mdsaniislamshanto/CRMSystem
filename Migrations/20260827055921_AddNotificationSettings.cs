using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace CRMSystem.Migrations
{
    /// <inheritdoc />
    public partial class AddNotificationSettings : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<bool>(
                name: "NotificationsEnabled",
                table: "Users",
                type: "tinyint(1)",
                nullable: false,
                defaultValue: false);

            migrationBuilder.AddColumn<bool>(
                name: "FeedbackNotificationEnabled",
                table: "SystemSettings",
                type: "tinyint(1)",
                nullable: false,
                defaultValue: false);

            migrationBuilder.AddColumn<bool>(
                name: "FollowUpNotificationEnabled",
                table: "SystemSettings",
                type: "tinyint(1)",
                nullable: false,
                defaultValue: false);

            migrationBuilder.AddColumn<bool>(
                name: "GlobalNotificationsEnabled",
                table: "SystemSettings",
                type: "tinyint(1)",
                nullable: false,
                defaultValue: false);

            migrationBuilder.AddColumn<bool>(
                name: "LeadAssignmentNotificationEnabled",
                table: "SystemSettings",
                type: "tinyint(1)",
                nullable: false,
                defaultValue: false);

            migrationBuilder.AddColumn<bool>(
                name: "OverdueNotificationEnabled",
                table: "SystemSettings",
                type: "tinyint(1)",
                nullable: false,
                defaultValue: false);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "NotificationsEnabled",
                table: "Users");

            migrationBuilder.DropColumn(
                name: "FeedbackNotificationEnabled",
                table: "SystemSettings");

            migrationBuilder.DropColumn(
                name: "FollowUpNotificationEnabled",
                table: "SystemSettings");

            migrationBuilder.DropColumn(
                name: "GlobalNotificationsEnabled",
                table: "SystemSettings");

            migrationBuilder.DropColumn(
                name: "LeadAssignmentNotificationEnabled",
                table: "SystemSettings");

            migrationBuilder.DropColumn(
                name: "OverdueNotificationEnabled",
                table: "SystemSettings");
        }
    }
}
