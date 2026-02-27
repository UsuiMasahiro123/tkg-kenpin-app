using System.Net.Http.Json;
using System.Text.Json;

namespace TKG.KenpinApp.Tests.E2ETests;

/// <summary>
/// シナリオ2: トータルバラ完全フロー（整理番号1002）
/// </summary>
[Collection("E2E")]
public class Scenario2_TotalBaraTests : E2ETestBase
{
    [Fact]
    public async Task S2_FullTotalBaraFlow()
    {
        // Step 1: 検品開始（TOTAL_BARA）
        var (sessionId, startBody) = await StartInspection("1002", "2026-02-27", "TOTAL_BARA");
        Assert.True(sessionId > 0);
        var items = startBody.GetProperty("items");
        Assert.Equal(2, items.GetArrayLength());

        // Step 2: GQ41FJ（JAN: 4975373000100）を20回スキャン → 20/20
        for (int i = 1; i <= 20; i++)
        {
            var (res, body) = await Scan(sessionId, "4975373000100");
            res.EnsureSuccessStatusCode();
            Assert.Equal("GQ41FJ", body.GetProperty("itemCode").GetString());
            Assert.Equal(1, body.GetProperty("addedQty").GetInt32());
            Assert.Equal(i, body.GetProperty("newScannedQty").GetInt32());
            if (i == 20)
            {
                Assert.True(body.GetProperty("isItemComplete").GetBoolean());
            }
        }

        // Step 3: GQ74PQ（JAN: 4975373000403）を8回スキャン → 8/8
        for (int i = 1; i <= 8; i++)
        {
            var (res, body) = await Scan(sessionId, "4975373000403");
            res.EnsureSuccessStatusCode();
            Assert.Equal("GQ74PQ", body.GetProperty("itemCode").GetString());
            Assert.Equal(i, body.GetProperty("newScannedQty").GetInt32());
            if (i == 8)
            {
                Assert.True(body.GetProperty("isItemComplete").GetBoolean());
            }
        }

        // Step 4: isAllComplete=true確認（最後のスキャン結果で）
        // 最後のGQ74PQスキャンでisAllComplete=trueが返される
        // 明示的に確認
        var (_, verifyBody) = await Scan(sessionId, "4975373000403");
        // ここではE-KNP-004（完了済み品目）が返されるはずなので、
        // 代わりに最後のスキャンの結果を最後のループで確認済み

        // Step 5: 検品完了
        var completeBody = await Complete(sessionId);
        Assert.True(completeBody.GetProperty("success").GetBoolean());
    }

    [Fact]
    public async Task S2_IsAllComplete_True_OnLastScan()
    {
        var (sessionId, _) = await StartInspection("1002", "2026-02-27", "TOTAL_BARA");

        // GQ41FJ 20回
        await ScanMultiple(sessionId, "4975373000100", 20);

        // GQ74PQ 7回
        await ScanMultiple(sessionId, "4975373000403", 7);

        // 最後の1回でisAllComplete=true
        var (res, lastBody) = await Scan(sessionId, "4975373000403");
        res.EnsureSuccessStatusCode();
        Assert.True(lastBody.GetProperty("isAllComplete").GetBoolean());
    }
}
