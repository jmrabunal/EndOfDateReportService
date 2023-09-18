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
            .Include(branch => branch.Notes.Where(n => n.CreatedDate == reportDate))
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

    public async Task<Note> CreateNote(Note entity)
    {
        var Note = await context.Notes.AddAsync(entity);
        await context.SaveChangesAsync();
        return Note.Entity;
    }

    public async Task<Lane> GetLaneByBranchId(int laneId, int branchId)
    {
        return await context.Lanes.FirstOrDefaultAsync(x => x.LaneId == laneId && x.BranchId == branchId);
    }

    public async Task<Note> GetNoteByBranchId( int branchId, DateTime date)
    {
        return await context.Notes.FirstOrDefaultAsync(x => x.BranchId == branchId && x.CreatedDate == date);
    }

    public async Task<bool> TryGetReport(DateTime reportDate)
    {
        return await context.PaymentMethods.AnyAsync(x => x.ReportDate == new DateTime(reportDate.Year, reportDate.Month,
            reportDate.Day, reportDate.Hour, reportDate.Minute, reportDate.Second, DateTimeKind.Utc));
    }

    public async Task<PaymentMethod> UpdatePaymentMethod(PaymentMethod paymentMethod)
    {
        try
        {
            var pm = context.PaymentMethods.Update(paymentMethod);
            await context.SaveChangesAsync();
            return pm.Entity;
        }catch (Exception ex)
        {
            var e =ex.Message;
            return paymentMethod;
        }
    }
   /* public async Task<Note> UpdateNote(PaymentMethod paymentMethod)
    {
        try
        {
            var pm = context.Notes.Update(paymentMethod);
            await context.SaveChangesAsync();
            return pm.Entity;
        }
        catch (Exception ex)
        {
            var e = ex.Message;
            return paymentMethod;
        }
    }*/
}