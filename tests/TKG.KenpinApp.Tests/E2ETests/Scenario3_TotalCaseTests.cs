using System.Net.Http.Json;
using System.Text.Json;

namespace TKG.KenpinApp.Tests.E2ETests;

/// <summary>
/// シナリオ3: トータルケース完全フロー（整理番号1003）
/// </summary>
[Collection("E2E")]
public class Scenario3_TotalCaseTests : E2ETestBase
{
    [Fact]
    public async Task S3_FullTotalCaseFlow()
    {
        // Step 1: 検品開始（TOTAL_CASE）
        var (sessionId, startBody) = await StartInspection("1003", "2026-02-27", "TOTAL_CASE");
        Assert.True(sessionId > 0);
        var items = startBody.GetProperty("items");
        Assert.Equal(2, items.GetArrayLength());

        // Step 2: GQ41FJ（JAN: 4975373000100）
        // shipQty=30, uchibako_irisu=6 → 5回スキャン（6×5=30/30）
        for (int i = 1; i <= 5; i++)
        {
            var (res, body) = await Scan(sessionId, "4975373000100");
            res.EnsureSuccessStatusCode();
            Assert.Equal("GQ41FJ", body.GetProperty("itemCode").GetString());
            Assert.Equal(6, body.GetProperty("addedQty").GetInt32());
            Assert.Equal(6 * i, body.GetProperty("newScannedQty").GetInt32());
            Assert.Equal(30 - 6 * i, body.GetProperty("remainQty").GetInt32());
            if (i == 5)
            {
                Assert.True(body.GetProperty("isItemComplete").GetBoolean());
            }
        }

        // Step 3: GQ85RS（JAN: 4975373000504）
        // shipQty=24, uchibako_irisu=6 → 4回スキャン（6×4=24/24）
        for (int i = 1; i <= 4; i++)
        {
            var (res, body) = await Scan(sessionId, "4975373000504");
            res.EnsureSuccessStatusCode();
            Assert.Equal("GQ85RS", body.GetProperty("itemCode").GetString());
            Assert.Equal(6, body.GetProperty("addedQty").GetInt32());
            Assert.Equal(6 * i, body.GetProperty("newScannedQty").GetInt32());
            if (i == 4)
            {
                Assert.True(body.GetProperty("isItemComplete").GetBoolean());
            }
        }

        // Step 4: isAllComplete=true確認（最後のスキャンで確認済み）

        // Step 5: 検品完了
        var completeBody = await Complete(sessionId);
        Assert.True(completeBody.GetProperty("success").GetBoolean());
    }

    [Fact]
    public async Task S3_IsAllComplete_True_OnLastScan()
    {
        var (sessionId, _) = await StartInspection("1003", "2026-02-27", "TOTAL_CASE");

        // GQ41FJ 5回（6×5=30/30）
        await ScanMultiple(sessionId, "4975373000100", 5);

        // GQ85RS 3回（6×3=18/24）
        await ScanMultiple(sessionId, "4975373000504", 3);

        // 最後の1回でisAllComplete=true
        var (res, lastBody) = await Scan(sessionId, "4975373000504");
        res.EnsureSuccessStatusCode();
        Assert.True(lastBody.GetProperty("isAllComplete").GetBoolean());
    }
}
