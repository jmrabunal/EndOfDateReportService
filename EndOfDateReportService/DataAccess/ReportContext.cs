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
        modelBuilder.Entity<Branch>()
            .HasMany(b => b.Lanes)
            .WithOne(l => l.Branch)
            .HasForeignKey(l => l.BranchId);

        modelBuilder.Entity<Lane>()
            .HasMany(l => l.PaymentMethods)
            .WithOne(pm => pm.Lane)
            .HasForeignKey(pm => pm.LaneId);

        modelBuilder.Entity<PaymentMethod>()
            .HasOne(pm => pm.Branch)
            .WithMany()
            .HasForeignKey(pm => pm.BranchId);

        modelBuilder.Entity<PaymentMethod>().HasIndex(x => new { x.BranchId, x.ReportDate, x.LaneId }).IsUnique();
        modelBuilder.Entity<PaymentMethod>().Property(x => x.Id).ValueGeneratedOnAdd();

        base.OnModelCreating(modelBuilder);

    }
}