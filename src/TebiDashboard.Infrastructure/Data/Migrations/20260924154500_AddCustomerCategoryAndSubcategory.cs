using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Migrations;
using TebiDashboard.Infrastructure.Data;

#nullable disable

namespace TebiDashboard.Infrastructure.Data.Migrations
{
    /// <inheritdoc />
    [DbContext(typeof(TebiDashboardDbContext))]
    [Migration("20260924154500_AddCustomerCategoryAndSubcategory")]
    public partial class AddCustomerCategoryAndSubcategory : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            // Store NetSuite category/subcategory display names on the customer row.
            migrationBuilder.Sql(
                """
                ALTER TABLE "Customers"
                ADD COLUMN IF NOT EXISTS "Category" character varying(128) NOT NULL DEFAULT '';
                """);
            migrationBuilder.Sql(
                """
                ALTER TABLE "Customers"
                ADD COLUMN IF NOT EXISTS "Subcategory" character varying(128) NOT NULL DEFAULT '';
                """);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(name: "Category", table: "Customers");
            migrationBuilder.DropColumn(name: "Subcategory", table: "Customers");
        }
    }
}
