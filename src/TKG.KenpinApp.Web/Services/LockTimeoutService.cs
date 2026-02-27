using Microsoft.EntityFrameworkCore;
using TKG.KenpinApp.Web.Data;
using TKG.KenpinApp.Web.Models;

namespace TKG.KenpinApp.Web.Services;

/// <summary>
/// 排他ロックタイムアウト監視バックグラウンドジョブ
/// 1分間隔でタイムアウトしたロックを自動解放する
/// </summary>
public class LockTimeoutService : BackgroundService
{
    private readonly IServiceProvider _serviceProvider;
    private readonly ILogger<LockTimeoutService> _logger;

    /// <summary>
    /// チェック間隔（1分）
    /// </summary>
    private static readonly TimeSpan CheckInterval = TimeSpan.FromMinutes(1);

    public LockTimeoutService(IServiceProvider serviceProvider, ILogger<LockTimeoutService> logger)
    {
        _serviceProvider = serviceProvider;
        _logger = logger;
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        _logger.LogInformation("ロックタイムアウト監視サービスを開始しました");

        while (!stoppingToken.IsCancellationRequested)
        {
            try
            {
                await CheckAndReleaseExpiredLocks();
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "ロックタイムアウト監視でエラーが発生しました");
            }

            await Task.Delay(CheckInterval, stoppingToken);
        }

        _logger.LogInformation("ロックタイムアウト監視サービスを停止しました");
    }

    /// <summary>
    /// タイムアウトしたロックを検出して自動解放する
    /// </summary>
    private async Task CheckAndReleaseExpiredLocks()
    {
        using var scope = _serviceProvider.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<KenpinDbContext>();
        var now = DateTime.Now;

        // タイムアウトしたロックを取得
        var expiredLocks = await db.KenpinLocks
            .Where(l => l.TimeoutAt < now && l.ReleasedAt == null)
            .ToListAsync();

        if (expiredLocks.Count == 0)
            return;

        foreach (var lk in expiredLocks)
        {
            // ロック解放
            lk.ReleasedAt = now;

            // 対応するSCANNINGセッションをPAUSEDに変更
            var session = await db.KenpinSessions
                .Where(s => s.SeiriNo == lk.SeiriNo
                            && s.ShukkaDate.Date == lk.ShukkaDate.Date
                            && s.Status == "SCANNING")
                .FirstOrDefaultAsync();

            if (session != null)
            {
                session.Status = "PAUSED";
                session.UpdatedAt = now;
            }

            // ログ記録
            db.AppLogs.Add(new AppLog
            {
                LogDatetime = now,
                UserCode = lk.LockedBy,
                ActionType = "LOCK_TIMEOUT",
                ScreenId = "SYSTEM",
                Detail = System.Text.Json.JsonSerializer.Serialize(new
                {
                    seiriNo = lk.SeiriNo,
                    lockedBy = lk.LockedBy,
                    lockedAt = lk.LockedAt,
                    timeoutAt = lk.TimeoutAt
                })
            });

            _logger.LogWarning(
                "ロックタイムアウト自動解放: seiriNo={SeiriNo}, lockedBy={LockedBy}, lockedAt={LockedAt}",
                lk.SeiriNo, lk.LockedBy, lk.LockedAt);
        }

        await db.SaveChangesAsync();
        _logger.LogInformation("タイムアウトロック {Count} 件を自動解放しました", expiredLocks.Count);
    }
}
