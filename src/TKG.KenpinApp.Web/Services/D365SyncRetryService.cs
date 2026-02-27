using Microsoft.EntityFrameworkCore;
using TKG.KenpinApp.Web.Data;
using TKG.KenpinApp.Web.Models;
using TKG.KenpinApp.Web.MockD365;

namespace TKG.KenpinApp.Web.Services;

/// <summary>
/// D365連携リトライバックグラウンドジョブ
/// 30秒間隔でPENDINGキューを処理し、指数バックオフでリトライする
/// </summary>
public class D365SyncRetryService : BackgroundService
{
    private readonly IServiceProvider _serviceProvider;
    private readonly ILogger<D365SyncRetryService> _logger;

    /// <summary>
    /// チェック間隔（30秒）
    /// </summary>
    private static readonly TimeSpan CheckInterval = TimeSpan.FromSeconds(30);

    /// <summary>
    /// 1回の処理で取得する最大件数
    /// </summary>
    private const int BatchSize = 10;

    public D365SyncRetryService(IServiceProvider serviceProvider, ILogger<D365SyncRetryService> logger)
    {
        _serviceProvider = serviceProvider;
        _logger = logger;
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        _logger.LogInformation("D365連携リトライサービスを開始しました");

        while (!stoppingToken.IsCancellationRequested)
        {
            try
            {
                await ProcessPendingItems();
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "D365連携リトライ処理でエラーが発生しました");
            }

            await Task.Delay(CheckInterval, stoppingToken);
        }

        _logger.LogInformation("D365連携リトライサービスを停止しました");
    }

    /// <summary>
    /// PENDINGキューを処理する
    /// </summary>
    private async Task ProcessPendingItems()
    {
        using var scope = _serviceProvider.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<KenpinDbContext>();
        var d365 = scope.ServiceProvider.GetRequiredService<ID365Service>();
        var now = DateTime.Now;

        // リトライ対象を取得
        var pendingItems = await db.D365SyncQueues
            .Where(q => q.Status == "PENDING"
                        && (q.NextRetryAt == null || q.NextRetryAt <= now)
                        && q.RetryCount < q.MaxRetries)
            .OrderBy(q => q.CreatedAt)
            .Take(BatchSize)
            .ToListAsync();

        if (pendingItems.Count == 0)
            return;

        _logger.LogInformation("D365連携リトライ対象: {Count} 件", pendingItems.Count);

        foreach (var item in pendingItems)
        {
            item.Status = "PROCESSING";
            await db.SaveChangesAsync();

            try
            {
                // D365 API呼出
                await d365.SyncInspectionResultAsync(item.SessionId, item.Payload);

                // 成功
                item.Status = "SUCCESS";
                item.CompletedAt = DateTime.Now;

                // セッションのd365_syncedフラグを更新
                var session = await db.KenpinSessions.FindAsync(item.SessionId);
                if (session != null)
                {
                    session.D365Synced = true;
                    session.UpdatedAt = DateTime.Now;
                }

                _logger.LogInformation(
                    "D365連携リトライ成功: queueId={QueueId}, sessionId={SessionId}",
                    item.QueueId, item.SessionId);
            }
            catch (Exception ex)
            {
                item.RetryCount++;
                item.LastError = ex.Message;

                if (item.RetryCount >= item.MaxRetries)
                {
                    // 最大リトライ回数超過 → FAILED
                    item.Status = "FAILED";

                    db.AppLogs.Add(new AppLog
                    {
                        LogDatetime = DateTime.Now,
                        ActionType = "D365_SYNC_FAILED",
                        ScreenId = "SYSTEM",
                        Detail = System.Text.Json.JsonSerializer.Serialize(new
                        {
                            queueId = item.QueueId,
                            sessionId = item.SessionId,
                            retryCount = item.RetryCount,
                            error = ex.Message
                        })
                    });

                    _logger.LogError(
                        "D365連携リトライ失敗（最大回数超過）: queueId={QueueId}, sessionId={SessionId}, error={Error}",
                        item.QueueId, item.SessionId, ex.Message);
                }
                else
                {
                    // リトライ待ち → PENDING（指数バックオフ: 30秒→60秒→120秒）
                    item.Status = "PENDING";
                    item.NextRetryAt = DateTime.Now.AddSeconds(30 * Math.Pow(2, item.RetryCount - 1));

                    _logger.LogWarning(
                        "D365連携リトライ待ち: queueId={QueueId}, retry={Retry}/{Max}, nextRetry={NextRetry}",
                        item.QueueId, item.RetryCount, item.MaxRetries, item.NextRetryAt);
                }
            }

            await db.SaveChangesAsync();
        }
    }
}
