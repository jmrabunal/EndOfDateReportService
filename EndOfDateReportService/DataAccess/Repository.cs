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
        return await context.PaymentMethods.Include(x => x.Lane).Include(x=>x.Branch).Where(x =>
            x.LaneId == laneId && x.ReportDate == reportDate && x.BranchId == branchId).ToListAsync();
    }
    
    public async Task<IEnumerable<PaymentMethod>> GetAllPaymentMethodReportByBranchId(DateTime reportDate, int laneId, int branchId)
    {
        return await context.PaymentMethods.Include(x => x.Lane).Include(x=>x.Branch).Where(x => x.ReportDate == reportDate && x.BranchId == branchId).ToListAsync();
    }

    public async Task<IEnumerable<Branch>> Get(DateTime reportDate)
    {
        return await context.Branches
            .Include(branch => branch.Lanes)
            .ThenInclude(lane => lane.PaymentMethods.Where(pm => pm.ReportDate == reportDate))
            .ToListAsync(); 
    }

    public async void CreatePaymentMethodReport(PaymentMethod paymentMethod)
    {
        await context.PaymentMethods.AddAsync(paymentMethod);
    }

    public async void CreateLane(Lane lane)
    {
       await  context.Lanes.AddAsync(lane);
    }
}