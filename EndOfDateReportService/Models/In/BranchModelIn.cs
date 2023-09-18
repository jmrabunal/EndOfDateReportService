using EndOfDateReportService.Domain;
using EndOfDateReportService.Models.Out;

namespace EndOfDateReportService.Models.In
{
    public class BranchModelIn
    {
        public int Id { get; set; }
        public string Name { get; set; }
        public ICollection<LaneModelOut> Lanes { get; set; }
        public Note? Note { get; set; }
        public double Gst { get; set; }
        public double EFTPOSFee { get; set; }
    }
}
