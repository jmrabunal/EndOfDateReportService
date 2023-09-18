using AutoMapper;
using EndOfDateReportService.Domain;
using EndOfDateReportService.Models.Out;

namespace EndOfDateReportService;

public class AutoMapper: Profile
{
    public AutoMapper()
    {
        CreateMap<Branch, BranchModelOut>();
        CreateMap<Lane, LaneModelOut>();
        CreateMap<PaymentMethod, PaymentMethodModelOut>();

    }
    
    
}