using EndOfDateReportService.ServicesInterfaces;
using Microsoft.Data.SqlClient;
using System;
using EndOfDateReportService.DataAccess;
using EndOfDateReportService.Domain;
using Microsoft.EntityFrameworkCore;

namespace EndOfDateReportService.Services
{
    public class BranchService : IBranchInterface
    {
        private string connectionString;
        private ReportContext _reportContext;
        private Repository _repository;
        private readonly IConfiguration _configuration;
        public BranchService(IConfiguration configuration, ReportContext reportContext, Repository repository)
        {
            _configuration = configuration;
            connectionString = configuration.GetConnectionString("DefaultConnection");
            _reportContext = reportContext;
            _repository = repository;
        }

        private async Task<Dictionary<string, decimal>> ExecuteQuery(DateTime startDate, DateTime endDate, int branchId, int stationId) 
        {
            using (SqlConnection connection = new SqlConnection(connectionString))
            {
                await connection.OpenAsync();

                string sqlQuery = "WITH AllPaymentMethods AS (\r\n    SELECT 'Cash' AS PaymentMethod\r\n    UNION ALL\r\n    SELECT 'EFTPOS'\r\n    UNION ALL\r\n    SELECT 'Account'\r\n    UNION ALL\r\n    SELECT 'Credit Card'\r\n    UNION ALL\r\n    SELECT 'CLICK AND COLLECT'\r\n    UNION ALL\r\n    SELECT 'Bank Payment'\r\n    UNION ALL\r\n    SELECT 'Credit Note'\r\n    UNION ALL\r\n    SELECT 'Voucher'\r\n    -- Add more PaymentMethods if necessary\r\n)\r\nSELECT\r\n    APM.PaymentMethod,\r\n    COALESCE(SUM(TP.Value), 0) AS ActualAmount\r\nFROM AllPaymentMethods APM\r\nLEFT JOIN (\r\n    SELECT TH.TransNo, TP.MediaID, SUM(TP.Value) AS Value\r\n    FROM Infinity.dbo.TransHeaders TH\r\n    LEFT JOIN Infinity.dbo.TransPayments TP ON TH.TransNo = TP.TransNo AND TH.Station = TP.Station\r\n    WHERE TH.Logged >= @StartDate AND TH.Logged <= @EndDate\r\n    AND TH.Branch = @BranchId AND TH.Station = @StationId\r\n    GROUP BY TH.TransNo, TP.MediaID\r\n) TP ON APM.PaymentMethod = \r\n    CASE\r\n        WHEN TP.MediaID = 1 THEN 'Cash'\r\n        WHEN TP.MediaID = 3 THEN 'EFTPOS'\r\n        WHEN TP.MediaID = 4 THEN 'Account'\r\n        WHEN TP.MediaID = 9 THEN 'Credit Card'\r\n        WHEN TP.MediaID = 10 THEN 'CLICK AND COLLECT'\r\n        WHEN TP.MediaID = 13 THEN 'Bank Payment'\r\n        WHEN TP.MediaID = 7 THEN 'Credit Note'\r\n        WHEN TP.MediaID = 6 THEN 'Voucher'\r\n        -- Add more WHEN clauses for other IDs if necessary\r\n    END\r\nGROUP BY APM.PaymentMethod\r\nORDER BY APM.PaymentMethod;";
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
            if(!await _repository.TryGetReport(startDate))
            {
                var branches = await _reportContext.Branches.ToListAsync();

                foreach (var branch in branches)
                {
                    int.TryParse(_configuration.GetSection("LanesByBranch").GetValue<string>(branch.Id.ToString()), out int branchAmountOfLanes);
                
                    for (int lane=1;lane<=branchAmountOfLanes;lane++)
                    {
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
            
            return await _repository.Get(new DateTime(startDate.Year, startDate.Month, startDate.Day, startDate.Hour, startDate.Minute, startDate.Second, DateTimeKind.Utc));
        }

        public async Task UpdatePaymentMethods(IEnumerable<PaymentMethod> paymentMethods)
        {
            foreach (var pm in paymentMethods)
            {
                await _repository.UpdatePaymentMethod(pm);
            }
            
        }
        
    }
    

};
