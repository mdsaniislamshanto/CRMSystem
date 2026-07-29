using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace CRMSystem.Migrations
{
    /// <inheritdoc />
    public partial class AddUniqueLeadCode : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateIndex(
                name: "IX_Leads_LeadCode",
                table: "Leads",
                column: "LeadCode",
                unique: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "IX_Leads_LeadCode",
                table: "Leads");
        }
    }
}
