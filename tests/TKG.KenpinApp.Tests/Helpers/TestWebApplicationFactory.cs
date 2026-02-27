using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using TKG.KenpinApp.Web.Data;
using TKG.KenpinApp.Web.MockD365;
using TKG.KenpinApp.Web.Services;

namespace TKG.KenpinApp.Tests.Helpers;

/// <summary>
/// テスト用WebApplicationFactory
/// SQLiteをインメモリDBに差し替え、D365モック失敗率を0%に設定
/// </summary>
public class TestWebApplicationFactory : WebApplicationFactory<Program>
{
    private readonly string _dbName = "TestDb_" + Guid.NewGuid().ToString("N");

    protected override void ConfigureWebHost(IWebHostBuilder builder)
    {
        builder.UseEnvironment("Development");

        var dbName = _dbName;

        builder.ConfigureServices(services =>
        {
            // 既存DbContextを削除し、インメモリDBに差し替え
            var dbDescriptor = services.SingleOrDefault(
                d => d.ServiceType == typeof(DbContextOptions<KenpinDbContext>));
            if (dbDescriptor != null) services.Remove(dbDescriptor);

            services.AddDbContext<KenpinDbContext>(options =>
                options.UseInMemoryDatabase(dbName));

            // バックグラウンドサービスを無効化（テスト安定性のため）
            var lockTimeout = services.SingleOrDefault(
                d => d.ImplementationType == typeof(LockTimeoutService));
            if (lockTimeout != null) services.Remove(lockTimeout);

            var d365Retry = services.SingleOrDefault(
                d => d.ImplementationType == typeof(D365SyncRetryService));
            if (d365Retry != null) services.Remove(d365Retry);
        });

        builder.UseSetting("MockD365:FailureRatePercent", "0");
    }

    /// <summary>
    /// テスト用のDBを初期化
    /// </summary>
    public void EnsureDbCreated()
    {
        using var scope = Services.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<KenpinDbContext>();
        db.Database.EnsureCreated();
    }

    /// <summary>
    /// テスト用のDBをリセット（全テーブルクリア）
    /// </summary>
    public void ResetDb()
    {
        using var scope = Services.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<KenpinDbContext>();
        db.Database.EnsureDeleted();
        db.Database.EnsureCreated();
    }
}
