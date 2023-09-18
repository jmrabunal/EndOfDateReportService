using Microsoft.AspNetCore.Mvc.ModelBinding;
using System.ComponentModel.DataAnnotations;
using System.Text.Json.Serialization;

namespace EndOfDateReportService.Domain
{
    public class Note
    {
        [Key]
        public int Id { get; set; }
        public string? SummaryNote { get; set; }
        public int BranchId { get; set; }
        public DateTime? CreatedDate { get; set; }
        [JsonIgnore]
        [BindNever]
        public Branch? Branch { get; set; }
    }
}
