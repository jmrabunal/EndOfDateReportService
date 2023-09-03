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

    public async Task<bool> GetLaneByBranchId(int laneId, int branchId)
    {
        return await context.Lanes.AnyAsync(x => x.Id == laneId && x.BranchId == branchId);
    }
}