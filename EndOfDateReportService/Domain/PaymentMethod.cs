using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace EndOfDateReportService.Domain;

public class PaymentMethod
{
    public int Id { get; set; }
    [Key]
    public string Name { get; set; }
    public decimal ActualAmount { get; set; }
    public decimal ReportedAmount { get; set; }
    public decimal TotalVariance { get; set; }
    public DateTime ReportDate { get; set; }
    
    public int LaneId { get; set; }
    public Lane Lane { get; set; }
    
    public int BranchId { get; set; }
    public Branch Branch { get; set; }
}