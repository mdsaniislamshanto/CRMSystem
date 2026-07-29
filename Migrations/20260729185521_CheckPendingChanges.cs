using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace CRMSystem.Migrations
{
    /// <inheritdoc />
    public partial class CheckPendingChanges : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_LeadAssignments_Leads_LeadId",
                table: "LeadAssignments");

            migrationBuilder.DropForeignKey(
                name: "FK_LeadAssignments_Users_AssignedBy",
                table: "LeadAssignments");

            migrationBuilder.DropForeignKey(
                name: "FK_LeadAssignments_Users_SalesOfficerId",
                table: "LeadAssignments");

            migrationBuilder.AddForeignKey(
                name: "FK_LeadAssignments_Leads_LeadId",
                table: "LeadAssignments",
                column: "LeadId",
                principalTable: "Leads",
                principalColumn: "LeadId",
                onDelete: ReferentialAction.Restrict);

            migrationBuilder.AddForeignKey(
                name: "FK_LeadAssignments_Users_AssignedBy",
                table: "LeadAssignments",
                column: "AssignedBy",
                principalTable: "Users",
                principalColumn: "UserId",
                onDelete: ReferentialAction.Restrict);

            migrationBuilder.AddForeignKey(
                name: "FK_LeadAssignments_Users_SalesOfficerId",
                table: "LeadAssignments",
                column: "SalesOfficerId",
                principalTable: "Users",
                principalColumn: "UserId",
                onDelete: ReferentialAction.Restrict);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_LeadAssignments_Leads_LeadId",
                table: "LeadAssignments");

            migrationBuilder.DropForeignKey(
                name: "FK_LeadAssignments_Users_AssignedBy",
                table: "LeadAssignments");

            migrationBuilder.DropForeignKey(
                name: "FK_LeadAssignments_Users_SalesOfficerId",
                table: "LeadAssignments");

            migrationBuilder.AddForeignKey(
                name: "FK_LeadAssignments_Leads_LeadId",
                table: "LeadAssignments",
                column: "LeadId",
                principalTable: "Leads",
                principalColumn: "LeadId",
                onDelete: ReferentialAction.Cascade);

            migrationBuilder.AddForeignKey(
                name: "FK_LeadAssignments_Users_AssignedBy",
                table: "LeadAssignments",
                column: "AssignedBy",
                principalTable: "Users",
                principalColumn: "UserId",
                onDelete: ReferentialAction.Cascade);

            migrationBuilder.AddForeignKey(
                name: "FK_LeadAssignments_Users_SalesOfficerId",
                table: "LeadAssignments",
                column: "SalesOfficerId",
                principalTable: "Users",
                principalColumn: "UserId",
                onDelete: ReferentialAction.Cascade);
        }
    }
}
