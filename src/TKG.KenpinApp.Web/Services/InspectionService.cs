using Microsoft.EntityFrameworkCore;
using TKG.KenpinApp.Web.Constants;
using TKG.KenpinApp.Web.Data;
using TKG.KenpinApp.Web.Exceptions;
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

    /// <summary>
    /// 許可されるステータス遷移マップ
    /// </summary>
    private static readonly Dictionary<string, HashSet<string>> AllowedTransitions = new()
    {
        { "SCANNING", new HashSet<string> { "PAUSED", "COMPLETED", "CANCELLED" } },
        { "PAUSED", new HashSet<string> { "SCANNING" } },
        { "COMPLETED", new HashSet<string>() },   // 完了後は変更不可
        { "CANCELLED", new HashSet<string>() },    // キャンセル後は変更不可
    };

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
                        && l.ReleasedAt == null)
            .FirstOrDefaultAsync();

        if (existingLock != null)
        {
            if (existingLock.TimeoutAt < now)
            {
                // タイムアウト済み → 自動解放
                existingLock.ReleasedAt = now;

                // SCANNINGセッションもPAUSEDに
                var oldSession = await _db.KenpinSessions
                    .Where(s => s.SeiriNo == request.SeiriNo
                                && s.ShukkaDate.Date == shukkaDate.Date
                                && s.Status == "SCANNING")
                    .FirstOrDefaultAsync();
                if (oldSession != null)
                {
                    oldSession.Status = "PAUSED";
                    oldSession.UpdatedAt = now;
                }

                _logger.LogWarning("検品開始時にタイムアウトロックを自動解放: seiriNo={SeiriNo}, lockedBy={LockedBy}",
                    request.SeiriNo, existingLock.LockedBy);
            }
            else
            {
                // 他ユーザーが検品中 → ロック競合エラー
                throw new BusinessException(ErrorCodes.LOCK_CONFLICT,
                    $"他のユーザー({existingLock.LockedBy})が検品中です");
            }
        }

        // 新規ロック取得
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

        // PAUSEDセッションの再開チェック
        var pausedSession = await _db.KenpinSessions
            .Where(s => s.SeiriNo == request.SeiriNo
                        && s.ShukkaDate.Date == shukkaDate.Date
                        && s.Status == "PAUSED")
            .OrderByDescending(s => s.UpdatedAt)
            .FirstOrDefaultAsync();

        if (pausedSession != null)
        {
            // 既存セッションを再開（ステータス遷移チェック: PAUSED → SCANNING）
            ValidateTransition(pausedSession.Status, "SCANNING");
            pausedSession.Status = "SCANNING";
            pausedSession.UpdatedAt = now;

            // ログ記録
            _db.AppLogs.Add(new AppLog
            {
                LogDatetime = now,
                UserCode = userCode,
                ActionType = "INSPECTION_START",
                ScreenId = "KENPIN",
                Detail = System.Text.Json.JsonSerializer.Serialize(new
                {
                    action = "RESUME",
                    sessionId = pausedSession.SessionId,
                    seiriNo = request.SeiriNo,
                    kenpinType = request.KenpinType
                })
            });

            await _db.SaveChangesAsync();

            _logger.LogInformation(
                "検品セッション再開: sessionId={SessionId}, seiriNo={SeiriNo}",
                pausedSession.SessionId, request.SeiriNo);

            return new InspectionStartResponse
            {
                SessionId = pausedSession.SessionId,
                Items = items
            };
        }

        // 新規セッション作成
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

        // ログ記録
        _db.AppLogs.Add(new AppLog
        {
            LogDatetime = now,
            UserCode = userCode,
            ActionType = "INSPECTION_START",
            ScreenId = "KENPIN",
            Detail = System.Text.Json.JsonSerializer.Serialize(new
            {
                action = "NEW",
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
            ?? throw new BusinessException(ErrorCodes.KNP_SESSION_NOT_FOUND, "セッションが見つかりません");

        // ステータスチェック: SCANNINGのみスキャン可能
        if (session.Status != "SCANNING")
        {
            throw new BusinessException(ErrorCodes.KNP_INVALID_STATUS,
                $"検品中ではありません（現在のステータス: {session.Status}）");
        }

        // バーコードから品目情報を照合
        var itemInfo = await _d365.LookupBarcodeAsync(request.Barcode)
            ?? throw new BusinessException(ErrorCodes.KNP_INVALID_BARCODE,
                "バーコードに対応する商品が見つかりません");

        // 出荷指示明細から対象品目の出荷数量を取得
        var orderItems = await _d365.GetShippingOrderItemsAsync(session.SeiriNo);
        var targetItem = orderItems.FirstOrDefault(i => i.ItemCode == itemInfo.ItemCode)
            ?? throw new BusinessException(ErrorCodes.KNP_ITEM_NOT_FOUND,
                "この出荷指示に含まれない商品です");

        // 既存スキャン数量を集計
        var scannedQty = await _db.KenpinDetails
            .Where(d => d.SessionId == request.SessionId
                        && d.ItemCode == itemInfo.ItemCode
                        && !d.CancelFlg)
            .SumAsync(d => d.ScanQty);

        // 完了済み品目チェック
        if (scannedQty >= targetItem.ShipQty)
        {
            throw new BusinessException(ErrorCodes.KNP_ALREADY_DONE,
                $"この商品は検品完了済みです（{scannedQty}/{targetItem.ShipQty}）");
        }

        // スキャン時の加算数量を計算
        int addQty = session.KenpinType switch
        {
            "SINGLE" => 1,
            "TOTAL_BARA" => 1,
            "TOTAL_CASE" => targetItem.UchibakoIrisu,
            _ => throw new BusinessException(ErrorCodes.KNP_INVALID_STATUS, "不正な検品タイプです")
        };

        // 端数処理（トータルケースで残数 < 入数の場合）
        if (session.KenpinType == "TOTAL_CASE" && scannedQty + addQty > targetItem.ShipQty)
        {
            addQty = targetItem.ShipQty - scannedQty;
        }

        // 超過チェック
        if (scannedQty + addQty > targetItem.ShipQty)
        {
            throw new BusinessException(ErrorCodes.KNP_OVER_QTY,
                $"出荷数量を超過しています（スキャン: {scannedQty + addQty} > 出荷: {targetItem.ShipQty}）");
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

        // ログ記録
        _db.AppLogs.Add(new AppLog
        {
            LogDatetime = DateTime.Now,
            UserCode = session.UserCode,
            ActionType = "SCAN",
            ScreenId = "KENPIN",
            Detail = System.Text.Json.JsonSerializer.Serialize(new
            {
                sessionId = request.SessionId,
                seiriNo = session.SeiriNo,
                barcode = request.Barcode,
                itemCode = itemInfo.ItemCode,
                addQty,
                method = request.ScanMethod
            })
        });

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
            ?? throw new BusinessException(ErrorCodes.KNP_SESSION_NOT_FOUND, "スキャン明細が見つかりません");

        if (detail.SessionId != request.SessionId)
        {
            throw new BusinessException(ErrorCodes.KNP_INVALID_STATUS, "セッションIDが一致しません");
        }

        // セッションのステータスチェック
        var session = await _db.KenpinSessions.FindAsync(request.SessionId)
            ?? throw new BusinessException(ErrorCodes.KNP_SESSION_NOT_FOUND, "セッションが見つかりません");

        if (session.Status != "SCANNING")
        {
            throw new BusinessException(ErrorCodes.KNP_INVALID_STATUS,
                $"検品中ではないため取消できません（現在のステータス: {session.Status}）");
        }

        if (detail.CancelFlg)
        {
            throw new BusinessException(ErrorCodes.KNP_INVALID_STATUS, "既に取消済みです");
        }

        // 取消フラグを立てる
        detail.CancelFlg = true;
        detail.CancelDatetime = DateTime.Now;

        // セッションのスキャン数量を減算
        session.ScannedQty -= detail.ScanQty;
        session.UpdatedAt = DateTime.Now;

        // ログ記録
        _db.AppLogs.Add(new AppLog
        {
            LogDatetime = DateTime.Now,
            UserCode = session.UserCode,
            ActionType = "SCAN_CANCEL",
            ScreenId = "KENPIN",
            Detail = System.Text.Json.JsonSerializer.Serialize(new
            {
                sessionId = request.SessionId,
                seiriNo = session.SeiriNo,
                detailId = request.DetailId,
                itemCode = detail.ItemCode,
                cancelQty = detail.ScanQty
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
            "スキャン取消: detailId={DetailId}, itemCode={ItemCode}, cancelQty={CancelQty}",
            request.DetailId, detail.ItemCode, detail.ScanQty);

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
            ?? throw new BusinessException(ErrorCodes.KNP_SESSION_NOT_FOUND, "セッションが見つかりません");

        // ステータス遷移チェック: SCANNING → PAUSED のみ許可
        ValidateTransition(session.Status, "PAUSED");

        session.Status = "PAUSED";
        session.UpdatedAt = DateTime.Now;

        // ロック解放せず保持（timeout_atをリセット）
        var activeLock = await _db.KenpinLocks
            .Where(l => l.SeiriNo == session.SeiriNo
                        && l.ShukkaDate.Date == session.ShukkaDate.Date
                        && l.ReleasedAt == null)
            .FirstOrDefaultAsync();

        if (activeLock != null)
        {
            // 中断時はロック解放
            activeLock.ReleasedAt = DateTime.Now;
        }

        // ログ記録
        _db.AppLogs.Add(new AppLog
        {
            LogDatetime = DateTime.Now,
            UserCode = session.UserCode,
            ActionType = "INSPECTION_PAUSE",
            ScreenId = "KENPIN",
            Detail = System.Text.Json.JsonSerializer.Serialize(new
            {
                sessionId,
                seiriNo = session.SeiriNo,
                scannedQty = session.ScannedQty,
                totalQty = session.TotalQty
            })
        });

        await _db.SaveChangesAsync();

        _logger.LogInformation("検品中断: sessionId={SessionId}, scanned={Scanned}/{Total}",
            sessionId, session.ScannedQty, session.TotalQty);

        return true;
    }

    /// <inheritdoc/>
    public async Task<InspectionCompleteResponse> CompleteAsync(long sessionId)
    {
        var session = await _db.KenpinSessions.FindAsync(sessionId)
            ?? throw new BusinessException(ErrorCodes.KNP_SESSION_NOT_FOUND, "セッションが見つかりません");

        // ステータス遷移チェック: SCANNING → COMPLETED のみ許可
        ValidateTransition(session.Status, "COMPLETED");

        session.Status = "COMPLETED";
        session.CompletedAt = DateTime.Now;
        session.UpdatedAt = DateTime.Now;

        // ロック解放
        await ReleaseLockAsync(session.SeiriNo, session.ShukkaDate);

        // D365連携を試行
        var d365SyncStatus = "Synced";
        var payload = System.Text.Json.JsonSerializer.Serialize(new
        {
            sessionId,
            seiriNo = session.SeiriNo,
            totalScanned = session.ScannedQty,
            totalQty = session.TotalQty,
            completedAt = session.CompletedAt
        });

        try
        {
            await _d365.SyncInspectionResultAsync(sessionId, payload);
            session.D365Synced = true;
            _logger.LogInformation("D365連携成功: sessionId={SessionId}", sessionId);
        }
        catch (Exception ex)
        {
            // D365連携失敗 → リトライキューに登録
            session.D365Synced = false;
            d365SyncStatus = "Pending";

            _db.D365SyncQueues.Add(new D365SyncQueue
            {
                SessionId = sessionId,
                SyncType = "KENPIN_RESULT",
                Payload = payload,
                Status = "PENDING",
                NextRetryAt = DateTime.Now.AddSeconds(30)
            });

            _logger.LogWarning(
                "D365連携失敗。リトライキューに登録: sessionId={SessionId}, error={Error}",
                sessionId, ex.Message);
        }

        // ログ記録
        _db.AppLogs.Add(new AppLog
        {
            LogDatetime = DateTime.Now,
            UserCode = session.UserCode,
            ActionType = "INSPECTION_COMPLETE",
            ScreenId = "KENPIN",
            Detail = System.Text.Json.JsonSerializer.Serialize(new
            {
                sessionId,
                seiriNo = session.SeiriNo,
                totalScanned = session.ScannedQty,
                totalQty = session.TotalQty,
                d365Synced = session.D365Synced,
                d365SyncStatus
            })
        });

        await _db.SaveChangesAsync();

        _logger.LogInformation("検品完了: sessionId={SessionId}, scanned={Scanned}, d365={D365Status}",
            sessionId, session.ScannedQty, d365SyncStatus);

        return new InspectionCompleteResponse
        {
            Success = true,
            D365SyncStatus = d365SyncStatus
        };
    }

    /// <inheritdoc/>
    public async Task<SlipVerifyResponse> VerifySlipAsync(SlipVerifyRequest request)
    {
        var session = await _db.KenpinSessions.FindAsync(request.SessionId)
            ?? throw new BusinessException(ErrorCodes.KNP_SESSION_NOT_FOUND, "セッションが見つかりません");

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

        // ログ記録
        _db.AppLogs.Add(new AppLog
        {
            LogDatetime = DateTime.Now,
            UserCode = session.UserCode,
            ActionType = "SLIP_VERIFY",
            ScreenId = "KENPIN",
            Detail = System.Text.Json.JsonSerializer.Serialize(new
            {
                sessionId = request.SessionId,
                seiriNo = session.SeiriNo,
                denpyoType = request.DenpyoType,
                barcode = request.Barcode,
                result
            })
        });

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
    /// ステータス遷移の検証
    /// 不正な遷移の場合は BusinessException をスロー
    /// </summary>
    private void ValidateTransition(string currentStatus, string newStatus)
    {
        if (!AllowedTransitions.ContainsKey(currentStatus)
            || !AllowedTransitions[currentStatus].Contains(newStatus))
        {
            throw new BusinessException(ErrorCodes.KNP_INVALID_STATUS,
                $"ステータス遷移が不正です（{currentStatus} → {newStatus}）");
        }
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
