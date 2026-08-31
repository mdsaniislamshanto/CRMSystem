using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace CRMSystem.Migrations
{
    /// <inheritdoc />
    public partial class AddLeadArchiveMetadata : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<DateTime>(
                name: "ArchivedAt",
                table: "Leads",
                type: "datetime(6)",
                nullable: true);

            migrationBuilder.AddColumn<long>(
                name: "ArchivedBy",
                table: "Leads",
                type: "bigint",
                nullable: true);

            migrationBuilder.CreateIndex(
                name: "IX_Leads_ArchivedBy",
                table: "Leads",
                column: "ArchivedBy");

            migrationBuilder.AddForeignKey(
                name: "FK_Leads_Users_ArchivedBy",
                table: "Leads",
                column: "ArchivedBy",
                principalTable: "Users",
                principalColumn: "UserId");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_Leads_Users_ArchivedBy",
                table: "Leads");

            migrationBuilder.DropIndex(
                name: "IX_Leads_ArchivedBy",
                table: "Leads");

            migrationBuilder.DropColumn(
                name: "ArchivedAt",
                table: "Leads");

            migrationBuilder.DropColumn(
                name: "ArchivedBy",
                table: "Leads");
        }
    }
}
