using System.Net.Http.Json;
using System.Text.Json;

namespace TKG.KenpinApp.Tests.E2ETests;

/// <summary>
/// シナリオ5: 中断・再開フロー（整理番号1004）
/// 1004: SINGLE, GQ41FJ(5), GQ52KL(2)
/// </summary>
[Collection("E2E")]
public class Scenario5_PauseResumeTests : E2ETestBase
{
    [Fact]
    public async Task S5_PauseAndResumeFlow()
    {
        // Step 1: 検品開始
        var (sessionId, startBody) = await StartInspection("1004", "2026-02-27", "SINGLE");
        Assert.True(sessionId > 0);
        var items = startBody.GetProperty("items");
        Assert.Equal(2, items.GetArrayLength());

        // Step 2: GQ41FJ を3回スキャン（途中）
        for (int i = 1; i <= 3; i++)
        {
            var (res, body) = await Scan(sessionId, "4975373000100");
            res.EnsureSuccessStatusCode();
            Assert.Equal("GQ41FJ", body.GetProperty("itemCode").GetString());
            Assert.Equal(i, body.GetProperty("newScannedQty").GetInt32());
        }

        // Step 3: 中断
        var pauseBody = await Pause(sessionId);
        Assert.True(pauseBody.GetProperty("success").GetBoolean());

        // Step 4: 再開（同じ整理番号で再度start）
        var (resumedSessionId, resumeBody) = await StartInspection("1004", "2026-02-27", "SINGLE");

        // Step 5: resumed確認 → SessionIdが同じことで再開を確認
        Assert.Equal(sessionId, resumedSessionId);

        // 検品数引継ぎ確認: GQ41FJの4回目のスキャンで4/5が返るはず
        var (scanRes, scanBody) = await Scan(resumedSessionId, "4975373000100");
        scanRes.EnsureSuccessStatusCode();
        Assert.Equal(4, scanBody.GetProperty("newScannedQty").GetInt32());
        Assert.Equal(1, scanBody.GetProperty("remainQty").GetInt32());

        // Step 6: 残りをスキャンして完了
        // GQ41FJ 残り1回
        var (_, scan5Body) = await Scan(resumedSessionId, "4975373000100");
        Assert.Equal(5, scan5Body.GetProperty("newScannedQty").GetInt32());
        Assert.True(scan5Body.GetProperty("isItemComplete").GetBoolean());

        // GQ52KL 2回
        await ScanMultiple(resumedSessionId, "4975373000201", 1);
        var (_, lastBody) = await Scan(resumedSessionId, "4975373000201");
        Assert.True(lastBody.GetProperty("isAllComplete").GetBoolean());

        // 検品完了
        var completeBody = await Complete(resumedSessionId);
        Assert.True(completeBody.GetProperty("success").GetBoolean());
    }
}
