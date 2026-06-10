using Microsoft.EntityFrameworkCore;
using TebiDashboard.Infrastructure.Data.Entities;

namespace TebiDashboard.Infrastructure.Data;

/// <summary>
/// EF Core database context for users and dashboard demo data.
/// </summary>
public sealed class TebiDashboardDbContext(DbContextOptions<TebiDashboardDbContext> options) : DbContext(options)
{
    public DbSet<UserEntity> Users => Set<UserEntity>();

    public DbSet<RevenueRecord> RevenueRecords => Set<RevenueRecord>();

    public DbSet<GoalRecord> Goals => Set<GoalRecord>();

    public DbSet<InventoryItemRecord> InventoryItems => Set<InventoryItemRecord>();

    public DbSet<StockLocationRecord> StockLocations => Set<StockLocationRecord>();

    public DbSet<OpenTransactionRecord> OpenTransactions => Set<OpenTransactionRecord>();

    public DbSet<ExpenseRecord> Expenses => Set<ExpenseRecord>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.Entity<UserEntity>(entity =>
        {
            entity.HasKey(e => e.Email);
            entity.Property(e => e.Email).HasMaxLength(256);
            entity.Property(e => e.Password).HasMaxLength(256);
            entity.Property(e => e.Role).HasMaxLength(64);
        });

        modelBuilder.Entity<RevenueRecord>(entity =>
        {
            entity.HasKey(e => e.Id);
            entity.Property(e => e.Id).HasMaxLength(64);
            entity.Property(e => e.CustomerId).HasMaxLength(64);
            entity.Property(e => e.CustomerName).HasMaxLength(256);
            entity.Property(e => e.ProductGroupId).HasMaxLength(64);
        });

        modelBuilder.Entity<GoalRecord>(entity =>
        {
            entity.HasKey(e => e.Id);
            entity.Property(e => e.Id).HasMaxLength(64);
            entity.Property(e => e.Period).HasMaxLength(16);
            entity.Property(e => e.Type).HasMaxLength(64);
            entity.Property(e => e.ItemId).HasMaxLength(64);
            entity.Property(e => e.ItemName).HasMaxLength(256);
            entity.Property(e => e.Status).HasMaxLength(32);
        });

        modelBuilder.Entity<InventoryItemRecord>(entity =>
        {
            entity.HasKey(e => e.ItemId);
            entity.Property(e => e.ItemId).HasMaxLength(64);
            entity.Property(e => e.ItemName).HasMaxLength(256);
            entity.Property(e => e.Barcode).HasMaxLength(64);
        });

        modelBuilder.Entity<StockLocationRecord>(entity =>
        {
            entity.HasKey(e => e.Id);
            entity.Property(e => e.ItemId).HasMaxLength(64);
            entity.Property(e => e.LocationId).HasMaxLength(64);
            entity.Property(e => e.LocationName).HasMaxLength(256);
            entity.HasIndex(e => e.ItemId);
        });

        modelBuilder.Entity<OpenTransactionRecord>(entity =>
        {
            entity.HasKey(e => e.TransactionId);
            entity.Property(e => e.TransactionId).HasMaxLength(64);
            entity.Property(e => e.Type).HasMaxLength(64);
            entity.Property(e => e.ItemId).HasMaxLength(64);
            entity.Property(e => e.Status).HasMaxLength(64);
            entity.HasIndex(e => e.ItemId);
        });

        modelBuilder.Entity<ExpenseRecord>(entity =>
        {
            entity.HasKey(e => e.Id);
            entity.Property(e => e.Id).HasMaxLength(64);
            entity.Property(e => e.Category).HasMaxLength(128);
            entity.Property(e => e.VendorName).HasMaxLength(256);
        });
    }
}
