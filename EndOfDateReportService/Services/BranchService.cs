using EndOfDateReportService.ServicesInterfaces;
using Microsoft.Data.SqlClient;
using System;
using EndOfDateReportService.DataAccess;
using EndOfDateReportService.Domain;
using Microsoft.EntityFrameworkCore;
using Microsoft.AspNetCore.Hosting.Server;
using Microsoft.Extensions.Configuration;

namespace EndOfDateReportService.Services
{
    public class BranchService : IBranchInterface
    {
        private string connectionString;
        private ReportContext _reportContext;
        private Repository _repository;
        private readonly IConfiguration _configuration;
        private PdfService _pdfService;
        public BranchService(IConfiguration configuration, ReportContext reportContext, Repository repository, PdfService pdfService)
        {
            _configuration = configuration;
            connectionString = configuration.GetConnectionString("DefaultConnection");
            _reportContext = reportContext;
            _repository = repository;
            _pdfService = pdfService;
        }

        private async Task<Dictionary<string, decimal>> ExecuteQuery(DateTime startDate, DateTime endDate, int branchId, int stationId) 
        {
            using (SqlConnection connection = new SqlConnection(connectionString))
            {
                await connection.OpenAsync();

                string sqlQuery = "WITH AllPaymentMethods AS (SELECT 'Cash' AS PaymentMethod UNION ALL SELECT 'EFTPOS' UNION ALL SELECT 'Account' UNION ALL SELECT 'Credit Card' UNION ALL SELECT 'Extra Cash' UNION ALL SELECT 'Bank Payment' UNION ALL SELECT 'Credit Note' UNION ALL SELECT 'Voucher') SELECT APM.PaymentMethod, COALESCE(SUM(TP.Value), 0) AS ActualAmount FROM AllPaymentMethods APM LEFT JOIN (SELECT TH.TransNo, TP.MediaID, SUM(TP.Value) AS Value FROM AKPOS.dbo.TransHeaders as TH LEFT JOIN AKPOS.dbo.TransPayments TP ON TH.TransNo = TP.TransNo AND TH.Station = TP.Station and TH.Branch = TP.Branch WHERE TH.Logged >=@StartDate AND TH.Logged <= @EndDate AND TH.Branch = @BranchID AND TH.Station = @StationID GROUP BY TH.TransNo, TP.MediaID union SELECT TH.TransNo, 1 as MediaID, -SUM(TP.Change) AS Value FROM AKPOS.dbo.TransHeaders TH LEFT JOIN AKPOS.dbo.TransPayments TP ON TH.TransNo = TP.TransNo AND TH.Station = TP.Station and TH.Branch = TP.Branch WHERE TH.Logged >= @StartDate AND TH.Logged <= @EndDate AND TH.Branch =@BranchID AND TH.Station = @StationID GROUP BY TH.TransNo, TP.MediaID) TP ON APM.PaymentMethod = CASE WHEN TP.MediaID = 1 THEN 'Cash' WHEN TP.MediaID = 3 THEN 'EFTPOS' WHEN TP.MediaID = 4 THEN 'Account' WHEN TP.MediaID = 9 THEN 'Credit Card' WHEN TP.MediaID = 10 THEN 'Extra Cash' WHEN TP.MediaID = 13 THEN 'Bank Payment' WHEN TP.MediaID = 7 THEN 'Credit Note' WHEN TP.MediaID = 6 THEN 'Voucher' END GROUP BY APM.PaymentMethod ORDER BY APM.PaymentMethod;";
                SqlCommand command = new SqlCommand(sqlQuery, connection);
                command.Parameters.AddWithValue("@StartDate", startDate);
                command.Parameters.AddWithValue("@EndDate", endDate);
                command.Parameters.AddWithValue("@BranchID", branchId);
                command.Parameters.AddWithValue("@StationID", stationId);

                using (SqlDataReader reader = await command.ExecuteReaderAsync())
                {
                    Dictionary<string, decimal> dictionary = new Dictionary<string, decimal>();
                    while (await reader.ReadAsync())
                    {
                        string paymentMethod = reader["PaymentMethod"].ToString();
                        decimal.TryParse(reader["ActualAmount"].ToString(), out decimal actualAmount);
                        dictionary[paymentMethod] = actualAmount;
                    }
                    return dictionary;
                }
            }

        }

        public async Task<IEnumerable<Branch>> GenerateReport(DateTime startDate, DateTime endDate)
        {
            if (!await _repository.TryGetReport(startDate))
            {
                var branches = await _reportContext.Branches.ToListAsync();
                var section = _configuration.GetSection("LanesByBranch");
                foreach (var branch in branches)
                {
                    var branchIdString = branch.Id.ToString();
                    var branchSection = section.GetSection(branchIdString);
                   
                    var NoteFromDb = await _repository.CreateNote(new Note()
                    {
                        SummaryNote = "",
                        CreatedDate = new DateTime(startDate.Year, startDate.Month, startDate.Day, startDate.Hour, startDate.Minute, startDate.Second, DateTimeKind.Utc),
                        BranchId = branch.Id
                    });

                    foreach (var laneKey in branchSection.GetChildren())
                    {
                        var laneValue = laneKey.Value;
                        int.TryParse(laneValue, out int lane);
                        var laneFromDb = await _repository.GetLaneByBranchId(lane, branch.Id);
                        if (laneFromDb is null)
                        {
                            laneFromDb = await _repository.CreateLane(new Lane()
                            {
                                LaneId = lane,
                                BranchId = branch.Id
                            });
                        }

                       var result = await ExecuteQuery(startDate, endDate, branch.Id, lane);

                        foreach (var pm in result)
                        {
                            var paymentMethod = new PaymentMethod()
                            {
                                BranchId = branch.Id,
                                LaneId = laneFromDb.Id,
                                ActualAmount = pm.Value,
                                Name = pm.Key,
                                ReportDate = new DateTime(startDate.Year, startDate.Month, startDate.Day, startDate.Hour, startDate.Minute, startDate.Second, DateTimeKind.Utc)
                            };
                            await _repository.CreatePaymentMethodReport(paymentMethod);
                        }
                    }
                }

            }
            IEnumerable<Branch> returnBranch = await _repository.Get(new DateTime(startDate.Year, startDate.Month, startDate.Day, startDate.Hour, startDate.Minute, startDate.Second, DateTimeKind.Utc));
            //foreach (var branch in returnBranch) {
            //    branch.EFTPOSFee = await _pdfService.ExecuteFEEQuery(startDate, branch);
            //    branch.Gst = await _pdfService.ExecuteGSTQuery(startDate, branch.Id);
            //}
            return returnBranch;
        }

        public async Task UpdatePaymentMethods(IEnumerable<Branch> branches)
        {
            foreach (var branch in branches)
            {
                foreach (var lane in branch.Lanes )
                {
                    foreach (var pm in lane.PaymentMethods)
                    {
                        await _repository.UpdatePaymentMethod(pm);
                    }
                    
                }
            }
            
        }

        public Task<byte[]> PdfGenerator(DateTime date, int branchId)
        {

            var branch = _reportContext.Branches.Include(x => x.Lanes).ThenInclude(l => l.PaymentMethods.Where(pm => pm.ReportDate == new DateTime(date.Year, date.Month, date.Day, date.Hour, date.Minute, date.Second, DateTimeKind.Utc))).FirstOrDefault(p => p.Id == branchId);
            if (branch != null)
            {
                return _pdfService.GenerateBranchPaymentMethodsPdf(branch, date);
            }
            return null;
        }
    }
};
