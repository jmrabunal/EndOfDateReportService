namespace EndOfDateReportService.Models;

public class PaymentMethodModelOut
{
    public int Id { get; set; }
    
    public string Name { get; set; }
    public decimal ActualAmount { get; set; }
    public decimal ReportedAmount { get; set; }
    public decimal TotalVariance { get; set; }
    public DateTime ReportDate { get; set; }
    
    public int LaneId { get; set; }
    public int BranchId { get; set; }

}