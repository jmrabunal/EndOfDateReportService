using EndOfDateReportService.Domain;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.ChangeTracking;

namespace EndOfDateReportService.DataAccess;

public class ReportContext: DbContext
{
    public DbSet<Branch> Branches { get; set; }
    public DbSet<Lane> Lanes { get; set; }
    public DbSet<PaymentMethod> PaymentMethods { get; set; }

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.Entity<Branch>().HasMany<Lane>().WithOne(x => x.Branch).HasForeignKey(x => x.BranchId)
            .IsRequired();
        modelBuilder.Entity<Branch>().HasMany<PaymentMethod>().WithOne(x => x.Branch).HasForeignKey(x => x.BranchId)
            .IsRequired();

        modelBuilder.Entity<Lane>().HasMany<PaymentMethod>().WithOne(x => x.Lane).HasForeignKey(x => x.LaneId)
            .IsRequired();
        modelBuilder.Entity<Lane>().HasIndex(x => x.Id).IsUnique();

        modelBuilder.Entity<PaymentMethod>().HasIndex(x => new { x.LocalId, x.LaneId, x.BranchId, x.ReportDate })
            .IsUnique();
        modelBuilder.Entity<PaymentMethod>().Property(x => x.Id).ValueGeneratedOnAdd();

    }
}