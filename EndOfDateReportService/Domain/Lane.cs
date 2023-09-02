using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace EndOfDateReportService.Domain;

public class Lane
{
    [Key]
    public int Id { get; set; }
    [Column("branch_id")]
    public int BranchId { get; set; }
    [ForeignKey(nameof(Branch))]
    [InverseProperty(nameof(Domain.Branch.Lanes))]
    public Branch Branch { get; set; }
    [InverseProperty(nameof(PaymentMethod.Lane))]
    public ICollection<PaymentMethod> PaymentMethods { get; set; }


}