using EndOfDateReportService.Domain;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.ChangeTracking;

namespace EndOfDateReportService.DataAccess;

public class ReportContext: DbContext
{
    public ReportContext(DbContextOptions<ReportContext> options) : base(options)
    {
        
    }
    public DbSet<Branch> Branches { get; set; }
    public DbSet<Lane> Lanes { get; set; }
    public DbSet<PaymentMethod> PaymentMethods { get; set; }

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.Entity<Branch>()
            .HasMany(b => b.Lanes);


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

        modelBuilder.Entity<Branch>().HasData(
            new Branch()
            {
                Id = 1,
                Name = "Moore Wilsons Wellington"
            },
            new Branch()
            {
                Id = 2,
                Name = "Moore Wilsons Porirua"
            }, new Branch()
            {
                Id = 3,
                Name = "Moore Wilsons Lower Hutt"
            }, new Branch()
            {
                Id = 4,
                Name = "Moore Wilsons Masterton"
            }
        );
        
        
        base.OnModelCreating(modelBuilder);

    }
}