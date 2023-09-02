using EndOfDateReportService.Domain;
using Microsoft.EntityFrameworkCore;

namespace EndOfDateReportService.DataAccess;

public class Repository
{
    private readonly ReportContext context;
    public Repository(ReportContext reportContent)
    {
        context = reportContent;
    }
    
    public async Task<IEnumerable<PaymentMethod>> GetPaymentMethodReport(DateTime reportDate, int laneId, int branchId)
    {
        return await context.PaymentMethods.Include(x => x.Lane).ThenInclude(x => x.Branch).Where(x =>
            x.LaneId == laneId && x.Lane.BranchId == branchId && x.ReportDate == reportDate).ToListAsync();
    }

    public async void CreatePaymentMethodReport(PaymentMethod paymentMethod)
    {
        await context.PaymentMethods.AddAsync(paymentMethod);
    }
}