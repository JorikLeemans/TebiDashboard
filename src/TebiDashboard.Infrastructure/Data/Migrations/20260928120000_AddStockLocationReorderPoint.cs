using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Migrations;
using TebiDashboard.Infrastructure.Data;

#nullable disable

namespace TebiDashboard.Infrastructure.Data.Migrations
{
    /// <inheritdoc />
    [DbContext(typeof(TebiDashboardDbContext))]
    [Migration("20260928120000_AddStockLocationReorderPoint")]
    public partial class AddStockLocationReorderPoint : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            // Per-location NetSuite itemlocationconfiguration.reorderpoint and preferredstocklevel.
            migrationBuilder.Sql(
                """
                ALTER TABLE "StockLocations"
                ADD COLUMN IF NOT EXISTS "ReorderPoint" numeric NOT NULL DEFAULT 0;
                """);
            migrationBuilder.Sql(
                """
                ALTER TABLE "StockLocations"
                ADD COLUMN IF NOT EXISTS "PreferredStockLevel" numeric NOT NULL DEFAULT 0;
                """);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(name: "ReorderPoint", table: "StockLocations");
            migrationBuilder.DropColumn(name: "PreferredStockLevel", table: "StockLocations");
        }
    }
}
