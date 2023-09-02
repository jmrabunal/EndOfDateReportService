using EndOfDateReportService.Services;
using Microsoft.AspNetCore.Mvc;

namespace EndOfDateReportService.Controllers;

[ApiController]
[Route("api/reports")]
public class ReportController:ControllerBase
{
    private readonly BranchService _branchService;
    public ReportController(BranchService branchService)
    {
        _branchService = branchService;
    }

    [HttpGet()]
    public async Task<IActionResult> GetReport(DateTime startDate, DateTime endDate)
    {
        var result = await _branchService.GenerateReport(startDate, endDate);
        return Ok(result);
    }
}

