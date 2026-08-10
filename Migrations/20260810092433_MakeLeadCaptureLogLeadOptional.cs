using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace CRMSystem.Migrations
{
    /// <inheritdoc />
    public partial class MakeLeadCaptureLogLeadOptional : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_LeadCaptureLogs_Leads_LeadId",
                table: "LeadCaptureLogs");

            migrationBuilder.AlterColumn<long>(
                name: "LeadId",
                table: "LeadCaptureLogs",
                type: "bigint",
                nullable: true,
                oldClrType: typeof(long),
                oldType: "bigint");

            migrationBuilder.AddForeignKey(
                name: "FK_LeadCaptureLogs_Leads_LeadId",
                table: "LeadCaptureLogs",
                column: "LeadId",
                principalTable: "Leads",
                principalColumn: "LeadId");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_LeadCaptureLogs_Leads_LeadId",
                table: "LeadCaptureLogs");

            migrationBuilder.AlterColumn<long>(
                name: "LeadId",
                table: "LeadCaptureLogs",
                type: "bigint",
                nullable: false,
                defaultValue: 0L,
                oldClrType: typeof(long),
                oldType: "bigint",
                oldNullable: true);

            migrationBuilder.AddForeignKey(
                name: "FK_LeadCaptureLogs_Leads_LeadId",
                table: "LeadCaptureLogs",
                column: "LeadId",
                principalTable: "Leads",
                principalColumn: "LeadId",
                onDelete: ReferentialAction.Cascade);
        }
    }
}
