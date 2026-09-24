using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace TebiDashboard.Infrastructure.Data.Migrations
{
    /// <inheritdoc />
    public partial class AddProductGroupName : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            // A shared dev database can already have this column from a different migration id.
            // IF NOT EXISTS still creates it on a fresh database and lets startup continue here.
            migrationBuilder.Sql(
                """
                ALTER TABLE "RevenueRecords"
                ADD COLUMN IF NOT EXISTS "ProductGroupName" character varying(256) NOT NULL DEFAULT '';
                """);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "ProductGroupName",
                table: "RevenueRecords");
        }
    }
}
