using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace EndOfDateReportService.Domain;

public class PaymentMethod
{
    [Key]
    public int Id { get; set; }
    [Column("local_Id")]
    public int LocalId { get; set; }
    [Column("name")]
    public string Name { get; set; }
    [Column("lane_id")]
    public int LaneId { get; set; }
    [ForeignKey(nameof(LaneId))]
    [InverseProperty(nameof(Domain.Lane.PaymentMethods))]
    public Lane Lane { get; set; }
    [Column("actual_amount")]
    public float ActualAmount { get; set; }
    [Column("reported_amount")]
    public float ReportedAmount { get; set; }
    [Column("total_variance")]
    public float TotalVariance { get; set; } 
    [Column("report_date")]
    public DateTime ReportDate { get; set; }
    public int BranchId { get; set; }
    [ForeignKey(nameof(BranchId))]
    [InverseProperty(nameof(Domain.Branch.PaymentMethods))]
    public Branch Branch { get; set; }
    
}