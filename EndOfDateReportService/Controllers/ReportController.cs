using AutoMapper;
using EndOfDateReportService.Domain;
using EndOfDateReportService.Models;
using EndOfDateReportService.Services;
using Microsoft.AspNetCore.Mvc;

namespace EndOfDateReportService.Controllers;

[ApiController]
[Route("api/reports")]
public class ReportController:ControllerBase
{
    private readonly BranchService _branchService;
    private readonly IMapper mapper;
    public ReportController(BranchService branchService)
    {
        _branchService = branchService;
    }

    [HttpGet()]
    public async Task<IActionResult> GetReport(DateTime startDate, DateTime endDate)
    {
        var result = await _branchService.GenerateReport(startDate, endDate);
        var mappedResult = mapper.Map<IEnumerable<BranchModelOut>>(result);
        return Ok(result);
    }

    [HttpPut()]
    public async Task<IActionResult> UpdateReport([FromBody] IEnumerable<Branch> branches)
    {
        await _branchService.UpdatePaymentMethods(branches);
        return Ok();
    }
}

