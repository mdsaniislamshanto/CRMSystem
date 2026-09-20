using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace CRMSystem.Migrations
{
    /// <inheritdoc />
    public partial class AddSalesManagerOwnershipToLead : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<long>(
                name: "SalesManagerId",
                table: "Leads",
                type: "bigint",
                nullable: true);

            migrationBuilder.CreateIndex(
                name: "IX_Leads_SalesManagerId",
                table: "Leads",
                column: "SalesManagerId");

            migrationBuilder.AddForeignKey(
                name: "FK_Leads_Users_SalesManagerId",
                table: "Leads",
                column: "SalesManagerId",
                principalTable: "Users",
                principalColumn: "UserId");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_Leads_Users_SalesManagerId",
                table: "Leads");

            migrationBuilder.DropIndex(
                name: "IX_Leads_SalesManagerId",
                table: "Leads");

            migrationBuilder.DropColumn(
                name: "SalesManagerId",
                table: "Leads");
        }
    }
}
