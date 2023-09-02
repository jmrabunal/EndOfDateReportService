using EndOfDateReportService.ServicesInterfaces;
using Microsoft.Data.SqlClient;
using System;

namespace EndOfDateReportService.Services
{
    public class BranchService : IBranchInterface
    {
    
    }
    var connectionString = Configuration.GetConnectionString("DefaultConnection");

    using(var connection = new SqlConnection(connectionString))
    {
        connection.Open();

        // Your raw SQL query
        var sql = "";

        // Parameters for your query
        var parameters = new { param1 = "some_value" };

        // Execute the query and map the results to YourEntity objects
        var results = connection.Query<YourEntity>(sql, parameters).ToList();
    
    }
};