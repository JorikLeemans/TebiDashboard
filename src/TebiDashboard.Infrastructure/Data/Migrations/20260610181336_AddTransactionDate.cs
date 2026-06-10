using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace TebiDashboard.Infrastructure.Data.Migrations
{
    /// <inheritdoc />
    public partial class AddTransactionDate : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<DateOnly>(
                name: "TransactionDate",
                table: "OpenTransactions",
                type: "date",
                nullable: false,
                defaultValue: new DateOnly(1, 1, 1));

            migrationBuilder.CreateIndex(
                name: "IX_OpenTransactions_TransactionDate",
                table: "OpenTransactions",
                column: "TransactionDate");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "IX_OpenTransactions_TransactionDate",
                table: "OpenTransactions");

            migrationBuilder.DropColumn(
                name: "TransactionDate",
                table: "OpenTransactions");
        }
    }
}
