using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace EndOfDateReportService.Domain;

public class Branch
{
    [Key] 
    public int Id { get; set; }
    [Column("name")]
    public string Name { get; set; }
    [InverseProperty(nameof(Lane.Branch))]
    public ICollection<Lane> Lanes { get; set; }
    [InverseProperty(nameof(PaymentMethod.Branch))]
    public ICollection<PaymentMethod> PaymentMethods { get; set; }
}