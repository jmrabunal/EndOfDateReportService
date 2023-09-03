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

    public async Task CreatePaymentMethodReport(PaymentMethod paymentMethod)
    {
        await context.PaymentMethods.AddAsync(paymentMethod);
        await context.SaveChangesAsync();

    }

    public async Task<Lane> CreateLane(Lane entity)
    {
        var lane = await  context.Lanes.AddAsync(entity);
        await context.SaveChangesAsync();
        return lane.Entity;
    }
    
    public async Task<Lane> GetLaneByBranchId(int laneId, int branchId)
    {
        return await context.Lanes.FirstOrDefaultAsync(x => x.LaneId == laneId && x.BranchId == branchId);
    }

    public async Task<bool> TryGetReport(DateTime reportDate)
    {
        return await context.PaymentMethods.AnyAsync(x => x.ReportDate == new DateTime(reportDate.Year, reportDate.Month,
            reportDate.Day, reportDate.Hour, reportDate.Minute, reportDate.Second, DateTimeKind.Utc));
    }
}