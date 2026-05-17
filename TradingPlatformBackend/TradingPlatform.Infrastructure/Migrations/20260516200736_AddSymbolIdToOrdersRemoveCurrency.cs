using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace TradingEngine.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class AddSymbolIdToOrdersRemoveCurrency : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "Currency",
                table: "Orders");

            migrationBuilder.AddColumn<Guid>(
                name: "SymbolId",
                table: "Orders",
                type: "uniqueidentifier",
                nullable: false,
                defaultValue: new Guid("00000000-0000-0000-0000-000000000000"));

            migrationBuilder.CreateIndex(
                name: "IX_Orders_SymbolId",
                table: "Orders",
                column: "SymbolId");

            migrationBuilder.AddForeignKey(
                name: "FK_Orders_Symbols_SymbolId",
                table: "Orders",
                column: "SymbolId",
                principalTable: "Symbols",
                principalColumn: "Id",
                onDelete: ReferentialAction.Cascade);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_Orders_Symbols_SymbolId",
                table: "Orders");

            migrationBuilder.DropIndex(
                name: "IX_Orders_SymbolId",
                table: "Orders");

            migrationBuilder.DropColumn(
                name: "SymbolId",
                table: "Orders");

            migrationBuilder.AddColumn<string>(
                name: "Currency",
                table: "Orders",
                type: "nvarchar(max)",
                nullable: false,
                defaultValue: "");
        }
    }
}
