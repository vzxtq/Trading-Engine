using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace TradingEngine.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class AddDurableOutboxPipeline : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<byte[]>(
                name: "RowVersion",
                table: "Positions",
                type: "rowversion",
                rowVersion: true,
                nullable: false,
                defaultValue: new byte[0]);

            migrationBuilder.AddColumn<byte[]>(
                name: "RowVersion",
                table: "Orders",
                type: "rowversion",
                rowVersion: true,
                nullable: false,
                defaultValue: new byte[0]);

            migrationBuilder.AddColumn<byte[]>(
                name: "RowVersion",
                table: "Accounts",
                type: "rowversion",
                rowVersion: true,
                nullable: false,
                defaultValue: new byte[0]);

            migrationBuilder.CreateTable(
                name: "OrderCommandOutbox",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    EnqueueId = table.Column<long>(type: "bigint", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    SequenceId = table.Column<long>(type: "bigint", nullable: true),
                    SymbolId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    OrderId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    ActiveCancellationOrderId = table.Column<Guid>(type: "uniqueidentifier", nullable: true),
                    CommandType = table.Column<string>(type: "nvarchar(450)", nullable: false),
                    Payload = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    Status = table.Column<string>(type: "nvarchar(450)", nullable: false),
                    AttemptCount = table.Column<int>(type: "int", nullable: false),
                    LastError = table.Column<string>(type: "nvarchar(2000)", maxLength: 2000, nullable: true),
                    CreatedAt = table.Column<DateTime>(type: "datetime2", nullable: false),
                    DispatchedAt = table.Column<DateTime>(type: "datetime2", nullable: true),
                    CompletedAt = table.Column<DateTime>(type: "datetime2", nullable: true),
                    RowVersion = table.Column<byte[]>(type: "rowversion", rowVersion: true, nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_OrderCommandOutbox", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "ProcessedExecutionReceipts",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    SymbolId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    SequenceId = table.Column<long>(type: "bigint", nullable: false),
                    ProcessedAt = table.Column<DateTime>(type: "datetime2", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_ProcessedExecutionReceipts", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "SymbolCommandSequences",
                columns: table => new
                {
                    SymbolId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    LastSequenceId = table.Column<long>(type: "bigint", nullable: false),
                    RowVersion = table.Column<byte[]>(type: "rowversion", rowVersion: true, nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_SymbolCommandSequences", x => x.SymbolId);
                });

            migrationBuilder.CreateTable(
                name: "ExecutionResultOutbox",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    CommandOutboxId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    SymbolId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    SequenceId = table.Column<long>(type: "bigint", nullable: false),
                    ResultType = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: false),
                    Payload = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    Status = table.Column<string>(type: "nvarchar(450)", nullable: false),
                    AttemptCount = table.Column<int>(type: "int", nullable: false),
                    LastError = table.Column<string>(type: "nvarchar(2000)", maxLength: 2000, nullable: true),
                    CreatedAt = table.Column<DateTime>(type: "datetime2", nullable: false),
                    ProcessedAt = table.Column<DateTime>(type: "datetime2", nullable: true),
                    RowVersion = table.Column<byte[]>(type: "rowversion", rowVersion: true, nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_ExecutionResultOutbox", x => x.Id);
                    table.ForeignKey(
                        name: "FK_ExecutionResultOutbox_OrderCommandOutbox_CommandOutboxId",
                        column: x => x.CommandOutboxId,
                        principalTable: "OrderCommandOutbox",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateIndex(
                name: "IX_ExecutionResultOutbox_CommandOutboxId",
                table: "ExecutionResultOutbox",
                column: "CommandOutboxId",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_ExecutionResultOutbox_Status_CreatedAt",
                table: "ExecutionResultOutbox",
                columns: new[] { "Status", "CreatedAt" });

            migrationBuilder.CreateIndex(
                name: "IX_ExecutionResultOutbox_SymbolId_SequenceId",
                table: "ExecutionResultOutbox",
                columns: new[] { "SymbolId", "SequenceId" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_OrderCommandOutbox_ActiveCancellationOrderId",
                table: "OrderCommandOutbox",
                column: "ActiveCancellationOrderId",
                unique: true,
                filter: "[ActiveCancellationOrderId] IS NOT NULL");

            migrationBuilder.CreateIndex(
                name: "IX_OrderCommandOutbox_OrderId_CommandType_Status",
                table: "OrderCommandOutbox",
                columns: new[] { "OrderId", "CommandType", "Status" });

            migrationBuilder.CreateIndex(
                name: "IX_OrderCommandOutbox_EnqueueId",
                table: "OrderCommandOutbox",
                column: "EnqueueId",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_OrderCommandOutbox_Status_EnqueueId",
                table: "OrderCommandOutbox",
                columns: new[] { "Status", "EnqueueId" });

            migrationBuilder.CreateIndex(
                name: "IX_OrderCommandOutbox_SymbolId_SequenceId",
                table: "OrderCommandOutbox",
                columns: new[] { "SymbolId", "SequenceId" },
                unique: true,
                filter: "[SequenceId] IS NOT NULL");

            migrationBuilder.CreateIndex(
                name: "IX_ProcessedExecutionReceipts_SymbolId_SequenceId",
                table: "ProcessedExecutionReceipts",
                columns: new[] { "SymbolId", "SequenceId" },
                unique: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "ExecutionResultOutbox");

            migrationBuilder.DropTable(
                name: "ProcessedExecutionReceipts");

            migrationBuilder.DropTable(
                name: "SymbolCommandSequences");

            migrationBuilder.DropTable(
                name: "OrderCommandOutbox");

            migrationBuilder.DropColumn(
                name: "RowVersion",
                table: "Positions");

            migrationBuilder.DropColumn(
                name: "RowVersion",
                table: "Orders");

            migrationBuilder.DropColumn(
                name: "RowVersion",
                table: "Accounts");
        }
    }
}
