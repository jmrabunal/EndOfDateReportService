using AutoMapper;
using EndOfDateReportService.Domain;
using EndOfDateReportService.Models;
using EndOfDateReportService.Services;
using Microsoft.AspNetCore.Cors;
using Microsoft.AspNetCore.Mvc;

namespace EndOfDateReportService.Controllers;
[ApiController]
[Route("api/reports")]
public class ReportController:ControllerBase
{
    private readonly BranchService _branchService;
    private readonly IMapper _mapper;
    public ReportController(BranchService branchService, IMapper mapper)
    {
        _branchService = branchService;
        _mapper = mapper;
    }

    [HttpGet()]
    public async Task<IActionResult> GetReport(DateTime startDate, DateTime endDate)
    {
        var result = await _branchService.GenerateReport(startDate, endDate);
        var mappedResult = _mapper.Map<IEnumerable<BranchModelOut>>(result);
        return Ok(mappedResult);
    }

    [HttpPut()]
    public async Task<IActionResult> UpdateReport([FromBody] IEnumerable<Branch> branches)
    {
        await _branchService.UpdatePaymentMethods(branches);
        return Ok();
    }

    [HttpPost()]
    public IActionResult CreateSummary([FromQuery] DateTime date, [FromQuery] int branchId)
    {
        _branchService.PdfGenerator(date, branchId);
        return Ok();
    }
}

