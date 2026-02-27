using TKG.KenpinApp.Web.Data;
using TKG.KenpinApp.Web.Models.Dto;
using TKG.KenpinApp.Web.MockD365;

namespace TKG.KenpinApp.Web.Services;

/// <summary>
/// 検品サービス実装
/// </summary>
public class InspectionService : IInspectionService
{
    private readonly KenpinDbContext _db;
    private readonly ID365Service _d365;
    private readonly ILogger<InspectionService> _logger;

    public InspectionService(KenpinDbContext db, ID365Service d365, ILogger<InspectionService> logger)
    {
        _db = db;
        _d365 = d365;
        _logger = logger;
    }

    // タスク6-2で本実装
    public Task<InspectionStartResponse> StartAsync(InspectionStartRequest request, string userCode, string siteCode) => throw new NotImplementedException();
    public Task<ScanResponse> ScanAsync(ScanRequest request) => throw new NotImplementedException();
    public Task<ScanCancelResponse> CancelScanAsync(ScanCancelRequest request) => throw new NotImplementedException();
    public Task<bool> PauseAsync(long sessionId) => throw new NotImplementedException();
    public Task<InspectionCompleteResponse> CompleteAsync(long sessionId) => throw new NotImplementedException();
    public Task<SlipVerifyResponse> VerifySlipAsync(SlipVerifyRequest request) => throw new NotImplementedException();
}
