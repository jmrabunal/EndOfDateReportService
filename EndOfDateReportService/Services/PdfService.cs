using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Linq;
using EndOfDateReportService.DataAccess;
using EndOfDateReportService.Domain;
using iTextSharp.text;
using iTextSharp.text.pdf;
using iTextSharp.text.pdf.draw;
using Microsoft.Data.SqlClient;
using Microsoft.Extensions.Configuration;

namespace EndOfDateReportService.Services
{
    public class PdfService
    {
        private readonly ReportContext _context;
        private readonly IConfiguration _configuration;
        private readonly string connectionString;

        public PdfService(ReportContext context, IConfiguration configuration)
        {
            _context = context;
            connectionString = configuration.GetConnectionString("DefaultConnection");
            _configuration = configuration;
        }

        public async Task<byte[]> GenerateBranchPaymentMethodsPdf(Branch branch, DateTime date)
        {
            var path = _configuration.GetSection("reportPath");
            String currentDirectory = Directory.GetCurrentDirectory() + "\\" + path.Value;

            var dateFormatted = date.Date.ToString("yyyy-MM-dd").Replace("/", "-");

            string filename = $"SummaryReport - {branch.Name} - {dateFormatted}.pdf";

            string fullPath = Path.Combine(currentDirectory, filename);

            var document = new Document();
            PdfWriter.GetInstance(document, new FileStream(fullPath, FileMode.Create));

            document.Open();

            var titleFont = FontFactory.GetFont(FontFactory.HELVETICA_BOLD, 18);
            var title = new Paragraph($"Payment Methods for {branch.Name} - {dateFormatted}", titleFont);
            title.Alignment = Element.ALIGN_CENTER;
            document.Add(title);

            foreach (var lane in branch.Lanes)
            {
                var laneHeaderFont = FontFactory.GetFont(FontFactory.HELVETICA_BOLD, 14);
                var laneHeader = new Paragraph($"Lane {lane.LaneId}", laneHeaderFont);
                laneHeader.SpacingBefore = 10f;
                document.Add(laneHeader);

                var table = new PdfPTable(4);
                table.WidthPercentage = 100;
                table.SetWidths(new[] { 2f, 2f, 2f, 2f });

                AddTableHeaderRow(table);

                decimal laneTotalActualAmount = 0;
                decimal laneTotalReportedAmount = 0;
                decimal laneTotalVariance = 0;

                foreach (var paymentMethod in lane.PaymentMethods)
                {
                    AddPaymentMethodToTable(table, paymentMethod);

                    laneTotalActualAmount += paymentMethod.ActualAmount;
                    laneTotalReportedAmount += paymentMethod.ReportedAmount;
                    laneTotalVariance += paymentMethod.ReportedAmount - paymentMethod.ActualAmount;
                }

                AddTotalsRow(table, "Lane Totals", laneTotalActualAmount, laneTotalReportedAmount, laneTotalVariance);

                document.Add(table);
            }

            var summaryHeaderFont = FontFactory.GetFont(FontFactory.HELVETICA_BOLD, 14);
            var summaryHeader = new Paragraph("Summary", summaryHeaderFont);
            summaryHeader.SpacingBefore = 20f;
            document.Add(summaryHeader);

            var summaryTable = new PdfPTable(4);
            summaryTable.WidthPercentage = 100;
            summaryTable.SpacingBefore = 10f;
            summaryTable.SetWidths(new[] { 2f, 2f, 2f, 2f });

            AddTableHeaderRow(summaryTable);

            var paymentMethodTotals = new Dictionary<string, Tuple<decimal, decimal, decimal>>();

            foreach (var lane in branch.Lanes)
            {
                foreach (var paymentMethod in lane.PaymentMethods)
                {
                    if (!paymentMethodTotals.ContainsKey(paymentMethod.Name))
                    {
                        paymentMethodTotals[paymentMethod.Name] = new Tuple<decimal, decimal, decimal>(0, 0, 0);
                    }

                    var methodTotal = paymentMethodTotals[paymentMethod.Name];
                    methodTotal = new Tuple<decimal, decimal, decimal>(
                        methodTotal.Item1 + paymentMethod.ActualAmount,
                        methodTotal.Item2 + paymentMethod.ReportedAmount,
                        methodTotal.Item3 + paymentMethod.TotalVariance
                    );

                    paymentMethodTotals[paymentMethod.Name] = methodTotal;
                }
            }

            foreach (var methodTotal in paymentMethodTotals)
            {
                AddTotalsRow(summaryTable, methodTotal.Key, methodTotal.Value.Item1, methodTotal.Value.Item2, methodTotal.Value.Item3);
            }

            decimal grandTotalActualAmount = paymentMethodTotals.Values.Sum(t => t.Item1);
            decimal grandTotalReportedAmount = paymentMethodTotals.Values.Sum(t => t.Item2);
            decimal grandTotalVariance = paymentMethodTotals.Values.Sum(t => t.Item3);

            AddTotalsRow(summaryTable, "Grand Totals", grandTotalActualAmount, grandTotalReportedAmount, grandTotalVariance);

            document.Add(summaryTable);

            var otherReports = FontFactory.GetFont(FontFactory.HELVETICA_BOLD, 14);
            var otherReportsHeader = new Paragraph("Other Reports", summaryHeaderFont);
            otherReportsHeader.SpacingBefore = 20f;
            document.Add(otherReportsHeader);

            var gst = await ExecuteGSTQuery(date, branch.Id);
            var fee = await ExecuteFEEQuery(date, branch);

            string gstLine = $"GST                                              ${gst}";
            string feeLine = $"EFTPOS Fee                                ${fee}";

            BaseFont bf = BaseFont.CreateFont(BaseFont.HELVETICA, BaseFont.CP1252, BaseFont.NOT_EMBEDDED);
            Font font = new Font(bf, 12, Font.NORMAL);

            Paragraph paragraphGST = new Paragraph(new Chunk(gstLine, font));
            Paragraph paragraphEFTPOS = new Paragraph(new Chunk(feeLine, font));

            document.Add(paragraphGST);
            document.Add(paragraphEFTPOS);

            document.Close();

            if (!System.IO.File.Exists(fullPath))
            {
                return null;
            }

            return System.IO.File.ReadAllBytes(fullPath);
        }

        private void AddTableHeaderRow(PdfPTable table)
        {
            table.AddCell("Payment Method");
            table.AddCell("Actual Amount");
            table.AddCell("Reported Amount");
            table.AddCell("Total Variance");
        }

        private void AddPaymentMethodToTable(PdfPTable table, PaymentMethod paymentMethod)
        {
            var currencyFormat = new CultureInfo("en-US", false).NumberFormat;
            currencyFormat.CurrencySymbol = "$";

            table.AddCell(paymentMethod.Name);
            table.AddCell(paymentMethod.ActualAmount.ToString("C", currencyFormat));
            table.AddCell(paymentMethod.ReportedAmount.ToString("C", currencyFormat));
            table.AddCell(paymentMethod.TotalVariance.ToString("C", currencyFormat));
        }

        private void AddTotalsRow(PdfPTable table, string label, decimal actualAmount, decimal reportedAmount, decimal totalVariance)
        {
            var currencyFormat = new CultureInfo("en-US", false).NumberFormat;
            currencyFormat.CurrencySymbol = "$";

            table.AddCell(label);
            table.AddCell(actualAmount.ToString("C", currencyFormat));
            table.AddCell(reportedAmount.ToString("C", currencyFormat));
            table.AddCell(totalVariance.ToString("C", currencyFormat));
        }
        public async Task<double> ExecuteGSTQuery(DateTime date, int branchId)
        {
            var server = _configuration.GetConnectionString(branchId.ToString());
            var conex = string.Format(connectionString, server); 
            using (SqlConnection connection = new SqlConnection(conex))
            {
                await connection.OpenAsync();

                string sqlQuery = "select sum(TH.TotalAfterTax-TH.TotalBeforeTax) as GST from TransHeaders TH where cast(TH.Logged as Date) = '"+date+"' and TransType = 'C' and TransStatus = 'C' and th.Branch =" + branchId;
                SqlCommand command = new SqlCommand(sqlQuery, connection);
                try
                {
                    using (SqlDataReader reader = await command.ExecuteReaderAsync())
                    {
                        double gstTotal = 0.0;
                        while (await reader.ReadAsync())
                        {
                            double gstValue = Convert.ToDouble(reader["GST"]);
                            gstTotal = gstValue;
                        }
                        return gstTotal;
                    }
                } catch (Exception ex)
                {
                    var E = ex;
                    return 0.0;
                }
            }

        }

        public async Task<double> ExecuteFEEQuery(DateTime date, Branch branch)
        {
            var startDate = date.AddHours(00).AddMinutes(00).AddSeconds(00).ToString("yyyy-MM-ddTHH:mm:ss");
            var endDate = date.AddHours(23).AddMinutes(59).AddSeconds(59).ToString("yyyy-MM-ddTHH:mm:ss");
            var server = _configuration.GetConnectionString(branch.Id.ToString());
            var conex = string.Format(connectionString, server);
            double totalFee = 0;
            foreach (var lane in branch.Lanes)
            {
                using (SqlConnection connection = new SqlConnection(conex))
                {
                    await connection.OpenAsync();

                    string sqlQuery = "select Sum(TP.Fee) as Fee From [AKPOS].[dbo].[TransHeaders] as th LEFT JOIN AKPOS.dbo.TransPayments TP ON TH.TransNo = TP.TransNo AND TH.Branch = TP.Branch AND Th.Station = Tp.Station WHERE TH.Logged >= '" + startDate + "' AND TH.Logged <= '" + endDate + "' AND TH.Branch = " + branch.Id + " and TH.Station =" + lane.LaneId;
                    SqlCommand command = new SqlCommand(sqlQuery, connection);
                    try
                    {
                        using (SqlDataReader reader = await command.ExecuteReaderAsync())
                        {
                            while (await reader.ReadAsync())
                            {
                                double feeValue = Convert.ToDouble(reader["Fee"]);
                                totalFee += feeValue;
                            }
                        }
                    }catch (Exception ex)
                    {
                        var e = ex;
                    }
                }
            }
            return totalFee;
        }
    }
}
