using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Migrations;
using TebiDashboard.Infrastructure.Data;

#nullable disable

namespace TebiDashboard.Infrastructure.Data.Migrations
{
    /// <inheritdoc />
    [DbContext(typeof(TebiDashboardDbContext))]
    [Migration("20260928093000_AddNetSuiteSyncCheckpoint")]
    public partial class AddNetSuiteSyncCheckpoint : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            // Singleton row so last successful sync time survives API restarts.
            migrationBuilder.Sql(
                """
                CREATE TABLE IF NOT EXISTS "NetSuiteSyncCheckpoints" (
                    "Id" integer NOT NULL,
                    "LastSucceededAt" timestamp with time zone,
                    "LastCompletedAt" timestamp with time zone,
                    "LastLookbackDays" integer NOT NULL DEFAULT 0,
                    "LastState" character varying(32) NOT NULL DEFAULT '',
                    "LastMessage" character varying(1024) NOT NULL DEFAULT '',
                    CONSTRAINT "PK_NetSuiteSyncCheckpoints" PRIMARY KEY ("Id")
                );
                """);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(name: "NetSuiteSyncCheckpoints");
        }
    }
}
