using Microsoft.EntityFrameworkCore;
using TKG.KenpinApp.Web.Data;
using TKG.KenpinApp.Web.Models;
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

    /// <summary>
    /// ロックタイムアウト（30分）
    /// </summary>
    private const int LockTimeoutMinutes = 30;

    public InspectionService(KenpinDbContext db, ID365Service d365, ILogger<InspectionService> logger)
    {
        _db = db;
        _d365 = d365;
        _logger = logger;
    }

    /// <inheritdoc/>
    public async Task<InspectionStartResponse> StartAsync(InspectionStartRequest request, string userCode, string siteCode)
    {
        var now = DateTime.Now;
        var shukkaDate = DateTime.Parse(request.ShukkaDate);

        // 排他ロック確認・取得
        var existingLock = await _db.KenpinLocks
            .Where(l => l.SeiriNo == request.SeiriNo
                        && l.ShukkaDate.Date == shukkaDate.Date
                        && l.ReleasedAt == null
                        && l.TimeoutAt > now)
            .FirstOrDefaultAsync();

        if (existingLock != null)
        {
            throw new InvalidOperationException($"他のユーザー({existingLock.LockedBy})が検品中です");
        }

        // T_KENPIN_LOCK INSERT
        var kenpinLock = new KenpinLock
        {
            SeiriNo = request.SeiriNo,
            ShukkaDate = shukkaDate,
            LockedBy = userCode,
            LockedAt = now,
            TimeoutAt = now.AddMinutes(LockTimeoutMinutes)
        };
        _db.KenpinLocks.Add(kenpinLock);

        // D365モックから出荷指示明細を取得
        var items = await _d365.GetShippingOrderItemsAsync(request.SeiriNo);
        var order = await _d365.GetShippingOrderAsync(request.SeiriNo);

        // T_KENPIN_SESSION INSERT
        var session = new KenpinSession
        {
            SeiriNo = request.SeiriNo,
            ShukkaDate = shukkaDate,
            KenpinType = request.KenpinType,
            UserCode = userCode,
            SiteCode = siteCode,
            WarehouseCode = order?.WarehouseCode,
            TotalItems = items.Count,
            TotalQty = items.Sum(i => i.ShipQty),
            ScannedQty = 0,
            Status = "SCANNING",
            StartedAt = now
        };
        _db.KenpinSessions.Add(session);

        // T_APP_LOG INSERT
        _db.AppLogs.Add(new AppLog
        {
            LogDatetime = now,
            UserCode = userCode,
            ActionType = "SCAN",
            ScreenId = "KENPIN",
            Detail = System.Text.Json.JsonSerializer.Serialize(new
            {
                action = "START",
                seiriNo = request.SeiriNo,
                kenpinType = request.KenpinType
            })
        });

        await _db.SaveChangesAsync();

        _logger.LogInformation(
            "検品セッション開始: sessionId={SessionId}, seiriNo={SeiriNo}, type={KenpinType}",
            session.SessionId, request.SeiriNo, request.KenpinType);

        return new InspectionStartResponse
        {
            SessionId = session.SessionId,
            Items = items
        };
    }

    /// <inheritdoc/>
    public async Task<ScanResponse> ScanAsync(ScanRequest request)
    {
        // セッション取得
        var session = await _db.KenpinSessions.FindAsync(request.SessionId)
            ?? throw new InvalidOperationException("セッションが見つかりません");

        if (session.Status != "SCANNING")
        {
            throw new InvalidOperationException("検品中ではありません");
        }

        // バーコードから品目情報を照合
        var itemInfo = await _d365.LookupBarcodeAsync(request.Barcode)
            ?? throw new InvalidOperationException("バーコードに対応する商品が見つかりません");

        // 出荷指示明細から対象品目の出荷数量を取得
        var orderItems = await _d365.GetShippingOrderItemsAsync(session.SeiriNo);
        var targetItem = orderItems.FirstOrDefault(i => i.ItemCode == itemInfo.ItemCode)
            ?? throw new InvalidOperationException("この出荷指示に含まれない商品です");

        // 既存スキャン数量を集計
        var scannedQty = await _db.KenpinDetails
            .Where(d => d.SessionId == request.SessionId
                        && d.ItemCode == itemInfo.ItemCode
                        && !d.CancelFlg)
            .SumAsync(d => d.ScanQty);

        // スキャン時の加算数量を計算
        int addQty = session.KenpinType switch
        {
            "SINGLE" => 1,
            "TOTAL_BARA" => 1,
            "TOTAL_CASE" => itemInfo.UchibakoIrisu,
            _ => throw new InvalidOperationException("不正な検品タイプです")
        };

        // 端数処理（トータルケースで残数 < 入数の場合）
        if (session.KenpinType == "TOTAL_CASE" && scannedQty + addQty > targetItem.ShipQty)
        {
            addQty = targetItem.ShipQty - scannedQty;
        }

        // 超過チェック
        if (scannedQty + addQty > targetItem.ShipQty)
        {
            throw new InvalidOperationException("出荷数量を超過しています（E-KNP-003）");
        }

        var scanDatetime = DateTime.TryParse(request.ScanDatetime, out var parsed) ? parsed : DateTime.Now;

        // T_KENPIN_DETAIL INSERT
        var detail = new KenpinDetail
        {
            SessionId = request.SessionId,
            ItemCode = itemInfo.ItemCode,
            Barcode = request.Barcode,
            KenpinCategory = itemInfo.KenpinCategory,
            ScanQty = addQty,
            ScanDatetime = scanDatetime,
            ScanMethod = request.ScanMethod
        };
        _db.KenpinDetails.Add(detail);

        // T_KENPIN_SESSION UPDATE（スキャン数量加算）
        session.ScannedQty += addQty;
        session.UpdatedAt = DateTime.Now;

        await _db.SaveChangesAsync();

        var newScannedQty = scannedQty + addQty;
        var remainQty = targetItem.ShipQty - newScannedQty;
        var isItemComplete = remainQty <= 0;

        // 全品目完了チェック
        var isAllComplete = await CheckAllItemsComplete(request.SessionId, session.SeiriNo);

        _logger.LogInformation(
            "スキャン成功: sessionId={SessionId}, itemCode={ItemCode}, addQty={AddQty}, remain={RemainQty}",
            request.SessionId, itemInfo.ItemCode, addQty, remainQty);

        return new ScanResponse
        {
            Success = true,
            DetailId = detail.DetailId,
            ItemCode = itemInfo.ItemCode,
            AddedQty = addQty,
            NewScannedQty = newScannedQty,
            RemainQty = remainQty,
            IsItemComplete = isItemComplete,
            IsAllComplete = isAllComplete
        };
    }

    /// <inheritdoc/>
    public async Task<ScanCancelResponse> CancelScanAsync(ScanCancelRequest request)
    {
        var detail = await _db.KenpinDetails.FindAsync(request.DetailId)
            ?? throw new InvalidOperationException("スキャン明細が見つかりません");

        if (detail.SessionId != request.SessionId)
        {
            throw new InvalidOperationException("セッションIDが一致しません");
        }

        if (detail.CancelFlg)
        {
            throw new InvalidOperationException("既に取消済みです");
        }

        // 取消フラグを立てる
        detail.CancelFlg = true;
        detail.CancelDatetime = DateTime.Now;

        // セッションのスキャン数量を減算
        var session = await _db.KenpinSessions.FindAsync(request.SessionId)!;
        session!.ScannedQty -= detail.ScanQty;
        session.UpdatedAt = DateTime.Now;

        // T_APP_LOG INSERT
        _db.AppLogs.Add(new AppLog
        {
            LogDatetime = DateTime.Now,
            UserCode = session.UserCode,
            ActionType = "CANCEL",
            ScreenId = "KENPIN",
            Detail = System.Text.Json.JsonSerializer.Serialize(new
            {
                detailId = request.DetailId,
                itemCode = detail.ItemCode
            })
        });

        await _db.SaveChangesAsync();

        // 当該品目の最新スキャン数量を計算
        var newScannedQty = await _db.KenpinDetails
            .Where(d => d.SessionId == request.SessionId
                        && d.ItemCode == detail.ItemCode
                        && !d.CancelFlg)
            .SumAsync(d => d.ScanQty);

        _logger.LogInformation(
            "スキャン取消: detailId={DetailId}, itemCode={ItemCode}",
            request.DetailId, detail.ItemCode);

        return new ScanCancelResponse
        {
            Success = true,
            UndoneItemCode = detail.ItemCode,
            NewScannedQty = newScannedQty
        };
    }

    /// <inheritdoc/>
    public async Task<bool> PauseAsync(long sessionId)
    {
        var session = await _db.KenpinSessions.FindAsync(sessionId)
            ?? throw new InvalidOperationException("セッションが見つかりません");

        session.Status = "PAUSED";
        session.UpdatedAt = DateTime.Now;

        // ロック解放
        await ReleaseLockAsync(session.SeiriNo, session.ShukkaDate);

        _db.AppLogs.Add(new AppLog
        {
            LogDatetime = DateTime.Now,
            UserCode = session.UserCode,
            ActionType = "SCAN",
            ScreenId = "KENPIN",
            Detail = System.Text.Json.JsonSerializer.Serialize(new
            {
                action = "PAUSE",
                sessionId
            })
        });

        await _db.SaveChangesAsync();

        _logger.LogInformation("検品中断: sessionId={SessionId}", sessionId);

        return true;
    }

    /// <inheritdoc/>
    public async Task<InspectionCompleteResponse> CompleteAsync(long sessionId)
    {
        var session = await _db.KenpinSessions.FindAsync(sessionId)
            ?? throw new InvalidOperationException("セッションが見つかりません");

        session.Status = "COMPLETED";
        session.CompletedAt = DateTime.Now;
        session.UpdatedAt = DateTime.Now;
        session.D365Synced = true; // モックでは即座に同期済みとする

        // ロック解放
        await ReleaseLockAsync(session.SeiriNo, session.ShukkaDate);

        _db.AppLogs.Add(new AppLog
        {
            LogDatetime = DateTime.Now,
            UserCode = session.UserCode,
            ActionType = "COMPLETE",
            ScreenId = "KENPIN",
            Detail = System.Text.Json.JsonSerializer.Serialize(new
            {
                sessionId,
                totalScanned = session.ScannedQty
            })
        });

        await _db.SaveChangesAsync();

        _logger.LogInformation("検品完了: sessionId={SessionId}", sessionId);

        return new InspectionCompleteResponse
        {
            Success = true,
            D365SyncStatus = "Synced"
        };
    }

    /// <inheritdoc/>
    public async Task<SlipVerifyResponse> VerifySlipAsync(SlipVerifyRequest request)
    {
        var session = await _db.KenpinSessions.FindAsync(request.SessionId)
            ?? throw new InvalidOperationException("セッションが見つかりません");

        // モック: 整理番号がバーコードに含まれていればOK
        var result = request.Barcode.Contains(session.SeiriNo) ? "OK" : "NG";

        // T_DENPYO_KENPIN INSERT
        var denpyo = new DenpyoKenpin
        {
            SessionId = request.SessionId,
            DenpyoType = request.DenpyoType,
            Barcode = request.Barcode,
            VerifiedAt = DateTime.Now,
            Result = result
        };
        _db.DenpyoKenpins.Add(denpyo);

        await _db.SaveChangesAsync();

        _logger.LogInformation(
            "伝票照合: sessionId={SessionId}, type={DenpyoType}, result={Result}",
            request.SessionId, request.DenpyoType, result);

        return new SlipVerifyResponse
        {
            Success = true,
            Result = result
        };
    }

    /// <summary>
    /// 全品目のスキャン完了チェック
    /// </summary>
    private async Task<bool> CheckAllItemsComplete(long sessionId, string seiriNo)
    {
        var orderItems = await _d365.GetShippingOrderItemsAsync(seiriNo);

        foreach (var item in orderItems)
        {
            var scanned = await _db.KenpinDetails
                .Where(d => d.SessionId == sessionId
                            && d.ItemCode == item.ItemCode
                            && !d.CancelFlg)
                .SumAsync(d => d.ScanQty);

            if (scanned < item.ShipQty)
                return false;
        }

        return true;
    }

    /// <summary>
    /// 排他ロック解放
    /// </summary>
    private async Task ReleaseLockAsync(string seiriNo, DateTime shukkaDate)
    {
        var locks = await _db.KenpinLocks
            .Where(l => l.SeiriNo == seiriNo
                        && l.ShukkaDate.Date == shukkaDate.Date
                        && l.ReleasedAt == null)
            .ToListAsync();

        foreach (var l in locks)
        {
            l.ReleasedAt = DateTime.Now;
        }
    }
}
