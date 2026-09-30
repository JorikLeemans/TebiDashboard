using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Migrations;
using TebiDashboard.Infrastructure.Data;

#nullable disable

namespace TebiDashboard.Infrastructure.Data.Migrations
{
    /// <inheritdoc />
    [DbContext(typeof(TebiDashboardDbContext))]
    [Migration("20260930140000_AddRevenueQuantity")]
    public partial class AddRevenueQuantity : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            // Units sold on each invoice line. Existing rows stay 0 until the next NetSuite sync.
            migrationBuilder.Sql(
                """
                ALTER TABLE "RevenueRecords"
                ADD COLUMN IF NOT EXISTS "Quantity" numeric NOT NULL DEFAULT 0;
                """);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(name: "Quantity", table: "RevenueRecords");
        }
    }
}
