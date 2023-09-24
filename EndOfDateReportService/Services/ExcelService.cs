using System.Data;
using EndOfDateReportService.Domain;
using Microsoft.Data.SqlClient;
using OfficeOpenXml;
using OfficeOpenXml.Style;

namespace EndOfDateReportService.Services;

public class ExcelService
{
    private readonly string connectionString;
    private readonly IConfiguration _configuration;
    private List<string> suppliers = new List<string>();

    public ExcelService(IConfiguration configuration)
    {
        connectionString = configuration.GetConnectionString("DefaultConnection");
        _configuration = configuration;

        ExcelPackage.LicenseContext = LicenseContext.NonCommercial;
    }

    private async Task<DataTable> ExecuteCommissionQuery(DateTime fromDateInclusive, DateTime toDateInclusive)
    {
        using (SqlConnection connection = new SqlConnection(connectionString))
        {
            await connection.OpenAsync();

            string createViewScript = @"
        -- CREATE VIEW
        IF OBJECT_ID('dbo.vw_ListCommission', 'V') IS NULL
        BEGIN
            EXEC('
                CREATE VIEW dbo.vw_ListCommission AS
                SELECT
                    CONVERT(varchar(10), TH.Logged, 120) AS Date, -- Format date as yyyy-MM-dd
                    B.Name AS Branch,
                    I.UPC,
                    I.SKU,
                    I.Description,
                    I.Supplier,
                    C.LastName,
                    I.Field_Integer AS [Commission Rate],
                    TL.PriceSet,
                    SUM(CASE WHEN DATEPART(dw, TH.Logged) = 2 THEN TL.Quantity ELSE 0 END) AS MON_QTY,
                    SUM(CASE WHEN DATEPART(dw, TH.Logged) = 3 THEN TL.Quantity ELSE 0 END) AS TUE_QTY,
                    SUM(CASE WHEN DATEPART(dw, TH.Logged) = 4 THEN TL.Quantity ELSE 0 END) AS WED_QTY,
                    SUM(CASE WHEN DATEPART(dw, TH.Logged) = 5 THEN TL.Quantity ELSE 0 END) AS THU_QTY,
                    SUM(CASE WHEN DATEPART(dw, TH.Logged) = 6 THEN TL.Quantity ELSE 0 END) AS FRI_QTY,
                    SUM(CASE WHEN DATEPART(dw, TH.Logged) = 7 THEN TL.Quantity ELSE 0 END) AS SAT_QTY,
                    SUM(CASE WHEN DATEPART(dw, TH.Logged) = 1 THEN TL.Quantity ELSE 0 END) AS SUN_QTY
                FROM
                    translines AS TL
                JOIN
                    Items AS I ON TL.UPC = I.UPC
                JOIN
                    Branches B ON TL.Branch = B.id
                JOIN
                    Customers AS C ON I.Supplier = C.Code
                JOIN
                    TransHeaders AS TH ON TL.Branch = TH.Branch
                                     AND TL.TransNo = TH.TransNo
                                     AND TL.Station = TH.Station
                WHERE
                    I.Field_Integer is not null
                GROUP BY
                    CONVERT(varchar(10), TH.Logged, 120), -- Format date as yyyy-MM-dd
                    B.Name,
                    I.UPC,
                    I.SKU,
                    I.Description,
                    I.Supplier,
                    C.LastName,
                    I.Field_Integer,
                    TL.PriceSet;
            ');
        END;
    ";

            string createProcedureScript = @"
    -- CREATE PROCEDURE
    IF OBJECT_ID('dbo.sp_SupplierCommission', 'P') IS NULL
    BEGIN
        EXEC('
            CREATE PROCEDURE dbo.sp_SupplierCommission
                @SupplierCode VARCHAR(20),
                @FromDateInclusive DATE,
                @ToDateInclusive DATE
            AS
            BEGIN
                SELECT
                    [Last Name],
                    Branch,
                    UPC,
                    Description,
                    dbo.vw_SupplierCommission.[Commission Rate] AS [Commission Rate], -- Specify the column name
                    SUM(MON_QTY) AS ''Monday'',
                    SUM(TUE_QTY) AS ''Tuesday'',
                    SUM(WED_QTY) AS ''Wednesday'',
                    SUM(THU_QTY) AS ''Thursday'',
                    SUM(FRI_QTY) AS ''Friday'',
                    SUM(SAT_QTY) AS ''Saturday'',
                    SUM(SUN_QTY) AS ''Sunday''
                FROM
                    dbo.vw_SupplierCommission
                WHERE
                    [Date] BETWEEN @FromDateInclusive AND @ToDateInclusive
                    AND UPC IN (SELECT UPC FROM Items WHERE Supplier = @SupplierCode)
                GROUP BY
                    [Last Name],
                    Branch,
                    UPC,
                    Description,
                    dbo.vw_SupplierCommission.[Commission Rate] -- Specify the table alias
                ORDER BY
                    Branch,
                    Description,
                    dbo.vw_SupplierCommission.[Commission Rate]; -- Specify the table alias
            END;
        ');
    END;
";


            string combinedScript = createViewScript + createProcedureScript;

            using (SqlCommand command = new SqlCommand(combinedScript, connection))
            {
                await command.ExecuteNonQueryAsync();

                using (SqlCommand spCommand = new SqlCommand("dbo.sp_ListCommission", connection))
                {
                    spCommand.CommandType = CommandType.StoredProcedure;
                    spCommand.Parameters.AddWithValue("@FromDateInclusive", fromDateInclusive);
                    spCommand.Parameters.AddWithValue("@ToDateInclusive", toDateInclusive);

                    using (SqlDataAdapter adapter = new SqlDataAdapter(spCommand))
                    {
                        DataTable dataTable = new DataTable();
                        await Task.Run(() => adapter.Fill(dataTable));
                        dataTable.Columns["Commision Rate"].ColumnName = "Commission Rate";

                        return dataTable;
                    }
                }
            }
        }
    }




    private async Task<DataTable> ExecuteSupplierCommisionQuery(string supplierCode, DateTime fromDateInclusive, DateTime toDateInclusive)
    {
        using (SqlConnection connection = new SqlConnection(connectionString))
        {
            await connection.OpenAsync();

            string createViewScript = @"
            -- CREATE VIEW
            IF OBJECT_ID('dbo.vw_SupplierCommission', 'V') IS NULL
            BEGIN
                EXEC('
                    CREATE VIEW dbo.vw_SupplierCommission AS
                    SELECT
                        C.LastName AS ''Last Name'',
                        B.Name AS ''Branch'',
                        I.UPC,
                        I.Description,
                        I.Field_Integer AS [Commission Rate],
                        CAST(TH.Logged AS DATE) AS ''Date'',
                        SUM(CASE WHEN DATEPART(dw, TH.Logged) = 2 THEN TL.Quantity ELSE 0 END) AS MON_QTY,
                        SUM(CASE WHEN DATEPART(dw, TH.Logged) = 3 THEN TL.Quantity ELSE 0 END) AS TUE_QTY,
                        SUM(CASE WHEN DATEPART(dw, TH.Logged) = 4 THEN TL.Quantity ELSE 0 END) AS WED_QTY,
                        SUM(CASE WHEN DATEPART(dw, TH.Logged) = 5 THEN TL.Quantity ELSE 0 END) AS THU_QTY,
                        SUM(CASE WHEN DATEPART(dw, TH.Logged) = 6 THEN TL.Quantity ELSE 0 END) AS FRI_QTY,
                        SUM(CASE WHEN DATEPART(dw, TH.Logged) = 7 THEN TL.Quantity ELSE 0 END) AS SAT_QTY,
                        SUM(CASE WHEN DATEPART(dw, TH.Logged) = 1 THEN TL.Quantity ELSE 0 END) AS SUN_QTY,
                        SUM(TL.Quantity) AS Total,  
                        SUM(CASE WHEN DATEPART(dw, TH.Logged) IN (2, 3, 4, 5, 6, 7, 1) THEN TL.Quantity ELSE 0 END) AS Totals,  
                        SUM(CASE WHEN DATEPART(dw, TH.Logged) = 2 THEN TL.Quantity * TL.PriceSet ELSE 0 END) AS MondaySales,
                        SUM(CASE WHEN DATEPART(dw, TH.Logged) = 3 THEN TL.Quantity * TL.PriceSet ELSE 0 END) AS TuesdaySales,
                        SUM(CASE WHEN DATEPART(dw, TH.Logged) = 4 THEN TL.Quantity * TL.PriceSet ELSE 0 END) AS WednesdaySales,
                        SUM(CASE WHEN DATEPART(dw, TH.Logged) = 5 THEN TL.Quantity * TL.PriceSet ELSE 0 END) AS ThursdaySales,
                        SUM(CASE WHEN DATEPART(dw, TH.Logged) = 6 THEN TL.Quantity * TL.PriceSet ELSE 0 END) AS FridaySales,
                        SUM(CASE WHEN DATEPART(dw, TH.Logged) = 7 THEN TL.Quantity * TL.PriceSet ELSE 0 END) AS SaturdaySales,
                        SUM(CASE WHEN DATEPART(dw, TH.Logged) = 1 THEN TL.Quantity * TL.PriceSet ELSE 0 END) AS SundaySales,
                        SUM(TL.Quantity * TL.PriceSet) AS TotalSales,  
                        (SUM(TL.Quantity * TL.PriceSet) * I.Field_Integer/100) AS Commission,  
                        SUM(TL.Quantity * TL.PriceSet) - (SUM(TL.Quantity * TL.PriceSet) * I.Field_Integer/100) AS Net  
                    FROM
                        TransLines AS TL
                    JOIN Items AS I ON TL.UPC = I.UPC
                    JOIN Branches AS B ON TL.Branch = B.ID
                    JOIN TransHeaders AS TH ON TL.Branch = TH.Branch
                                        AND TL.TransNo = TH.TransNo
                                        AND TL.Station = TH.Station
                    JOIN Customers AS C ON I.Supplier = C.Code
                    WHERE
                        I.Field_Integer is not null
                    GROUP BY
                        C.LastName,
                        B.Name,  
                        I.UPC,
                        I.Description,
                        I.Field_Integer,
                        CAST(TH.Logged AS DATE);
                ');
            END;
        ";

            string createProcedureScript = @"
            -- CREATE PROCEDURE
            IF OBJECT_ID('dbo.sp_SupplierCommission', 'P') IS NULL
            BEGIN
                EXEC('
                    CREATE PROCEDURE dbo.sp_SupplierCommission
                        @SupplierCode VARCHAR(20),
                        @FromDateInclusive DATE,
                        @ToDateInclusive DATE
                    AS
                    BEGIN
                        SELECT
                            [Last Name],
                            Branch,
                            UPC,
                            Description,
                            dbo.vw_SupplierCommission.[Commission Rate] AS [Commission Rate], -- Specify the table alias
                            SUM(MON_QTY) AS ''Monday'',
                            SUM(TUE_QTY) AS ''Tuesday'',
                            SUM(WED_QTY) AS ''Wednesday'',
                            SUM(THU_QTY) AS ''Thursday'',
                            SUM(FRI_QTY) AS ''Friday'',
                            SUM(SAT_QTY) AS ''Saturday'',
                            SUM(SUN_QTY) AS ''Sunday'',
                            SUM(Totals) AS ''Totals'',
                            SUM(MondaySales) AS ''Monday Sales'',
                            SUM(TuesdaySales) AS ''Tuesday Sales'',
                            SUM(WednesdaySales) AS ''Wednesday Sales'',
                            SUM(ThursdaySales) AS ''Thursday Sales'',
                            SUM(FridaySales) AS ''Friday Sales'',
                            SUM(SaturdaySales) AS ''Saturday Sales'',
                            SUM(SundaySales) AS ''Sunday Sales'',
                            SUM(TotalSales) AS ''Total Sales'',
                            SUM(Commission) AS ''Commission'',
                            SUM(Net) AS ''Net''
                        FROM
                            dbo.vw_SupplierCommission
                        WHERE
                            [Date] BETWEEN @FromDateInclusive AND @ToDateInclusive
                            AND UPC IN (SELECT UPC FROM Items WHERE Supplier = @SupplierCode)
                        GROUP BY
                            [Last Name],
                            Branch,
                            UPC,
                            Description,
                            dbo.vw_SupplierCommission.[Commission Rate] -- Specify the table alias
                        ORDER BY
                            Branch,
                            Description,
                            dbo.vw_SupplierCommission.[Commission Rate]; -- Specify the table alias
                    END;
                ');
            END;
        ";

            string combinedScript = createViewScript + createProcedureScript;

            using (SqlCommand command = new SqlCommand(combinedScript, connection))
            {
                await command.ExecuteNonQueryAsync();
            }

            using (SqlCommand spCommand = new SqlCommand("dbo.sp_SupplierCommission", connection))
            {
                spCommand.CommandType = CommandType.StoredProcedure;
                spCommand.Parameters.AddWithValue("@SupplierCode", supplierCode);
                spCommand.Parameters.AddWithValue("@FromDateInclusive", fromDateInclusive);
                spCommand.Parameters.AddWithValue("@ToDateInclusive", toDateInclusive);

                using (SqlDataAdapter adapter = new SqlDataAdapter(spCommand))
                {
                    DataTable dataTable = new DataTable();
                    await Task.Run(() => adapter.Fill(dataTable));
                    return dataTable;
                }
            }
        }
    }


    private async Task<DataTable> ExecuteSummaryQuery(DateTime fromDateInclusive, DateTime toDateInclusive)
    {
        using (SqlConnection connection = new SqlConnection(connectionString))
        {
            await connection.OpenAsync();

            string script = @"
        IF OBJECT_ID('dbo.vw_SummaryCommission', 'V') IS NOT NULL
        BEGIN
            EXEC('DROP VIEW dbo.vw_SummaryCommission;');
        END;

        EXEC('
            CREATE VIEW dbo.vw_SummaryCommission AS
            SELECT
                C.LastName AS ''Supplier'',
                CAST(TH.Logged AS DATE) AS ''Date'',
                SUM(TL.Quantity * TL.PriceSet) AS Total,  
                (SUM(TL.Quantity * TL.PriceSet) * I.Field_Integer/100) AS Commission,  
                SUM(TL.Quantity * TL.PriceSet) - (SUM(TL.Quantity * TL.PriceSet) * I.Field_Integer/100) AS Net ,
                SUM(TL.Quantity * TL.PriceSet) - (SUM(TL.Quantity * TL.PriceSet) * I.Field_Integer/100) AS Sheet  
            FROM
                TransLines AS TL
            JOIN Items AS I ON TL.UPC = I.UPC
            JOIN Branches AS B ON TL.Branch = B.ID
            JOIN TransHeaders AS TH ON TL.Branch = TH.Branch
                                  AND TL.TransNo = TH.TransNo
                                  AND TL.Station = TH.Station
            JOIN Customers AS C ON I.Supplier = C.Code
            WHERE
                I.Field_Integer is not null
            GROUP BY
                C.LastName,
                CAST(TH.Logged AS DATE),
                I.Field_Integer;
        ');

        IF OBJECT_ID('dbo.sp_SummaryCommission', 'P') IS NOT NULL
        BEGIN
            EXEC('DROP PROCEDURE dbo.sp_SummaryCommission;');
        END;

        EXEC('
            CREATE PROCEDURE dbo.sp_SummaryCommission
                @FromDateInclusive DATE,
                @ToDateInclusive DATE
            AS
            BEGIN
                SELECT DISTINCT
                    [Supplier],
                    SUM(Total) AS ''Total'',
                    SUM(Commission) AS ''Commission'',
                    SUM(Net) AS ''Net'',
                    SUM(Sheet) AS ''Sheet''
                FROM
                    dbo.vw_SummaryCommission
                WHERE
                    [Date] BETWEEN @FromDateInclusive AND @ToDateInclusive
                GROUP BY
                    [Supplier]
                ORDER BY
                    [Supplier];
            END;
        ');
        ";

            using (SqlCommand command = new SqlCommand(script, connection))
            {
                await command.ExecuteNonQueryAsync();
            }



            using (SqlCommand spCommand = new SqlCommand("dbo.sp_SummaryCommission", connection))
            {
                spCommand.CommandType = CommandType.StoredProcedure;
                spCommand.Parameters.AddWithValue("@FromDateInclusive", fromDateInclusive);
                spCommand.Parameters.AddWithValue("@ToDateInclusive", toDateInclusive);

                using (SqlDataAdapter adapter = new SqlDataAdapter(spCommand))
                {
                    DataTable dataTable = new DataTable();
                    await Task.Run(() => adapter.Fill(dataTable));
                    return dataTable;
                }
            }
        }
    }


    private async Task<DataTable> ExecuteDraftWorkingOutputQuery(DateTime fromDateInclusive, DateTime toDateInclusive)
    {
        using (SqlConnection connection = new SqlConnection(connectionString))
        {
            await connection.OpenAsync();

            string createViewScript = @"
        IF OBJECT_ID('dbo.vw_AllSupplierCommission', 'V') IS NULL
        BEGIN
            EXEC('
                CREATE VIEW dbo.vw_AllSupplierCommission AS
                SELECT
                    C.LastName AS ''Supplier'',
                    CAST(TH.Logged AS DATE) AS ''Date'',
                    SUM(CASE WHEN DATEPART(dw, TH.Logged) = 2 THEN TL.Quantity * TL.PriceSet ELSE 0 END) AS MondaySales,
                    SUM(CASE WHEN DATEPART(dw, TH.Logged) = 3 THEN TL.Quantity * TL.PriceSet ELSE 0 END) AS TuesdaySales,
                    SUM(CASE WHEN DATEPART(dw, TH.Logged) = 4 THEN TL.Quantity * TL.PriceSet ELSE 0 END) AS WednesdaySales,
                    SUM(CASE WHEN DATEPART(dw, TH.Logged) = 5 THEN TL.Quantity * TL.PriceSet ELSE 0 END) AS ThursdaySales,
                    SUM(CASE WHEN DATEPART(dw, TH.Logged) = 6 THEN TL.Quantity * TL.PriceSet ELSE 0 END) AS FridaySales,
                    SUM(CASE WHEN DATEPART(dw, TH.Logged) = 7 THEN TL.Quantity * TL.PriceSet ELSE 0 END) AS SaturdaySales,
                    SUM(CASE WHEN DATEPART(dw, TH.Logged) = 1 THEN TL.Quantity * TL.PriceSet ELSE 0 END) AS SundaySales,
                    SUM(TL.Quantity * TL.PriceSet) AS TotalSales,
                    (SUM(TL.Quantity * TL.PriceSet) * I.Field_Integer/100) AS Commission,
                    SUM(TL.Quantity * TL.PriceSet) - (SUM(TL.Quantity * TL.PriceSet) * I.Field_Integer/100) AS Net  
                FROM
                    TransLines AS TL
                JOIN Items AS I ON TL.UPC = I.UPC
                JOIN TransHeaders AS TH ON TL.Branch = TH.Branch
                                  AND TL.TransNo = TH.TransNo
                                  AND TL.Station = TH.Station
                JOIN Customers AS C ON I.Supplier = C.Code
                WHERE
                    I.Field_Integer is not null
                GROUP BY
                    C.LastName,
                    I.Field_Integer,
                    CAST(TH.Logged AS DATE);
            ');
        END;
        ";
            string createProcedureScript = @"
        IF OBJECT_ID('dbo.sp_AllSupplierCommission', 'P') IS NULL
        BEGIN
            EXEC('
                CREATE PROCEDURE dbo.sp_AllSupplierCommission
                    @FromDateInclusive DATE,
                    @ToDateInclusive DATE
                AS
                BEGIN
                    SELECT
                        Supplier,
                        SUM(MondaySales) AS ''Monday Sales'',
                        SUM(TuesdaySales) AS ''Tuesday Sales'',
                        SUM(WednesdaySales) AS ''Wednesday Sales'',
                        SUM(ThursdaySales) AS ''Thursday Sales'',
                        SUM(FridaySales) AS ''Friday Sales'',
                        SUM(SaturdaySales) AS ''Saturday Sales'',
                        SUM(SundaySales) AS ''Sunday Sales'',
                        SUM(TotalSales) AS ''Total Sales'',
                        SUM(Commission) AS ''Commission'',
                        SUM(Net) AS ''Net''
                    FROM
                        dbo.vw_AllSupplierCommission
                    WHERE
                        [Date] BETWEEN @FromDateInclusive AND @ToDateInclusive
                    GROUP BY
                        Supplier;
                END;
            ');
        END;
        ";


            string combinedScript = createViewScript + createProcedureScript;

            using (SqlCommand command = new SqlCommand(combinedScript, connection))
            {
                await command.ExecuteNonQueryAsync();
            }

            using (SqlCommand spCommand = new SqlCommand("dbo.sp_AllSupplierCommission", connection))
            {
                spCommand.CommandType = CommandType.StoredProcedure;
                spCommand.Parameters.AddWithValue("@FromDateInclusive", fromDateInclusive);
                spCommand.Parameters.AddWithValue("@ToDateInclusive", toDateInclusive);

                using (SqlDataAdapter adapter = new SqlDataAdapter(spCommand))
                {
                    DataTable dataTable = new DataTable();
                    await Task.Run(() => adapter.Fill(dataTable));
                    return dataTable;
                }
            }
        }
    }



    public async Task ExportToExcel(DateTime fromDateInclusive, DateTime toDateInclusive)
    {

        var path = _configuration.GetSection("commisionSalesPath");
        string currentDirectory = Directory.GetCurrentDirectory() + "//" + path.Value;

        var dateFormatted = fromDateInclusive.Date.ToString("yyyy-MM-dd").Replace("/", "-");
        string filename = $"CommissionSales - {dateFormatted}.xlsm";
        string fullPath = Path.Combine(currentDirectory, filename);
        FileInfo fileInfo = new FileInfo(fullPath);

        if (fileInfo.Exists)
        {
            fileInfo.Delete();
        }

        using (ExcelPackage package = new ExcelPackage(fileInfo))
        {

            var dataTable = await ExecuteCommissionQuery(fromDateInclusive, toDateInclusive);
            CreateCommisionSalesSheet(dataTable, package);

            var dataTableSummary = await ExecuteSummaryQuery(fromDateInclusive, toDateInclusive);
            CreateSummarySheet(dataTableSummary, package, fromDateInclusive);

            var dataTableDraftWorkingOutput = await ExecuteDraftWorkingOutputQuery(fromDateInclusive, toDateInclusive);
            CreateDraftWorkingOutputSheet(dataTableDraftWorkingOutput, package);

            GetSupliers(dataTable);
            foreach (var supplier in suppliers)
            {
                var supplierDataTable =  await ExecuteSupplierCommisionQuery(supplier, fromDateInclusive, toDateInclusive);
                CreateSupplierComissionSheet(supplier, supplierDataTable, package);
            }
            package.Save();
        }

    }

    private void CreateDraftWorkingOutputSheet(DataTable dataTable, ExcelPackage package)
    {
        var brightGreen = System.Drawing.ColorTranslator.FromHtml("#A4CAFB");
        var decimalColumns = new List<string> { "Commission", "Net" };
        var worksheet = package.Workbook.Worksheets.Add("Draft Working Output");
        worksheet.DefaultColWidth = 15;

        for (int i = 0; i < dataTable.Columns.Count; i++)
        {

            worksheet.Cells[1, i + 1].Value = dataTable.Columns[i].ColumnName;
            worksheet.Cells[1, i + 1].Style.Fill.PatternType = ExcelFillStyle.Solid;
            worksheet.Cells[1, i + 1].Style.Fill.BackgroundColor.SetColor(brightGreen);

            if (dataTable.Columns[i].ColumnName.EndsWith("Sales") || decimalColumns.Contains(dataTable.Columns[i].ColumnName))
            {
                decimal totalData = 0;
                int row;
                for (row = 0; row < dataTable.Rows.Count; row++)
                {
                    var cell = worksheet.Cells[row + 2, i + 1];
                    cell.Style.Numberformat.Format = "$0.00";
                    cell.Value = dataTable.Rows[row][i];
                    decimal.TryParse(dataTable.Rows[row][i].ToString(), out decimal data);
                    totalData += data;
                }
                if (row > 0)
                {
                    var celTotal = worksheet.Cells[row + 2, i + 1];
                    celTotal.Style.Numberformat.Format = "$0.00";
                    celTotal.Style.Font.Bold = true;
                    celTotal.Value = totalData;
                }
            }
            else
            {
                for (int row = 0; row < dataTable.Rows.Count; row++)
                {
                    worksheet.Cells[row + 2, i + 1].Value = dataTable.Rows[row][i];
                }

            }
        }
    }

    private void CreateSummarySheet(DataTable dataTable, ExcelPackage package, DateTime calculationDate)
    {
        var worksheet = package.Workbook.Worksheets.Add("Summary");
        var decimalColumns = new List<string> { "Total", "Commission", "Net", "Sheet" };

        worksheet.Column(1).Width = 30;
        worksheet.Column(2).Width = 15;
        worksheet.Column(3).Width = 15;
        worksheet.Column(4).Width = 15;
        worksheet.Column(5).Width = 15;

        worksheet.Cells[1, 1].Style.Numberformat.Format = "yyyy-MM-dd";

        worksheet.Cells[1,1].Value = "COMMISSION " + calculationDate.ToShortDateString();
        worksheet.Cells[1, 1].Style.Fill.PatternType = ExcelFillStyle.Solid;
        worksheet.Cells[1, 1].Style.Fill.BackgroundColor.SetColor(System.Drawing.ColorTranslator.FromHtml("#F8E6D8"));



        for (int i =0; i < dataTable.Columns.Count; i++)
        {
            worksheet.Cells[2, i + 1].Value = dataTable.Columns[i].ColumnName;
            worksheet.Cells[2, i + 1].Style.Font.Bold = true;

            if (dataTable.Columns[i].ColumnName.EndsWith("Sales") || decimalColumns.Contains(dataTable.Columns[i].ColumnName))
            {
                decimal totalData = 0;
                int row;
                for (row = 0; row < dataTable.Rows.Count; row++)
                {
                    var cell = worksheet.Cells[row + 3, i + 1];
                    cell.Style.Numberformat.Format = "$0.00";
                    cell.Value = dataTable.Rows[row][i];
                    decimal.TryParse(dataTable.Rows[row][i].ToString(), out decimal data);
                    totalData += data;
                }
                if (row > 0)
                {
                    var celTotal = worksheet.Cells[row + 3, i + 1];
                    celTotal.Style.Numberformat.Format = "$0.00";
                    celTotal.Style.Font.Bold = true;
                    celTotal.Value = totalData;
                }
                row = 0;
            }
            else {
                for (int row = 0; row < dataTable.Rows.Count; row++)
                {
                    worksheet.Cells[row + 3, i + 1].Value = dataTable.Rows[row][i];
                }
            }
        }

    }






    private void GetSupliers(DataTable dataTable)
    {
        for (int i = 0; i < dataTable.Columns.Count; i++)
        {
            if (dataTable.Columns[i].ColumnName == "Supplier")
            {
                for (int row = 0; row < dataTable.Rows.Count; row++)
                {
                    if (!suppliers.Contains(dataTable.Rows[row][i]))
                    {
                        suppliers.Add(dataTable.Rows[row][i].ToString());
                    }
                }
            }
        }
    }

    public void CreateSupplierComissionSheet(string supplier, DataTable dataTable, ExcelPackage package)
    {
        var brightGreen = System.Drawing.ColorTranslator.FromHtml("#A4CAFB");
        var decimalColumns = new List<string> { "Commission", "Net" };
        ExcelWorksheet worksheet = null;
        int count = 0;

        for (int i = 0; i < dataTable.Columns.Count; i++)
        {
            if (dataTable.Columns[i].ColumnName == "Last Name" && count == 0)
            {
                worksheet = package.Workbook.Worksheets.Add(dataTable.Rows[0][i].ToString());
                worksheet.DefaultColWidth = 15;
                count = 1;
            }

            if (dataTable.Columns[i].ColumnName != "Last Name")
            {
                worksheet.Cells[1, i].Value = dataTable.Columns[i].ColumnName;
                worksheet.Cells[1, i].Style.Fill.PatternType = ExcelFillStyle.Solid;
                worksheet.Cells[1, i].Style.Fill.BackgroundColor.SetColor(brightGreen);
            }

            if (dataTable.Columns[i].ColumnName == "Commission Rate")
            {
                for (int row = 0; row < dataTable.Rows.Count; row++)
                {
                    var cell = worksheet.Cells[row + 2, i];
                    cell.Style.Numberformat.Format = "0.00\\%";
                    cell.Value = dataTable.Rows[row][i];
                }
            }
            else if (dataTable.Columns[i].ColumnName.EndsWith("ay") || dataTable.Columns[i].ColumnName.Equals("Totals"))
            {
                for (int row = 0; row < dataTable.Rows.Count; row++)
                {
                    var cell = worksheet.Cells[row + 2, i];
                    cell.Style.Numberformat.Format = "#,##0.000";
                    cell.Value = dataTable.Rows[row][i];
                }
            }
            else if (dataTable.Columns[i].ColumnName.EndsWith("Sales") || decimalColumns.Contains(dataTable.Columns[i].ColumnName))
            {
                decimal totalData = 0;
                int row;
                for (row = 0; row < dataTable.Rows.Count; row++)
                {
                    var cell = worksheet.Cells[row + 2, i];
                    cell.Style.Numberformat.Format = "$0.00";
                    cell.Value = dataTable.Rows[row][i];
                    decimal.TryParse(dataTable.Rows[row][i].ToString(), out decimal data);
                    totalData += data;
                }
                if (row > 0)
                {
                    var celTotal = worksheet.Cells[row + 2, i];
                    celTotal.Style.Numberformat.Format = "$0.00";
                    celTotal.Style.Font.Bold = true;
                    celTotal.Value = totalData;
                }
            }
            else
            {
                if (dataTable.Columns[i].ColumnName != "Last Name")
                {
                    for (int row = 0; row < dataTable.Rows.Count; row++)
                    {
                        worksheet.Cells[row + 2, i].Value = dataTable.Rows[row][i];
                    }
                }
            }
        }
    }



    public void CreateCommisionSalesSheet(DataTable dataTable, ExcelPackage package)
    {
        ExcelWorksheet worksheet = package.Workbook.Worksheets.Add("CommissionSales");
        var brightGreen = System.Drawing.ColorTranslator.FromHtml("#AAFF17");
        worksheet.DefaultColWidth = 15;

        for (int i = 0; i < dataTable.Columns.Count; i++)
        {
            worksheet.Cells[1, i + 1].Value = dataTable.Columns[i].ColumnName;

            worksheet.Cells[1, i + 1].Style.Fill.PatternType = ExcelFillStyle.Solid;
            worksheet.Cells[1, i + 1].Style.Fill.BackgroundColor.SetColor(brightGreen);

            if (dataTable.Columns[i].ColumnName == "Period From" || dataTable.Columns[i].ColumnName == "Period To")
            {
                for (int row = 0; row < dataTable.Rows.Count; row++)
                {
                    var cell = worksheet.Cells[row + 2, i + 1];
                    cell.Style.Numberformat.Format = "MM/dd/yy";

                    if (DateTime.TryParse(dataTable.Rows[row][i].ToString(), out DateTime dateValue))
                    {
                        cell.Value = dateValue.ToString("MM/dd/yy");
                    }
                    else
                    {
                        cell.Value = dataTable.Rows[row][i];
                    }
                }
            }
            else if (dataTable.Columns[i].ColumnName == "Commission Rate")
            {
                for (int row = 0; row < dataTable.Rows.Count; row++)
                {
                    var cell = worksheet.Cells[row + 2, i + 1];
                    cell.Style.Numberformat.Format = "0.00\\%";

                    cell.Value = dataTable.Rows[row][i];

                }
            }
            else if (dataTable.Columns[i].ColumnName == "PriceSet")
            {
                for (int row = 0; row < dataTable.Rows.Count; row++)
                {
                    var cell = worksheet.Cells[row + 2, i + 1];
                    cell.Style.Numberformat.Format = "$0.00";
                    cell.Value = dataTable.Rows[row][i];
                }
            }
            else if (dataTable.Columns[i].ColumnName.EndsWith("ay"))
            {
                for (int row = 0; row < dataTable.Rows.Count; row++)
                {
                    var cell = worksheet.Cells[row + 2, i + 1];
                    cell.Style.Numberformat.Format = "#,##0.000";
                    cell.Value = dataTable.Rows[row][i];
                }
            }
            else
            {
                for (int row = 0; row < dataTable.Rows.Count; row++)
                {
                    worksheet.Cells[row + 2, i + 1].Value = dataTable.Rows[row][i];
                }
            }

            worksheet.Column(i + 1).Width = 30;
        }
    }
}