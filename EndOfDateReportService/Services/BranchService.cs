using EndOfDateReportService.ServicesInterfaces;
using Microsoft.Data.SqlClient;
using System;

namespace EndOfDateReportService.Services
{
    public class BranchService : IBranchInterface
    {
        private string connectionString;
        public BranchService(IConfiguration configuration) 
        {
            connectionString = configuration.GetConnectionString("DefaultConnection");

        }

        public Dictionary<string, decimal> ExecuteQuery(DateTime startDate, DateTime endDate, int branchId, int stationId) 
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
                        // Acceder a los datos recuperados aquí
                        string paymentMethod = reader["PaymentMethod"].ToString();
                        decimal.TryParse(reader["ActualAmount"].ToString(), out decimal actualAmount);
                        dictionary[paymentMethod] = actualAmount;
                    }
                    return dictionary;
                }
            }

        }

        public void GenerateReport() 
        { 
            
        
        }

    }
    

};