using TKG.KenpinApp.Web.Models.Dto;

namespace TKG.KenpinApp.Web.Services;

/// <summary>
/// 検品サービスインターフェース
/// </summary>
public interface IInspectionService
{
    /// <summary>
    /// 検品セッション開始
    /// </summary>
    Task<InspectionStartResponse> StartAsync(InspectionStartRequest request, string userCode, string siteCode);

    /// <summary>
    /// 商品バーコードスキャン
    /// </summary>
    Task<ScanResponse> ScanAsync(ScanRequest request);

    /// <summary>
    /// スキャン取消
    /// </summary>
    Task<ScanCancelResponse> CancelScanAsync(ScanCancelRequest request);

    /// <summary>
    /// 検品中断
    /// </summary>
    Task<bool> PauseAsync(long sessionId);

    /// <summary>
    /// 検品完了
    /// </summary>
    Task<InspectionCompleteResponse> CompleteAsync(long sessionId);

    /// <summary>
    /// 伝票照合
    /// </summary>
    Task<SlipVerifyResponse> VerifySlipAsync(SlipVerifyRequest request);
}
