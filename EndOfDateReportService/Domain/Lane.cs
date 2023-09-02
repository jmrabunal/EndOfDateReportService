using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace EndOfDateReportService.Domain;

public class Lane
{
    [Key]
    public int Id { get; set; }
    
    public int BranchId { get; set; }
    public Branch Branch { get; set; }
    
    public ICollection<PaymentMethod> PaymentMethods { get; set; }
}