using Microsoft.AspNetCore.Mvc;
using TKG.KenpinApp.Web.Models.Dto;
using TKG.KenpinApp.Web.Services;

namespace TKG.KenpinApp.Web.Controllers;

/// <summary>
/// 検品APIコントローラー
/// </summary>
[ApiController]
[Route("api/inspection")]
public class InspectionController : ControllerBase
{
    private readonly IInspectionService _inspectionService;
    private readonly IAuthService _authService;
    private readonly ILogger<InspectionController> _logger;

    public InspectionController(
        IInspectionService inspectionService,
        IAuthService authService,
        ILogger<InspectionController> logger)
    {
        _inspectionService = inspectionService;
        _authService = authService;
        _logger = logger;
    }

    /// <summary>
    /// API-5: 検品セッション開始
    /// </summary>
    [HttpPost("start")]
    public async Task<ActionResult<InspectionStartResponse>> Start([FromBody] InspectionStartRequest request)
    {
        try
        {
            // セッショントークンからユーザー情報を取得
            var sessionToken = GetSessionToken();
            var userSession = sessionToken != null
                ? await _authService.ValidateSessionAsync(sessionToken)
                : null;

            // セッションが無い場合はデフォルト値を使用（開発用）
            var userCode = userSession?.UserCode ?? "dev-user";
            var siteCode = userSession?.SiteCode ?? "TOA-K";

            var result = await _inspectionService.StartAsync(request, userCode, siteCode);
            return Ok(result);
        }
        catch (InvalidOperationException ex)
        {
            _logger.LogWarning("検品開始失敗: {Message}", ex.Message);
            return BadRequest(new { error = ex.Message });
        }
    }

    /// <summary>
    /// API-6: 商品バーコードスキャン
    /// </summary>
    [HttpPost("scan")]
    public async Task<ActionResult<ScanResponse>> Scan([FromBody] ScanRequest request)
    {
        try
        {
            var result = await _inspectionService.ScanAsync(request);
            return Ok(result);
        }
        catch (InvalidOperationException ex)
        {
            _logger.LogWarning("スキャン失敗: {Message}", ex.Message);
            return BadRequest(new { error = ex.Message });
        }
    }

    /// <summary>
    /// API-7: スキャン取消
    /// </summary>
    [HttpDelete("scan")]
    public async Task<ActionResult<ScanCancelResponse>> CancelScan([FromBody] ScanCancelRequest request)
    {
        try
        {
            var result = await _inspectionService.CancelScanAsync(request);
            return Ok(result);
        }
        catch (InvalidOperationException ex)
        {
            _logger.LogWarning("スキャン取消失敗: {Message}", ex.Message);
            return BadRequest(new { error = ex.Message });
        }
    }

    /// <summary>
    /// API-8: 検品中断
    /// </summary>
    [HttpPut("pause")]
    public async Task<ActionResult<SuccessResponse>> Pause([FromBody] SessionActionRequest request)
    {
        try
        {
            var result = await _inspectionService.PauseAsync(request.SessionId);
            return Ok(new SuccessResponse { Success = result });
        }
        catch (InvalidOperationException ex)
        {
            _logger.LogWarning("検品中断失敗: {Message}", ex.Message);
            return BadRequest(new { error = ex.Message });
        }
    }

    /// <summary>
    /// API-9: 検品完了
    /// </summary>
    [HttpPut("complete")]
    public async Task<ActionResult<InspectionCompleteResponse>> Complete([FromBody] SessionActionRequest request)
    {
        try
        {
            var result = await _inspectionService.CompleteAsync(request.SessionId);
            return Ok(result);
        }
        catch (InvalidOperationException ex)
        {
            _logger.LogWarning("検品完了失敗: {Message}", ex.Message);
            return BadRequest(new { error = ex.Message });
        }
    }

    /// <summary>
    /// API-10: 伝票照合
    /// </summary>
    [HttpPost("slip-verify")]
    public async Task<ActionResult<SlipVerifyResponse>> SlipVerify([FromBody] SlipVerifyRequest request)
    {
        try
        {
            var result = await _inspectionService.VerifySlipAsync(request);
            return Ok(result);
        }
        catch (InvalidOperationException ex)
        {
            _logger.LogWarning("伝票照合失敗: {Message}", ex.Message);
            return BadRequest(new { error = ex.Message });
        }
    }

    /// <summary>
    /// リクエストヘッダーからセッショントークンを取得
    /// </summary>
    private string? GetSessionToken()
    {
        if (Request.Headers.TryGetValue("X-Session-Token", out var token))
        {
            return token.ToString();
        }
        return null;
    }
}
