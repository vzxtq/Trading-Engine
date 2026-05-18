using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace TradingEngine.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class RefactorTradesToSymbolId : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            // 1. Add SymbolId as nullable
            migrationBuilder.AddColumn<Guid>(
                name: "SymbolId",
                table: "Trades",
                type: "uniqueidentifier",
                nullable: true);

            // 2. Data Migration: Join Trades and Symbols by Name and fill SymbolId
            migrationBuilder.Sql(
                @"UPDATE T 
                  SET T.SymbolId = S.Id 
                  FROM Trades T 
                  INNER JOIN Symbols S ON T.Symbol = S.Name");

            // 3. Delete records where symbol was not found (otherwise step 4 will fail)
            migrationBuilder.Sql("DELETE FROM Trades WHERE SymbolId IS NULL");

            // 4. Make SymbolId non-nullable
            migrationBuilder.AlterColumn<Guid>(
                name: "SymbolId",
                table: "Trades",
                type: "uniqueidentifier",
                nullable: false,
                oldClrType: typeof(Guid),
                oldType: "uniqueidentifier",
                oldNullable: true);

            // 5. Drop old string column
            migrationBuilder.DropColumn(
                name: "Symbol",
                table: "Trades");

            // 6. Create index and Foreign Key
            migrationBuilder.CreateIndex(
                name: "IX_Trades_SymbolId",
                table: "Trades",
                column: "SymbolId");

            migrationBuilder.AddForeignKey(
                name: "FK_Trades_Symbols_SymbolId",
                table: "Trades",
                column: "SymbolId",
                principalTable: "Symbols",
                principalColumn: "Id",
                onDelete: ReferentialAction.Cascade);
        }

        protected override void Down(MigrationBuilder migrationBuilder)
        {
            // 1. Restore Symbol column
            migrationBuilder.AddColumn<string>(
                name: "Symbol",
                table: "Trades",
                type: "nvarchar(10)",
                maxLength: 10,
                nullable: true);

            // 2. Restore string values from Symbols table
            migrationBuilder.Sql(
                @"UPDATE T 
                  SET T.Symbol = S.Name 
                  FROM Trades T 
                  INNER JOIN Symbols S ON T.SymbolId = S.Id");

            // 3. Drop FK and Index
            migrationBuilder.DropForeignKey(
                name: "FK_Trades_Symbols_SymbolId",
                table: "Trades");

            migrationBuilder.DropIndex(
                name: "IX_Trades_SymbolId",
                table: "Trades");

            // 4. Drop SymbolId column
            migrationBuilder.DropColumn(
                name: "SymbolId",
                table: "Trades");
        }
    }
}
