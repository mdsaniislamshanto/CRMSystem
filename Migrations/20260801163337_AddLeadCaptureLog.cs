using System;
using Microsoft.EntityFrameworkCore.Metadata;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace CRMSystem.Migrations
{
    /// <inheritdoc />
    public partial class AddLeadCaptureLog : Migration
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

            migrationBuilder.CreateTable(
                name: "LeadCaptureLogs",
                columns: table => new
                {
                    CaptureLogId = table.Column<long>(type: "bigint", nullable: false)
                        .Annotation("MySql:ValueGenerationStrategy", MySqlValueGenerationStrategy.IdentityColumn),
                    LeadId = table.Column<long>(type: "bigint", nullable: false),
                    CaptureSource = table.Column<int>(type: "int", nullable: false),
                    CaptureStatus = table.Column<int>(type: "int", nullable: false),
                    ExternalLeadId = table.Column<string>(type: "varchar(200)", maxLength: 200, nullable: true)
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    ErrorMessage = table.Column<string>(type: "varchar(500)", maxLength: 500, nullable: true)
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    PayloadJson = table.Column<string>(type: "longtext", nullable: true)
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    ReceivedAt = table.Column<DateTime>(type: "datetime(6)", nullable: false),
                    ProcessedAt = table.Column<DateTime>(type: "datetime(6)", nullable: true),
                    IsActive = table.Column<bool>(type: "tinyint(1)", nullable: false),
                    IsDeleted = table.Column<bool>(type: "tinyint(1)", nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "datetime(6)", nullable: false),
                    CreatedBy = table.Column<long>(type: "bigint", nullable: true),
                    UpdatedAt = table.Column<DateTime>(type: "datetime(6)", nullable: true),
                    UpdatedBy = table.Column<long>(type: "bigint", nullable: true),
                    DeletedAt = table.Column<DateTime>(type: "datetime(6)", nullable: true),
                    DeletedBy = table.Column<long>(type: "bigint", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_LeadCaptureLogs", x => x.CaptureLogId);
                    table.ForeignKey(
                        name: "FK_LeadCaptureLogs_Leads_LeadId",
                        column: x => x.LeadId,
                        principalTable: "Leads",
                        principalColumn: "LeadId",
                        onDelete: ReferentialAction.Cascade);
                })
                .Annotation("MySql:CharSet", "utf8mb4");

            migrationBuilder.CreateIndex(
                name: "IX_LeadCaptureLogs_LeadId",
                table: "LeadCaptureLogs",
                column: "LeadId");

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

            migrationBuilder.DropTable(
                name: "LeadCaptureLogs");

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
    }
}
