using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using System.Text.Json.Serialization;

namespace EndOfDateReportService.Domain;

public class Lane
{
    [Key]
    public int Id { get; set; }
    public int LaneId { get; set; }
    public int BranchId { get; set; }
    [JsonIgnore]
    public Branch Branch { get; set; }
    public ICollection<PaymentMethod> PaymentMethods { get; set; }
}