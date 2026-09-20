using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace CRMSystem.Migrations
{
    /// <inheritdoc />
    public partial class AddTeamLeadRelationship : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_Users_Users_SalesManagerId",
                table: "Users");

            migrationBuilder.AddColumn<long>(
                name: "TeamLeadId",
                table: "Users",
                type: "bigint",
                nullable: true);

            migrationBuilder.AlterColumn<long>(
                name: "CreatedBy",
                table: "SalesTargets",
                type: "bigint",
                nullable: true,
                oldClrType: typeof(long),
                oldType: "bigint");

            migrationBuilder.CreateIndex(
                name: "IX_Users_TeamLeadId",
                table: "Users",
                column: "TeamLeadId");

            migrationBuilder.AddForeignKey(
                name: "FK_Users_Users_SalesManagerId",
                table: "Users",
                column: "SalesManagerId",
                principalTable: "Users",
                principalColumn: "UserId",
                onDelete: ReferentialAction.Restrict);

            migrationBuilder.AddForeignKey(
                name: "FK_Users_Users_TeamLeadId",
                table: "Users",
                column: "TeamLeadId",
                principalTable: "Users",
                principalColumn: "UserId",
                onDelete: ReferentialAction.Restrict);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_Users_Users_SalesManagerId",
                table: "Users");

            migrationBuilder.DropForeignKey(
                name: "FK_Users_Users_TeamLeadId",
                table: "Users");

            migrationBuilder.DropIndex(
                name: "IX_Users_TeamLeadId",
                table: "Users");

            migrationBuilder.DropColumn(
                name: "TeamLeadId",
                table: "Users");

            migrationBuilder.AlterColumn<long>(
                name: "CreatedBy",
                table: "SalesTargets",
                type: "bigint",
                nullable: false,
                defaultValue: 0L,
                oldClrType: typeof(long),
                oldType: "bigint",
                oldNullable: true);

            migrationBuilder.AddForeignKey(
                name: "FK_Users_Users_SalesManagerId",
                table: "Users",
                column: "SalesManagerId",
                principalTable: "Users",
                principalColumn: "UserId");
        }
    }
}
