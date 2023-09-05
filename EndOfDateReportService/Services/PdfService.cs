using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Linq;
using EndOfDateReportService.DataAccess;
using EndOfDateReportService.Domain;
using iTextSharp.text;
using iTextSharp.text.pdf;

namespace EndOfDateReportService.Services
{
    public class PdfService
    {
        private readonly ReportContext _context;

        public PdfService(ReportContext context)
        {
            _context = context;
        }

        public async Task GenerateBranchPaymentMethodsPdf(Branch branch)
        {
            String currentDirectory = Directory.GetCurrentDirectory();

            string filename = "SummaryReport.pdf";

            string fullPath = Path.Combine(currentDirectory, filename);

            var document = new Document();
            PdfWriter.GetInstance(document, new FileStream(fullPath, FileMode.Create));

            document.Open();

            var titleFont = FontFactory.GetFont(FontFactory.HELVETICA_BOLD, 18);
            var title = new Paragraph($"Payment Methods for {branch.Name}", titleFont);
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
                    laneTotalVariance += paymentMethod.TotalVariance;
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
            document.Close();
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
    }
}
