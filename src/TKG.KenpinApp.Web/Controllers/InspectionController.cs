using Microsoft.AspNetCore.Mvc;
using TKG.KenpinApp.Web.Models.Dto;
using TKG.KenpinApp.Web.Services;

namespace TKG.KenpinApp.Web.Controllers;

/// <summary>
/// 検品APIコントローラー
/// 例外はExceptionHandlingMiddlewareで一元処理されるため、
/// コントローラーではtry-catchを行わない
/// </summary>
[ApiController]
[Route("api/inspection")]
public class InspectionController : ControllerBase
{
    private readonly IInspectionService _inspectionService;
    private readonly ILogger<InspectionController> _logger;

    public InspectionController(
        IInspectionService inspectionService,
        ILogger<InspectionController> logger)
    {
        _inspectionService = inspectionService;
        _logger = logger;
    }

    /// <summary>
    /// API-5: 検品セッション開始
    /// </summary>
    [HttpPost("start")]
    public async Task<ActionResult<InspectionStartResponse>> Start([FromBody] InspectionStartRequest request)
    {
        // SessionValidationMiddlewareが格納したユーザー情報を取得
        var userCode = HttpContext.Items["UserCode"]?.ToString() ?? "dev-user";
        var siteCode = HttpContext.Items["SiteCode"]?.ToString() ?? "TOA-K";

        var result = await _inspectionService.StartAsync(request, userCode, siteCode);
        return Ok(result);
    }

    /// <summary>
    /// API-6: 商品バーコードスキャン
    /// </summary>
    [HttpPost("scan")]
    public async Task<ActionResult<ScanResponse>> Scan([FromBody] ScanRequest request)
    {
        var result = await _inspectionService.ScanAsync(request);
        return Ok(result);
    }

    /// <summary>
    /// API-7: スキャン取消
    /// </summary>
    [HttpDelete("scan")]
    public async Task<ActionResult<ScanCancelResponse>> CancelScan([FromBody] ScanCancelRequest request)
    {
        var result = await _inspectionService.CancelScanAsync(request);
        return Ok(result);
    }

    /// <summary>
    /// API-8: 検品中断
    /// </summary>
    [HttpPut("pause")]
    public async Task<ActionResult<SuccessResponse>> Pause([FromBody] SessionActionRequest request)
    {
        var result = await _inspectionService.PauseAsync(request.SessionId);
        return Ok(new SuccessResponse { Success = result });
    }

    /// <summary>
    /// API-9: 検品完了
    /// </summary>
    [HttpPut("complete")]
    public async Task<ActionResult<InspectionCompleteResponse>> Complete([FromBody] SessionActionRequest request)
    {
        var result = await _inspectionService.CompleteAsync(request.SessionId);
        return Ok(result);
    }

    /// <summary>
    /// API-10: 伝票照合
    /// </summary>
    [HttpPost("slip-verify")]
    public async Task<ActionResult<SlipVerifyResponse>> SlipVerify([FromBody] SlipVerifyRequest request)
    {
        var result = await _inspectionService.VerifySlipAsync(request);
        return Ok(result);
    }
}
