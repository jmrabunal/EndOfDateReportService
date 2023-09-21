using System.Data;
using Microsoft.Data.SqlClient;
using OfficeOpenXml;
using OfficeOpenXml.Style;

namespace EndOfDateReportService.Services;

public class ExcelService
{
    private readonly string connectionString;
    private readonly IConfiguration _configuration;

    public ExcelService(IConfiguration configuration)
    {
        connectionString = configuration.GetConnectionString("DefaultConnection");
        _configuration = configuration;

        ExcelPackage.LicenseContext = LicenseContext.NonCommercial;
    }

    private async Task<DataTable> ExecuteQuery(DateTime fromDateInclusive, DateTime toDateInclusive)
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
                        SELECT cast(TH.Logged as Date) as ''Date'', B.Name as ''Branch'', i.UPC,  I.SKU, I.Description, I.Supplier, C.LastName, I.Field_Integer as ''Commision Rate'', TL.PriceSet, 
                        SUM(CASE WHEN DATEPART(dw, TH.Logged) = 2 THEN TL.Quantity ELSE 0 END) AS MON_QTY,
                        SUM(CASE WHEN DATEPART(dw, TH.Logged) = 3 THEN TL.Quantity ELSE 0 END) AS TUE_QTY,
                        SUM(CASE WHEN DATEPART(dw, TH.Logged) = 4 THEN TL.Quantity ELSE 0 END) AS WED_QTY,
                        SUM(CASE WHEN DATEPART(dw, TH.Logged) = 5 THEN TL.Quantity ELSE 0 END) AS THU_QTY,
                        SUM(CASE WHEN DATEPART(dw, TH.Logged) = 6 THEN TL.Quantity ELSE 0 END) AS FRI_QTY,
                        SUM(CASE WHEN DATEPART(dw, TH.Logged) = 7 THEN TL.Quantity ELSE 0 END) AS SAT_QTY,
                        SUM(CASE WHEN DATEPART(dw, TH.Logged) = 1 THEN TL.Quantity ELSE 0 END) AS SUN_QTY
                        from translines as TL, Items as I, Branches B, Customers as C, TransHeaders as TH
                        where TL.UPC = I.UPC
                        and TL.Branch = B.id 
                        and I.Supplier = C.Code
                        and TL.Branch = TH.Branch
                        and TL.TransNo = TH.TransNo
                        and TL.Station = TH.Station
                        and I.Field_Integer > 0
                        group by cast(TH.Logged as Date), B.Name, i.UPC, I.SKU, I.Description, I.Supplier, C.LastName, I.Field_Integer, TL.PriceSet;
                    ');
                END;
            ";

            string createProcedureScript = @"
                -- CREATE PROCEDURE
                IF OBJECT_ID('dbo.sp_ListCommissionWithParams', 'P') IS NULL
                BEGIN
                    EXEC('
                        CREATE PROCEDURE dbo.sp_ListCommissionWithParams
                            @FromDateInclusive DATE,
                            @ToDateInclusive DATE
                        AS
                        BEGIN
                            SELECT @FromDateInclusive as [Period From], @ToDateInclusive as [Period To], Branch, Supplier, LastName, UPC, SKU, Description, [Commision Rate], PriceSet,
                                SUM(Mon_QTY) as ''Monday'', SUM(Tue_qty) as ''Tuesday'', SUM(WED_QTY) as ''Wednesday'', SUM(THU_QTY) as ''Thursday'', SUM(FRI_QTY) as ''Friday'', SUM(SAT_QTY) as ''Saturday'', SUM(SUN_QTY) as ''Sunday''
                            FROM vw_ListCommission
                            WHERE [Date] >= @FromDateInclusive
                            AND [Date] <= @ToDateInclusive
                            GROUP BY Branch, Supplier, LastName, UPC, SKU, Description, [Commision Rate], PriceSet
                            ORDER BY branch, Supplier, Description, [Commision Rate];
                        END;
                    ');
                END;
            ";

            using (SqlCommand command = new SqlCommand(createViewScript + createProcedureScript, connection))
            {
                await command.ExecuteNonQueryAsync();

                using (SqlCommand spCommand = new SqlCommand("dbo.sp_ListCommissionWithParams", connection))
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


    }

    public async Task ExportToExcel(DateTime fromDateInclusive, DateTime toDateInclusive)
    {
        var path = _configuration.GetSection("commisionSalesPath");
        string currentDirectory = Directory.GetCurrentDirectory() + "//" + path.Value;

        var dateFormatted = fromDateInclusive.Date.ToString("yyyy-MM-dd").Replace("/", "-");
        string filename = $"CommisionSales - {dateFormatted}.xlsm";
        string fullPath = Path.Combine(currentDirectory, filename);
        FileInfo fileInfo = new FileInfo(fullPath);

        if (fileInfo.Exists)
        {
            fileInfo.Delete();
        }

        var dataTable = await ExecuteQuery(fromDateInclusive, toDateInclusive);
        using (ExcelPackage package = new ExcelPackage(fileInfo))
        {
            ExcelWorksheet worksheet = package.Workbook.Worksheets.Add("CommissionSales");
            var brightGreen = System.Drawing.ColorTranslator.FromHtml("#AAFF17");

            int originalColumnCount = dataTable.Columns.Count;

            int columnIndex = 1;
            for (int i = 0; i < originalColumnCount; i++)
            {
                string columnName = dataTable.Columns[i].ColumnName;

                if (columnName.EndsWith("ay"))
                {
                    string dayTotalColumnName = columnName + " Total";
                    worksheet.Cells[1, columnIndex].Value = columnName;
                    worksheet.Cells[1, columnIndex + 1].Value = dayTotalColumnName;

                    for (int row = 0; row < dataTable.Rows.Count; row++)
                    {
                        decimal dayQty = Convert.ToDecimal(dataTable.Rows[row][columnName]);
                        decimal unitPrice = Convert.ToDecimal(dataTable.Rows[row]["PriceSet"]);
                        decimal dayTotal = dayQty * unitPrice;

                        worksheet.Cells[row + 2, columnIndex].Value = dayQty;
                        worksheet.Cells[row + 2, columnIndex + 1].Value = dayTotal;
                        worksheet.Cells[row + 2, columnIndex].Style.Numberformat.Format = "#,##0.00"; // Format as 7.00
                        worksheet.Cells[row + 2, columnIndex + 1].Style.Numberformat.Format = "$#,##0.00"; // Format as currency with two decimal places

                        // Set cell size
                        worksheet.Column(columnIndex).Width = 15;
                        worksheet.Column(columnIndex + 1).Width = 15;

                        // Set background color
                        worksheet.Cells[1, columnIndex, 1, columnIndex + 1].Style.Fill.PatternType = ExcelFillStyle.Solid;
                        worksheet.Cells[1, columnIndex, 1, columnIndex + 1].Style.Fill.BackgroundColor.SetColor(brightGreen);
                    }

                    columnIndex += 2; // Move to the next column for the next day total
                }
                else if (dataTable.Columns[i].DataType == typeof(DateTime))
                {
                    worksheet.Cells[1, columnIndex].Value = columnName;
                    worksheet.Cells[1, columnIndex].Style.Fill.PatternType = ExcelFillStyle.Solid;
                    worksheet.Cells[1, columnIndex].Style.Fill.BackgroundColor.SetColor(brightGreen);

                    for (int row = 0; row < dataTable.Rows.Count; row++)
                    {
                        worksheet.Cells[row + 2, columnIndex].Value = dataTable.Rows[row][i];
                    }

                    worksheet.Column(columnIndex).Width = 15;
                    worksheet.Column(columnIndex + 1).Width = 15;
                    worksheet.Column(columnIndex).Style.Numberformat.Format = "yyyy-MM-dd";
                    

                    columnIndex++;
                }
                else
                {
                    worksheet.Cells[1, columnIndex].Value = columnName;
                    worksheet.Cells[1, columnIndex].Style.Fill.PatternType = ExcelFillStyle.Solid;
                    worksheet.Cells[1, columnIndex].Style.Fill.BackgroundColor.SetColor(brightGreen);

                    if (columnName == "Commision Rate")
                    {
                        for (int row = 0; row < dataTable.Rows.Count; row++)
                        {
                            var cell = worksheet.Cells[row + 2, columnIndex];
                            cell.Style.Numberformat.Format = "0%"; // Format as percentage

                            if (decimal.TryParse(dataTable.Rows[row][i].ToString(), out decimal commissionRate))
                            {
                                cell.Value = commissionRate / 100;
                            }
                            else
                            {
                                cell.Value = dataTable.Rows[row][i];
                            }
                        }
                    }
                    else if (columnName == "PriceSet")
                    {
                        for (int row = 0; row < dataTable.Rows.Count; row++)
                        {
                            var cell = worksheet.Cells[row + 2, columnIndex];
                            cell.Style.Numberformat.Format = "$#,##0.00"; // Format as currency with two decimal places
                            cell.Value = dataTable.Rows[row][i];
                        }
                    }
                    else
                    {
                        for (int row = 0; row < dataTable.Rows.Count; row++)
                        {
                            worksheet.Cells[row + 2, columnIndex].Value = dataTable.Rows[row][i];
                        }
                    }

                    worksheet.Column(columnIndex).Width = 15;
                    columnIndex++;
                }
            }

            package.Save();
        }
    


}
}