using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Migrations;
using TebiDashboard.Infrastructure.Data;

#nullable disable

namespace TebiDashboard.Infrastructure.Data.Migrations
{
    /// <inheritdoc />
    [DbContext(typeof(TebiDashboardDbContext))]
    [Migration("20260930160000_AddRevenueUnitsCaptured")]
    public partial class AddRevenueUnitsCaptured : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            // Existing invoice lines have amounts but no units. False makes the next sync re-read them.
            migrationBuilder.Sql(
                """
                ALTER TABLE "RevenueRecords"
                ADD COLUMN IF NOT EXISTS "UnitsCaptured" boolean NOT NULL DEFAULT false;
                """);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(name: "UnitsCaptured", table: "RevenueRecords");
        }
    }
}
