using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace CRMSystem.Migrations
{
    /// <inheritdoc />
    public partial class AddSalesManagerRelationship : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<long>(
                name: "SalesManagerId",
                table: "Users",
                type: "bigint",
                nullable: true);

            migrationBuilder.CreateIndex(
                name: "IX_Users_SalesManagerId",
                table: "Users",
                column: "SalesManagerId");

            migrationBuilder.AddForeignKey(
                name: "FK_Users_Users_SalesManagerId",
                table: "Users",
                column: "SalesManagerId",
                principalTable: "Users",
                principalColumn: "UserId");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_Users_Users_SalesManagerId",
                table: "Users");

            migrationBuilder.DropIndex(
                name: "IX_Users_SalesManagerId",
                table: "Users");

            migrationBuilder.DropColumn(
                name: "SalesManagerId",
                table: "Users");
        }
    }
}
