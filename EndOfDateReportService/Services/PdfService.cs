using EndOfDateReportService.DataAccess;
using EndOfDateReportService.Domain;
using iTextSharp.text;
using iTextSharp.text.pdf;

namespace EndOfDateReportService.Services;

public class PdfService
{
    private readonly ReportContext _reportContext;
    private readonly IConfiguration _configuration;

    public PdfService(ReportContext reportContext, IConfiguration configuration)
    {
        _reportContext = reportContext;
        _configuration = configuration;
    }

    public async Task GenerateBranchPaymentMethodsPdf(int branchId, string outputPath)
    {
        var branch = await _reportContext.Branches.FindAsync(branchId);

        if (branch == null)
        {
            throw new ArgumentException("Branch not found");
        }

        var document = new Document();
        PdfWriter.GetInstance(document, new FileStream(outputPath, FileMode.Create));

        document.Open();

        var titleFont = FontFactory.GetFont(FontFactory.HELVETICA_BOLD, 18);
        var title = new Paragraph($"Payment Methods for {branch.Name}", titleFont);
        title.Alignment = Element.ALIGN_CENTER;
        document.Add(title);

        var summaryPaymenMethods = await GetSummaryPaymentMethods();
        var summayPaymentMethodTotal = new PaymentMethod
        {
            Name = "Total",
            ActualAmount = 0,
            ReportedAmount = 0,
            TotalVariance = 0
        };
        
        foreach (var lane in branch.Lanes)
        {
            var laneHeaderFont = FontFactory.GetFont(FontFactory.HELVETICA_BOLD, 14);
            var laneHeader = new Paragraph($"Lane {lane.LaneId}", laneHeaderFont);
            laneHeader.SpacingBefore = 10f;
            document.Add(laneHeader);

            var lanePaymentMethodTotal = new PaymentMethod
            {
                Name = "Total",
                ActualAmount = 0,
                ReportedAmount = 0,
                TotalVariance = 0
            };

            foreach (var paymentMethod in lane.PaymentMethods)
            {

                var paymentMethodText = $"{paymentMethod.Name}: Actual Amount - {paymentMethod.ActualAmount:C}, " +
                                        $"Reported Amount - {paymentMethod.ReportedAmount:C}, " +
                                        $"Total Variance - {paymentMethod.TotalVariance:C}";

                var paymentMethodParagraph = new Paragraph(paymentMethodText);
                document.Add(paymentMethodParagraph);
                lanePaymentMethodTotal.ActualAmount += paymentMethod.ActualAmount;
                lanePaymentMethodTotal.ReportedAmount += paymentMethod.ReportedAmount;
                lanePaymentMethodTotal.TotalVariance += paymentMethod.TotalVariance;
                
                summayPaymentMethodTotal.ActualAmount += lanePaymentMethodTotal.ActualAmount;
                summayPaymentMethodTotal.ReportedAmount += lanePaymentMethodTotal.ReportedAmount;
                summayPaymentMethodTotal.TotalVariance += lanePaymentMethodTotal.TotalVariance;
                
                var pm = summaryPaymenMethods.FirstOrDefault(x => x.Name == paymentMethod.Name);
                pm.ActualAmount += paymentMethod.ActualAmount;
                pm.ReportedAmount += paymentMethod.ReportedAmount;
                pm.TotalVariance += paymentMethod.TotalVariance;
                
            }

            var paymentMethodTextLocal =
                $"{lanePaymentMethodTotal.Name}: Actual Amount - {lanePaymentMethodTotal.ActualAmount:C}, " +
                $"Reported Amount - {lanePaymentMethodTotal.ReportedAmount:C}, " +
                $"Total Variance - {lanePaymentMethodTotal.TotalVariance:C}";

            var paymentMethodTotalParagraph = new Paragraph(paymentMethodTextLocal);
            document.Add(paymentMethodTotalParagraph);
        }
        
        CreateSummaryPaymentMethods(document, summaryPaymenMethods);
        

        document.Close();
    }

    private void CreateSummaryPaymentMethods(Document document, IEnumerable<PaymentMethod> paymentMethods)
    {
        foreach (var pm in paymentMethods)
        {
            var paymentMethodTextLocal =
                $"{pm.Name}: Actual Amount - {pm.ActualAmount:C}, " +
                $"Reported Amount - {pm.ReportedAmount:C}, " +
                $"Total Variance - {pm.TotalVariance:C}";

            var paymentMethodTotalParagraph = new Paragraph(paymentMethodTextLocal);
            document.Add(paymentMethodTotalParagraph);        }
    }

    private async Task<IEnumerable<PaymentMethod>> GetSummaryPaymentMethods()
    {
        var paymentMethodsNames = _configuration.GetSection("PaymentMethods")
            .GetChildren()
            .Select(x => x.Value)
            .ToList();
        var paymentMethods = new List<PaymentMethod>();
        foreach (var pm in paymentMethodsNames)
        {

            paymentMethods.Add(new PaymentMethod
            {
                Name = pm,
                ActualAmount = 0,
                ReportedAmount = 0,
                TotalVariance = 0
            });
        }

        return paymentMethods;

    }


}