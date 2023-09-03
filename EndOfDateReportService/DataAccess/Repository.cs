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
        var branches = await context.Branches.ToListAsync();

        // Retrieve lanes for each branch
        foreach (var branch in branches)
        {
            branch.Lanes = await context.Lanes
                .Include(lane => lane.PaymentMethods)
                .Where(lane => lane.BranchId == branch.Id)
                .ToListAsync();

            // Clear the reference to the branch within each lane
            foreach (var lane in branch.Lanes)
            {
                lane.Branch = null;
                foreach (var paymentMethod in lane.PaymentMethods) 
                {
                    paymentMethod.Lane = null;
                }
            }
        }
        return branches;
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