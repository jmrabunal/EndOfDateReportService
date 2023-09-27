using Microsoft.AspNetCore.Mvc.ModelBinding;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using System.Text.Json.Serialization;

namespace EndOfDateReportService.Domain;

public class PaymentMethod
{
    [Key]
    public long Id { get; set; }
    
    public string Name { get; set; }
    public decimal ActualAmount { get; set; }
    public decimal ReportedAmount { get; set; }
    public decimal TotalVariance { get; set; }
    public DateTime ReportDate { get; set; }
    public int LaneId { get; set; }
    [JsonIgnore]
    [BindNever]
    public Lane? Lane { get; set; }
    public int BranchId { get; set; }
}