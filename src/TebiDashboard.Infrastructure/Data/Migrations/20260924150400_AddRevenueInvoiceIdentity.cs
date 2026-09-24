using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Migrations;
using TebiDashboard.Infrastructure.Data;

#nullable disable

namespace TebiDashboard.Infrastructure.Data.Migrations
{
    /// <inheritdoc />
    [DbContext(typeof(TebiDashboardDbContext))]
    [Migration("20260924150400_AddRevenueInvoiceIdentity")]
    public partial class AddRevenueInvoiceIdentity : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            // Parent invoice id/number so line rows can show which invoice they belong to.
            migrationBuilder.Sql(
                """
                ALTER TABLE "RevenueRecords"
                ADD COLUMN IF NOT EXISTS "InvoiceId" character varying(64) NOT NULL DEFAULT '';
                """);
            migrationBuilder.Sql(
                """
                ALTER TABLE "RevenueRecords"
                ADD COLUMN IF NOT EXISTS "InvoiceNumber" character varying(64) NOT NULL DEFAULT '';
                """);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(name: "InvoiceId", table: "RevenueRecords");
            migrationBuilder.DropColumn(name: "InvoiceNumber", table: "RevenueRecords");
        }
    }
}
