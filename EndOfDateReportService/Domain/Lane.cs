using Microsoft.AspNetCore.Mvc.ModelBinding;
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
    public double? CallAdjustment { get; set; }
    [JsonIgnore]
    [BindNever]
    public Branch? Branch { get; set; }
    public ICollection<PaymentMethod> PaymentMethods { get; set; }
}