using System.Net.Http.Json;
using System.Text.Json;

namespace TKG.KenpinApp.Tests.E2ETests;

/// <summary>
/// シナリオ6: 取消テスト（整理番号1005）
/// 1005: TOTAL_BARA, GQ74PQ(12)
/// </summary>
[Collection("E2E")]
public class Scenario6_CancelTests : E2ETestBase
{
    [Fact]
    public async Task S6_CancelScanFlow()
    {
        // Step 1: 検品開始
        var (sessionId, startBody) = await StartInspection("1005", "2026-02-27", "TOTAL_BARA");
        Assert.True(sessionId > 0);

        // Step 2: GQ74PQ を2回スキャン
        long lastDetailId = 0;
        for (int i = 1; i <= 2; i++)
        {
            var (res, body) = await Scan(sessionId, "4975373000403");
            res.EnsureSuccessStatusCode();
            Assert.Equal(i, body.GetProperty("newScannedQty").GetInt32());
            lastDetailId = body.GetProperty("detailId").GetInt64();
        }

        // Step 3: 直前のスキャン取消（DELETE）
        var cancelReq = new HttpRequestMessage(HttpMethod.Delete, "/api/inspection/scan")
        {
            Content = JsonContent.Create(new
            {
                sessionId,
                detailId = lastDetailId
            })
        };
        var cancelRes = await Client.SendAsync(cancelReq);
        cancelRes.EnsureSuccessStatusCode();
        var cancelBody = await cancelRes.Content.ReadFromJsonAsync<JsonElement>();

        // Step 4: 検品数が-1された（2→1）ことを確認
        Assert.True(cancelBody.GetProperty("success").GetBoolean());
        Assert.Equal("GQ74PQ", cancelBody.GetProperty("undoneItemCode").GetString());
        Assert.Equal(1, cancelBody.GetProperty("newScannedQty").GetInt32());

        // Step 5: 残りをスキャンして完了（残り11回: 1→12）
        for (int i = 2; i <= 12; i++)
        {
            var (res, body) = await Scan(sessionId, "4975373000403");
            res.EnsureSuccessStatusCode();
            Assert.Equal(i, body.GetProperty("newScannedQty").GetInt32());
            if (i == 12)
            {
                Assert.True(body.GetProperty("isItemComplete").GetBoolean());
                Assert.True(body.GetProperty("isAllComplete").GetBoolean());
            }
        }

        // 検品完了
        var completeBody = await Complete(sessionId);
        Assert.True(completeBody.GetProperty("success").GetBoolean());
    }
}
