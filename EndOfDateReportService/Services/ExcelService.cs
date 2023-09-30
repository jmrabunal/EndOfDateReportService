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
    private Dictionary<string, Tuple<int, int>> suppliersToFromRows;
    private string commisionSalesSheet = "CommissionSales";
    private int PRICE_SET_COLUMN = 10;
    private int COMMISION_RATE_COLUMN = 9;

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

            GetSupliers(dataTable);
            CreateDraftWorkingOutputFromSheet(package);
            CreateSummaryFromDic(package);

            foreach (var supplier in suppliersToFromRows)
            {
                CreateSupplierSheet(supplier, package);
            }

            package.Save();
        }

    }

    private void CreateSupplierSheet(KeyValuePair<string, Tuple<int, int>> supplier, ExcelPackage package)
    {
        var supplierWorksheet = package.Workbook.Worksheets.Add(supplier.Key);
        var commisionSheet = package.Workbook.Worksheets.FirstOrDefault(x => x.Name == commisionSalesSheet);

        supplierWorksheet.Cells[1, 1].Value = "Supplier";
        supplierWorksheet.Cells[1, 2].Value = "PLU";
        supplierWorksheet.Cells[1, 3].Value = "Product";
        supplierWorksheet.Cells[1, 4].Value = "Rate";
        supplierWorksheet.Cells[1, 12].Value = "Total";
        supplierWorksheet.Cells[1, 20].Value = "Total Sales";
        supplierWorksheet.Cells[1, 21].Value = "Commission";
        supplierWorksheet.Cells[1, 22].Value = "Net";

        supplierWorksheet.Cells[1, 1].Style.Font.Bold = true;
        supplierWorksheet.Cells[1, 2].Style.Font.Bold = true;
        supplierWorksheet.Cells[1, 3].Style.Font.Bold = true;
        supplierWorksheet.Cells[1, 4].Style.Font.Bold = true;
        supplierWorksheet.Cells[1, 12].Style.Font.Bold = true;
        supplierWorksheet.Cells[1, 21].Style.Font.Bold = true;
        supplierWorksheet.Cells[1, 22].Style.Font.Bold = true;
        supplierWorksheet.Cells[1, 20].Style.Font.Bold = true;

        supplierWorksheet.DefaultColWidth = 14;

        //CommissionSalesSheet columns to take data from
        var branchNameColumn = 3;
        var productNameColumn = 8;
        var pluCodeColumn = 6;
        var commissionRateColumn = 9;
        int firstDayColumn = 11;
        int lastDayColumn = 17;


        var branchSupplierSheetColumn = 1;
        var PLUSupplierSheetColumn = 2;
        var ProductSupplierSheetColumn = 3;
        var RateSupplierSheetColumn = 4;
        var supplierAmountLastDayColumn = 11;
        var supplierTotalAmountColumn = 12;
        var supplierLastDaySales = 19;
        var supplierTotalSales = 20;
        var supplierCommissionSales = 21;
        var supplierNetSales = 22;

        int startRow = supplier.Value.Item1;
        int endRow = supplier.Value.Item2;
        

        int currentRow = 2;

        for (int row = startRow; row <= endRow; row++)
        {
            supplierWorksheet.Cells[currentRow, branchSupplierSheetColumn].Value = commisionSheet.Cells[row, branchNameColumn].Value;
            supplierWorksheet.Column(branchSupplierSheetColumn).Width = 30;

            supplierWorksheet.Cells[currentRow, PLUSupplierSheetColumn].Value = commisionSheet.Cells[row, pluCodeColumn].Value;

            supplierWorksheet.Cells[currentRow, ProductSupplierSheetColumn].Value = commisionSheet.Cells[row, productNameColumn].Value;
            supplierWorksheet.Column(ProductSupplierSheetColumn).Width = 30;

            var productRateCommissionSheetCell = commisionSheet.Cells[row, commissionRateColumn].Address;
            supplierWorksheet.Cells[currentRow, commissionRateColumn].Style.Numberformat.Format = "$0.00";

            supplierWorksheet.Cells[currentRow, RateSupplierSheetColumn].Formula = $"{commisionSalesSheet}!{productRateCommissionSheetCell}";

            var dayColumnCount = 5;
            var daySalesCount = 13;
            var totalAmountFormula = "";
            var totalFormula = "";
            var commissionFormula = "";
            var netFormula = "";

            for (int col = firstDayColumn; col <= lastDayColumn; col++)
            {
                var priceSetCell = commisionSheet.Cells[row, PRICE_SET_COLUMN].Address;

                var dayFormula = "";
                var cell = commisionSheet.Cells[row, col].Address;

                if (dayColumnCount <= supplierAmountLastDayColumn)
                {
                    if (commisionSheet.Cells[row, col].Value != null)
                    {
                        supplierWorksheet.Cells[currentRow,dayColumnCount].Formula = $"{commisionSheet}!{cell}";
                        supplierWorksheet.Cells[1, dayColumnCount].Value = commisionSheet.Cells[1, col].Value;
                        totalAmountFormula += $"{supplierWorksheet.Cells[currentRow, dayColumnCount].Address}+";
                        dayFormula += $"({commisionSalesSheet}!{cell}*{commisionSalesSheet}!{priceSetCell})+";
                        dayFormula = dayFormula.TrimEnd('+');

                        supplierWorksheet.Cells[1, daySalesCount].Value = supplierWorksheet.Cells[1, dayColumnCount].Value + " Sales";
                        supplierWorksheet.Cells[currentRow, daySalesCount].Formula = dayFormula;
                        supplierWorksheet.Cells[currentRow, daySalesCount].Style.Numberformat.Format = "$0.00";
                    }

                    dayColumnCount++;
                }
                else
                {
                    dayColumnCount = 5;
                }

                var commissionCell = commisionSheet.Cells[row, COMMISION_RATE_COLUMN].Address;
                totalFormula += $"({commisionSalesSheet}!{cell}*{commisionSalesSheet}!{priceSetCell})+";
                commissionFormula += $"(({commisionSalesSheet}!{cell}*{commisionSalesSheet}!{priceSetCell})*({commisionSalesSheet}!{commissionCell}/100))+";

                

                daySalesCount++;
            }
            totalAmountFormula = totalAmountFormula.TrimEnd('+');
            supplierWorksheet.Cells[currentRow, supplierTotalAmountColumn].Formula = totalAmountFormula;

            totalAmountFormula = "";

            totalFormula = totalFormula.TrimEnd('+');
            commissionFormula = commissionFormula.TrimEnd('+');
            netFormula = $"({supplierWorksheet.Cells[currentRow, supplierTotalSales].Address} - {supplierWorksheet.Cells[currentRow, supplierCommissionSales].Address})";
            supplierWorksheet.Cells[currentRow, supplierTotalSales].Formula = totalFormula;
            supplierWorksheet.Cells[currentRow, supplierCommissionSales].Formula = commissionFormula;
            supplierWorksheet.Cells[currentRow, supplierNetSales].Formula = netFormula;

            supplierWorksheet.Cells[currentRow, supplierTotalSales].Style.Numberformat.Format = "$0.00";
            supplierWorksheet.Cells[currentRow, supplierNetSales].Style.Numberformat.Format = "$0.00";
            supplierWorksheet.Cells[currentRow, supplierCommissionSales].Style.Numberformat.Format = "$0.00";

            currentRow++;
        }

        int grandTotalRow = currentRow + 1;
        supplierWorksheet.Cells[grandTotalRow, 1].Value = "Grand Total";
        supplierWorksheet.Cells[grandTotalRow, 1].Style.Font.Bold = true;

        string totalSumFormula = $"SUM({supplierWorksheet.Cells[2, supplierTotalSales].Address}:{supplierWorksheet.Cells[currentRow, supplierTotalSales].Address})";
        supplierWorksheet.Cells[grandTotalRow, supplierTotalSales].Formula = totalSumFormula;
        supplierWorksheet.Cells[grandTotalRow, supplierTotalSales].Style.Font.Bold = true;
        supplierWorksheet.Cells[currentRow, supplierTotalSales].Style.Numberformat.Format = "$0.00";


        string commissionSumFormula = $"SUM({supplierWorksheet.Cells[2, supplierCommissionSales].Address}:{supplierWorksheet.Cells[currentRow, supplierCommissionSales].Address})";
        supplierWorksheet.Cells[grandTotalRow, supplierCommissionSales].Formula = commissionSumFormula;
        supplierWorksheet.Cells[grandTotalRow, supplierCommissionSales].Style.Font.Bold = true;
        supplierWorksheet.Cells[currentRow, supplierCommissionSales].Style.Numberformat.Format = "$0.00";


        string netSumFormula = $"SUM({supplierWorksheet.Cells[2, supplierNetSales].Address}:{supplierWorksheet.Cells[currentRow, supplierNetSales].Address})";
        supplierWorksheet.Cells[grandTotalRow, supplierNetSales].Formula = netSumFormula;
        supplierWorksheet.Cells[grandTotalRow, supplierNetSales].Style.Font.Bold = true;
        supplierWorksheet.Cells[currentRow, supplierNetSales].Style.Numberformat.Format = "$0.00";

    }

    private void CreateSummaryFromDic(ExcelPackage package)
    {
        var summaryWorksheet = package.Workbook.Worksheets.Add("Summary");
        var commisionSheet = package.Workbook.Worksheets.FirstOrDefault(x => x.Name == commisionSalesSheet);

        if (commisionSheet?.Dimension != null)
        {
            summaryWorksheet.Cells[1, 1].Value = "Supplier";
            summaryWorksheet.Cells[1, 2].Value = "Total";
            summaryWorksheet.Cells[1, 3].Value = "Commission";
            summaryWorksheet.Cells[1, 4].Value = "Net";
            summaryWorksheet.Cells[1, 1].Style.Font.Bold = true;
            summaryWorksheet.Cells[1, 2].Style.Font.Bold = true;
            summaryWorksheet.Cells[1, 3].Style.Font.Bold = true;
            summaryWorksheet.Cells[1, 4].Style.Font.Bold = true;

            summaryWorksheet.Column(1).Width = 45;
            summaryWorksheet.Column(2).Width = 15;
            summaryWorksheet.Column(3).Width = 15;
            summaryWorksheet.Column(4).Width = 15;
            summaryWorksheet.Column(5).Width = 15;

            int currentRow = 2;
            int TotalColumn = 2;
            int CommissionColumn = 3;
            int NetColumn = 4;
            int firstDayColumn = 11;
            int lastDayColumn = 17;

            foreach (var supplier in suppliersToFromRows.Keys)
            {

                var rowRange = suppliersToFromRows[supplier];
                int startRow = rowRange.Item1;
                int endRow = rowRange.Item2;
                var totalFormula = "";
                var commissionFormula = "";
                var netFormula = "";

                summaryWorksheet.Cells[currentRow, 1].Value = supplier;
                for (int col = firstDayColumn; col <= lastDayColumn; col++)
                {
                    for (int row = startRow; row <= endRow; row++)
                    {

                        if (commisionSheet.Cells[row, col].Value != null)
                        {
                            var cell = commisionSheet.Cells[row, col].Address;
                            var priceSetCell = commisionSheet.Cells[row, PRICE_SET_COLUMN].Address;
                            var commissionCell = commisionSheet.Cells[row, COMMISION_RATE_COLUMN].Address;
                            totalFormula += $"({commisionSalesSheet}!{cell}*{commisionSalesSheet}!{priceSetCell})+";
                            commissionFormula += $"(({commisionSalesSheet}!{cell}*{commisionSalesSheet}!{priceSetCell})*({commisionSalesSheet}!{commissionCell}/100))+";
                        }
                     
                    }
                }
                totalFormula = totalFormula.TrimEnd('+');
                commissionFormula = commissionFormula.TrimEnd('+');
                netFormula = $"({summaryWorksheet.Cells[currentRow, TotalColumn].Address} - {summaryWorksheet.Cells[currentRow, CommissionColumn].Address})";
                summaryWorksheet.Cells[currentRow, TotalColumn].Formula = totalFormula;
                summaryWorksheet.Cells[currentRow, CommissionColumn].Formula = commissionFormula;
                summaryWorksheet.Cells[currentRow, NetColumn].Formula = netFormula;

                summaryWorksheet.Cells[currentRow, TotalColumn].Style.Numberformat.Format = "$0.00";
                summaryWorksheet.Cells[currentRow, CommissionColumn].Style.Numberformat.Format = "$0.00";
                summaryWorksheet.Cells[currentRow, NetColumn].Style.Numberformat.Format = "$0.00";


                currentRow++;
            }
            int grandTotalRow = currentRow + 1; 
            summaryWorksheet.Cells[grandTotalRow, 1].Value = "Grand Total";
            summaryWorksheet.Cells[grandTotalRow, 1].Style.Font.Bold = true;

            string totalSumFormula = $"SUM({summaryWorksheet.Cells[2, TotalColumn].Address}:{summaryWorksheet.Cells[currentRow, TotalColumn].Address})";
            summaryWorksheet.Cells[grandTotalRow, TotalColumn].Formula = totalSumFormula;
            summaryWorksheet.Cells[grandTotalRow, TotalColumn].Style.Font.Bold = true;
            summaryWorksheet.Cells[currentRow, TotalColumn].Style.Numberformat.Format = "$0.00";


            string commissionSumFormula = $"SUM({summaryWorksheet.Cells[2, CommissionColumn].Address}:{summaryWorksheet.Cells[currentRow, CommissionColumn].Address})";
            summaryWorksheet.Cells[grandTotalRow, CommissionColumn].Formula = commissionSumFormula;
            summaryWorksheet.Cells[grandTotalRow, CommissionColumn].Style.Font.Bold = true;
            summaryWorksheet.Cells[currentRow, CommissionColumn].Style.Numberformat.Format = "$0.00";


            string netSumFormula = $"SUM({summaryWorksheet.Cells[2, NetColumn].Address}:{summaryWorksheet.Cells[currentRow, NetColumn].Address})";
            summaryWorksheet.Cells[grandTotalRow, NetColumn].Formula = netSumFormula;
            summaryWorksheet.Cells[grandTotalRow, NetColumn].Style.Font.Bold = true;
            summaryWorksheet.Cells[currentRow, NetColumn].Style.Numberformat.Format = "$0.00";

        }
    }

    private void CreateDraftWorkingOutputFromSheet(ExcelPackage package)
    {
        var summaryWorksheet = package.Workbook.Worksheets.Add("Draft Working Output");
        var commisionSheet = package.Workbook.Worksheets.FirstOrDefault(x => x.Name == commisionSalesSheet);
        var supplierCount = suppliersToFromRows.Keys.Count();
        if (commisionSheet?.Dimension != null)
        {
            int currentRow = 2;
            int TotalColumn = 9;
            int CommissionColumn = 10;
            int NetColumn = 11;
            int firstDayColumn = 11;
            int lastDayColumn = 17;

            summaryWorksheet.Column(1).Width = 40;
            summaryWorksheet.Column(9).Width = 25;
            summaryWorksheet.Column(10).Width = 25;
            summaryWorksheet.Column(11).Width = 25;

            summaryWorksheet.Cells[1, 1].Value = "Supplier";
            summaryWorksheet.Cells[1, 9].Value = "Total";
            summaryWorksheet.Cells[1, 10].Value = "Commission";
            summaryWorksheet.Cells[1, 11].Value = "Net";

            summaryWorksheet.Cells[1, 9].Style.Font.Bold = true;
            summaryWorksheet.Cells[1, 10].Style.Font.Bold = true;
            summaryWorksheet.Cells[1, 11].Style.Font.Bold = true;
            summaryWorksheet.Cells[1, 1].Style.Font.Bold = true;

            foreach (var supplier in suppliersToFromRows.Keys)
            {
                var rowRange = suppliersToFromRows[supplier];
                int startRow = rowRange.Item1;
                int endRow = rowRange.Item2;
                var totalFormula = "";
                var commissionFormula = "";
                var netFormula = "";

                int dayColumnCount = 2;
                summaryWorksheet.Cells[currentRow, 1].Value = supplier;
                for (int col = firstDayColumn; col <= lastDayColumn; col++)
                {
                    var dayFormula = "";
                    var dayCellAddress = commisionSheet.Cells[1, col].Address;
                    summaryWorksheet.Cells[1, dayColumnCount].Formula = $"{commisionSalesSheet}!{dayCellAddress}";
                    summaryWorksheet.Column(dayColumnCount).Width = 25;
                    summaryWorksheet.Cells[1, dayColumnCount].Style.Font.Bold = true;

                    for (int row = startRow; row <= endRow; row++)
                    {

                        if (commisionSheet.Cells[row, col].Value != null)
                        {
                            var cell = commisionSheet.Cells[row, col].Address;
                            var priceSetCell = commisionSheet.Cells[row, PRICE_SET_COLUMN].Address;
                            var commissionCell = commisionSheet.Cells[row, COMMISION_RATE_COLUMN].Address;
                            totalFormula += $"({commisionSalesSheet}!{cell}*{commisionSalesSheet}!{priceSetCell})+";
                            commissionFormula += $"(({commisionSalesSheet}!{cell}*{commisionSalesSheet}!{priceSetCell})*({commisionSalesSheet}!{commissionCell}/100))+";


                            dayFormula += $"({commisionSalesSheet}!{cell}*{commisionSalesSheet}!{priceSetCell})+";
                        }

                    }
                    dayFormula = dayFormula.TrimEnd('+');
                    summaryWorksheet.Cells[currentRow, dayColumnCount].Formula = dayFormula;
                    summaryWorksheet.Cells[currentRow, dayColumnCount].Style.Numberformat.Format = "$0.00";

                    dayColumnCount++;
                }
                totalFormula = totalFormula.TrimEnd('+');
                commissionFormula = commissionFormula.TrimEnd('+');
                netFormula = $"({summaryWorksheet.Cells[currentRow, TotalColumn].Address} - {summaryWorksheet.Cells[currentRow, CommissionColumn].Address})";
                summaryWorksheet.Cells[currentRow, TotalColumn].Formula = totalFormula;
                summaryWorksheet.Cells[currentRow, CommissionColumn].Formula = commissionFormula;
                summaryWorksheet.Cells[currentRow, NetColumn].Formula = netFormula;

                summaryWorksheet.Cells[currentRow, CommissionColumn].Style.Numberformat.Format = "$0.00";
                summaryWorksheet.Cells[currentRow, NetColumn].Style.Numberformat.Format = "$0.00";
                summaryWorksheet.Cells[currentRow, TotalColumn].Style.Numberformat.Format = "$0.00";


                currentRow++;
            }
            int grandTotalRow = currentRow + 1;
            summaryWorksheet.Cells[grandTotalRow, 1].Value = "Grand Total";
            summaryWorksheet.Cells[grandTotalRow, 1].Style.Font.Bold = true;

            string totalSumFormula = $"SUM({summaryWorksheet.Cells[2, TotalColumn].Address}:{summaryWorksheet.Cells[currentRow, TotalColumn].Address})";
            summaryWorksheet.Cells[grandTotalRow, TotalColumn].Formula = totalSumFormula;
            summaryWorksheet.Cells[grandTotalRow, TotalColumn].Style.Font.Bold = true;
            summaryWorksheet.Cells[currentRow, TotalColumn].Style.Numberformat.Format = "$0.00";


            string commissionSumFormula = $"SUM({summaryWorksheet.Cells[2, CommissionColumn].Address}:{summaryWorksheet.Cells[currentRow, CommissionColumn].Address})";
            summaryWorksheet.Cells[grandTotalRow, CommissionColumn].Formula = commissionSumFormula;
            summaryWorksheet.Cells[grandTotalRow, CommissionColumn].Style.Font.Bold = true;
            summaryWorksheet.Cells[currentRow, CommissionColumn].Style.Numberformat.Format = "$0.00";


            string netSumFormula = $"SUM({summaryWorksheet.Cells[2, NetColumn].Address}:{summaryWorksheet.Cells[currentRow, NetColumn].Address})";
            summaryWorksheet.Cells[grandTotalRow, NetColumn].Formula = netSumFormula;
            summaryWorksheet.Cells[grandTotalRow, NetColumn].Style.Font.Bold = true;
            summaryWorksheet.Cells[currentRow, NetColumn].Style.Numberformat.Format = "$0.00";


        }
    }

    private void GetSupliers(DataTable dataTable)
    {
        suppliersToFromRows = new Dictionary<string, Tuple<int, int>>();
        string currentSupplier = null;
        int startRow = -1;

        for (int i = 0; i < dataTable.Columns.Count; i++)
        {
            if (dataTable.Columns[i].ColumnName == "LastName")
            {
                for (int row = 2; row < dataTable.Rows.Count; row++)
                {
                    string supplier = dataTable.Rows[row][i].ToString();

                    if (currentSupplier == null)
                    {
                        currentSupplier = supplier;
                        startRow = row; // Start on the next row (1-based index)
                    }
                    else if (supplier != currentSupplier)
                    {
                        // Add the data range for the previous supplier
                        suppliersToFromRows.Add(currentSupplier, Tuple.Create(startRow, row + 1));

                        currentSupplier = supplier;
                        startRow = row + 2; // Start on the next row (1-based index)
                    }

                    if (!suppliers.Contains(supplier))
                    {
                        suppliers.Add(supplier);
                    }
                }

                if (currentSupplier != null)
                {
                    // Add the data range for the last supplier
                    suppliersToFromRows.Add(currentSupplier, Tuple.Create(startRow, dataTable.Rows.Count - 1));
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
