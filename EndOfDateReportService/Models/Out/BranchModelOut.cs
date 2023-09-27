using EndOfDateReportService.Domain;

namespace EndOfDateReportService.Models.Out;

public class BranchModelOut
{
    public int Id { get; set; }
    public string Name { get; set; }
    public ICollection<LaneModelOut> Lanes { get; set; }
    public Note Note { get; set; }
    public double Gst { get; set; }
    public double EFTPOSFee { get; set; }
}