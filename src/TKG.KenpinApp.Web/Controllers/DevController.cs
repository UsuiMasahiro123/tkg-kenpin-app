using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using TKG.KenpinApp.Web.Data;
using TKG.KenpinApp.Web.MockD365;

namespace TKG.KenpinApp.Web.Controllers;

/// <summary>
/// 開発用APIコントローラー（Development環境でのみ有効）
/// </summary>
[ApiController]
[Route("api/dev")]
public class DevController : ControllerBase
{
    private readonly ID365Service _d365;
    private readonly KenpinDbContext _db;
    private readonly ILogger<DevController> _logger;

    public DevController(ID365Service d365, KenpinDbContext db, ILogger<DevController> logger)
    {
        _d365 = d365;
        _db = db;
        _logger = logger;
    }

    /// <summary>
    /// モックデータをJSONファイルから再読込（サーバー再起動不要）
    /// 同時にDBの検品セッション・ロックもクリアする
    /// </summary>
    [HttpPost("reload-mockdata")]
    public async Task<ActionResult> ReloadMockData()
    {
        // MockD365Serviceのデータ再読込
        if (_d365 is MockD365Service mockService)
        {
            mockService.ReloadData();
        }

        // DBクリーンアップ: 検品セッション・ロック・同期キューをクリア
        await _db.Database.ExecuteSqlRawAsync("DELETE FROM T_KENPIN_DETAIL");
        await _db.Database.ExecuteSqlRawAsync("DELETE FROM T_DENPYO_KENPIN");
        await _db.Database.ExecuteSqlRawAsync("DELETE FROM T_D365_SYNC_QUEUE");
        await _db.Database.ExecuteSqlRawAsync("DELETE FROM T_KENPIN_SESSION");
        await _db.Database.ExecuteSqlRawAsync("DELETE FROM T_KENPIN_LOCK");
        await _db.Database.ExecuteSqlRawAsync("DELETE FROM T_APP_LOG");

        _logger.LogInformation("モックデータ再読込 + DBクリーンアップ完了");

        return Ok(new { success = true, message = "モックデータ再読込・DBクリーンアップ完了" });
    }
}
