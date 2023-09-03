namespace EndOfDateReportService.Models;

public class LaneModelOut
{
    public int LaneId { get; set; }
    public int BranchId { get; set; }
    public ICollection<PaymentMethodModelOut> PaymentMethods { get; set; }
}