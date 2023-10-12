namespace EndOfDateReportService.Models.Out;

public class LaneModelOut
{
    public int LaneId { get; set; }
    public int BranchId { get; set; }
    public double? CallAdjustment { get; set; }
    public ICollection<PaymentMethodModelOut> PaymentMethods { get; set; }
}