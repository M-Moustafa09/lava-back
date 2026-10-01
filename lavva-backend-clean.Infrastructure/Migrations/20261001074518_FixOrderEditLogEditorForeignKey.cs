using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace lavva_backend_clean.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class FixOrderEditLogEditorForeignKey : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_OrderEditLogs_Users_EditorId",
                table: "OrderEditLogs");

            migrationBuilder.DropIndex(
                name: "IX_OrderEditLogs_EditorId",
                table: "OrderEditLogs");

            migrationBuilder.DropColumn(
                name: "EditorId",
                table: "OrderEditLogs");

            migrationBuilder.CreateIndex(
                name: "IX_OrderEditLogs_EditedBy",
                table: "OrderEditLogs",
                column: "EditedBy");

            migrationBuilder.AddForeignKey(
                name: "FK_OrderEditLogs_Users_EditedBy",
                table: "OrderEditLogs",
                column: "EditedBy",
                principalTable: "Users",
                principalColumn: "Id",
                onDelete: ReferentialAction.Restrict);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_OrderEditLogs_Users_EditedBy",
                table: "OrderEditLogs");

            migrationBuilder.DropIndex(
                name: "IX_OrderEditLogs_EditedBy",
                table: "OrderEditLogs");

            migrationBuilder.AddColumn<Guid>(
                name: "EditorId",
                table: "OrderEditLogs",
                type: "uniqueidentifier",
                nullable: false,
                defaultValue: new Guid("00000000-0000-0000-0000-000000000000"));

            migrationBuilder.CreateIndex(
                name: "IX_OrderEditLogs_EditorId",
                table: "OrderEditLogs",
                column: "EditorId");

            migrationBuilder.AddForeignKey(
                name: "FK_OrderEditLogs_Users_EditorId",
                table: "OrderEditLogs",
                column: "EditorId",
                principalTable: "Users",
                principalColumn: "Id",
                onDelete: ReferentialAction.Restrict);
        }
    }
}
