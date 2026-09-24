using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Migrations;
using TebiDashboard.Infrastructure.Data;

#nullable disable

namespace TebiDashboard.Infrastructure.Data.Migrations
{
    /// <inheritdoc />
    [DbContext(typeof(TebiDashboardDbContext))]
    [Migration("20260924123000_AddRevenueAndTransactionItemNames")]
    public partial class AddRevenueAndTransactionItemNames : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            // Persist invoice-line item identity and names so the UI does not show raw ids.
            migrationBuilder.Sql(
                """
                ALTER TABLE "RevenueRecords"
                ADD COLUMN IF NOT EXISTS "ItemId" character varying(64) NOT NULL DEFAULT '';
                """);
            migrationBuilder.Sql(
                """
                ALTER TABLE "RevenueRecords"
                ADD COLUMN IF NOT EXISTS "ItemName" character varying(256) NOT NULL DEFAULT '';
                """);
            migrationBuilder.Sql(
                """
                ALTER TABLE "RevenueRecords"
                ALTER COLUMN "Id" TYPE character varying(128);
                """);
            migrationBuilder.Sql(
                """
                ALTER TABLE "OpenTransactions"
                ADD COLUMN IF NOT EXISTS "ItemName" character varying(256) NOT NULL DEFAULT '';
                """);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(name: "ItemId", table: "RevenueRecords");
            migrationBuilder.DropColumn(name: "ItemName", table: "RevenueRecords");
            migrationBuilder.DropColumn(name: "ItemName", table: "OpenTransactions");
        }
    }
}
