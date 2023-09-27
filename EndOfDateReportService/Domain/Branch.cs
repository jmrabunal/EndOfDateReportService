using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace EndOfDateReportService.Domain;

public class Branch
{
    [Key]
    public int Id { get; set; }
    public string Name { get; set; }
    public ICollection<Lane> Lanes { get; set; }
    public Note? Note { get; set; } 
    //public double? Gst { get; set; }
    //public double? EFTPOSFee { get; set; }
}