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
        private readonly int AMOUNT_OF_LANES;
        public BranchService(IConfiguration configuration, ReportContext reportContext, Repository repository) 
        {
            connectionString = configuration.GetConnectionString("DefaultConnection");
            AMOUNT_OF_LANES = configuration.GetValue<int>("Lanes");
            _reportContext = reportContext;
            _repository = repository;
        }

        private Dictionary<string, decimal> ExecuteQuery(DateTime startDate, DateTime endDate, int branchId, int stationId) 
        {
            using (SqlConnection connection = new SqlConnection(connectionString))
            {
                connection.Open();

                string sqlQuery = "SELECT\r\n    CASE\r\n        WHEN TP.MediaID = 1 THEN 'Cash'\r\n        WHEN TP.MediaID = 3 THEN 'EFTPOS'\r\n        WHEN TP.MediaID = 4 THEN 'Account'\r\n        WHEN TP.MediaID = 9 THEN 'Credit Card'\r\n        WHEN TP.MediaID = 10 THEN 'CLICK AND COLLECT'\r\n        WHEN TP.MediaID = 13 THEN 'Bank Payment'\r\n        WHEN TP.MediaID = 7 THEN 'Credit Note'\r\n        WHEN TP.MediaID = 6 THEN 'Voucher'\r\n        -- Add more WHEN clauses for other IDs if necessary\r\n    END AS PaymentMethod,\r\n    SUM(TP.Value) AS ActualAmount\r\nFROM Infinity.dbo.TransHeaders TH\r\nINNER JOIN Infinity.dbo.TransPayments TP ON TH.TransNo = TP.TransNo AND TH.Station = TP.Station\r\nWHERE TH.Logged >= @StartDate AND TH.Logged <= @EndDate\r\n    AND TH.Branch = @BranchID\r\n    AND TH.Station = @StationID\r\nGROUP BY\r\n    CASE\r\n        WHEN TP.MediaID = 1 THEN 'Cash'\r\n        WHEN TP.MediaID = 3 THEN 'EFTPOS'\r\n        WHEN TP.MediaID = 4 THEN 'Account'\r\n        WHEN TP.MediaID = 9 THEN 'Credit Card'\r\n        WHEN TP.MediaID = 10 THEN 'CLICK AND COLLECT'\r\n        WHEN TP.MediaID = 13 THEN 'Bank Payment'\r\n        WHEN TP.MediaID = 7 THEN 'Credit Note'\r\n        WHEN TP.MediaID = 6 THEN 'Voucher'\r\n        -- Add more WHEN clauses for other IDs if necessary\r\n    END;";
                
                SqlCommand command = new SqlCommand(sqlQuery, connection);
                command.Parameters.AddWithValue("@StartDate", startDate);
                command.Parameters.AddWithValue("@EndDate", endDate);
                command.Parameters.AddWithValue("@BranchID", branchId);
                command.Parameters.AddWithValue("@StationID", stationId);

                using (SqlDataReader reader = command.ExecuteReader())
                {
                    Dictionary<string, decimal> dictionary = new Dictionary<string, decimal>();
                    while (reader.Read())
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
            var branches = await _reportContext.Branches.ToListAsync();
            var lanes = AMOUNT_OF_LANES;

            foreach (var branch in branches)
            {
                for (int lane=1;lane<=lanes;lane++)
                {
                    _repository.CreateLane(new Lane()
                    {
                        Id = lane,
                        BranchId = branch.Id
                    });

                    var result = ExecuteQuery(startDate, endDate, branch.Id, lane);
                    foreach (var pm in result)
                    {
                        var paymentMethod = new PaymentMethod()
                        {
                            BranchId = branch.Id,
                            LaneId = lane,
                            ActualAmount = pm.Value,
                            Name = pm.Key,
                            ReportDate = startDate
                        };
                        _repository.CreatePaymentMethodReport(paymentMethod);
                    }
                   
                }
            }

            return await _repository.Get(startDate);
        }
        
    }
    

};