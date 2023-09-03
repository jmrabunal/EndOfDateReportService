namespace EndOfDateReportService.Models;

public class BranchModelOut
{
    public int Id { get; set; }
    public string Name { get; set; }
    
    public ICollection<LaneModelOut> Lanes { get; set; }
}