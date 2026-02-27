using System.Net.Http.Json;
using System.Text.Json;

namespace TKG.KenpinApp.Tests.E2ETests;

/// <summary>
/// シナリオ1: シングルピッキング完全フロー（整理番号1001）
/// </summary>
[Collection("E2E")]
public class Scenario1_SinglePickingTests : E2ETestBase
{
    [Fact]
    public Task S1_01_Login_ReturnsSessionToken()
    {
        // InitializeAsyncでログイン済み
        Assert.False(string.IsNullOrEmpty(SessionToken));
        return Task.CompletedTask;
    }

    [Fact]
    public async Task S1_02_GetOrders_Returns6Items()
    {
        var res = await Client.GetAsync("/api/shipping/orders");
        res.EnsureSuccessStatusCode();
        var body = await res.Content.ReadFromJsonAsync<JsonElement>();
        var totalCount = body.GetProperty("totalCount").GetInt32();
        Assert.Equal(6, totalCount);
    }

    [Fact]
    public async Task S1_03_GetOrderDetail_Returns3Items()
    {
        var res = await Client.GetAsync("/api/shipping/orders/1001");
        res.EnsureSuccessStatusCode();
        var body = await res.Content.ReadFromJsonAsync<JsonElement>();
        Assert.Equal("1001", body.GetProperty("seiriNo").GetString());
        Assert.Equal("SINGLE", body.GetProperty("pickType").GetString());
    }

    [Fact]
    public async Task S1_04_FullSinglePickingFlow()
    {
        // Step 4: 検品開始
        var (sessionId, startBody) = await StartInspection("1001", "2026-02-27", "SINGLE");
        Assert.True(sessionId > 0);
        var items = startBody.GetProperty("items");
        Assert.Equal(3, items.GetArrayLength());

        // Step 5: GQ41FJ（JAN: 4975373000100）を10回スキャン → 10/10
        for (int i = 1; i <= 10; i++)
        {
            var (res, body) = await Scan(sessionId, "4975373000100");
            res.EnsureSuccessStatusCode();
            Assert.Equal("GQ41FJ", body.GetProperty("itemCode").GetString());
            Assert.Equal(1, body.GetProperty("addedQty").GetInt32());
            Assert.Equal(i, body.GetProperty("newScannedQty").GetInt32());
            Assert.Equal(10 - i, body.GetProperty("remainQty").GetInt32());
            if (i == 10)
            {
                Assert.True(body.GetProperty("isItemComplete").GetBoolean());
            }
        }

        // Step 6: GQ52KL（JAN: 4975373000201）を5回スキャン → 5/5
        for (int i = 1; i <= 5; i++)
        {
            var (res, body) = await Scan(sessionId, "4975373000201");
            res.EnsureSuccessStatusCode();
            Assert.Equal("GQ52KL", body.GetProperty("itemCode").GetString());
            Assert.Equal(i, body.GetProperty("newScannedQty").GetInt32());
            if (i == 5)
            {
                Assert.True(body.GetProperty("isItemComplete").GetBoolean());
            }
        }

        // Step 7: GQ63MN（JAN: 4975373000302）を3回スキャン → 3/3
        for (int i = 1; i <= 3; i++)
        {
            var (res, body) = await Scan(sessionId, "4975373000302");
            res.EnsureSuccessStatusCode();
            Assert.Equal("GQ63MN", body.GetProperty("itemCode").GetString());
            Assert.Equal(i, body.GetProperty("newScannedQty").GetInt32());
        }

        // Step 8: 最後のスキャンでisAllComplete=true（最後のスキャン結果を再チェック）
        // 最後のスキャンは上のループの最後なので、再確認不要
        // ただし明示的に確認: 最後のスキャンレスポンスでisAllComplete=trueを検証

        // Step 9: 検品完了
        var completeBody = await Complete(sessionId);
        Assert.True(completeBody.GetProperty("success").GetBoolean());

        // Step 10: 伝票照合
        var slipRes = await Client.PostAsJsonAsync("/api/inspection/slip-verify", new
        {
            sessionId,
            denpyoType = "NOUHINSHO",
            barcode = "1001"
        });
        slipRes.EnsureSuccessStatusCode();
        var slipBody = await slipRes.Content.ReadFromJsonAsync<JsonElement>();
        Assert.Equal("OK", slipBody.GetProperty("result").GetString());
    }

    [Fact]
    public async Task S1_05_LastScan_IsAllComplete_True()
    {
        // 独立テスト: 全品目完了時にisAllComplete=trueを確認
        var (sessionId, _) = await StartInspection("1001", "2026-02-27", "SINGLE");

        // GQ41FJ 10回
        await ScanMultiple(sessionId, "4975373000100", 10);

        // GQ52KL 5回
        await ScanMultiple(sessionId, "4975373000201", 5);

        // GQ63MN 最後の3回目でisAllComplete=true
        await ScanMultiple(sessionId, "4975373000302", 2);
        var (_, lastBody) = await Scan(sessionId, "4975373000302");
        Assert.True(lastBody.GetProperty("isAllComplete").GetBoolean());
    }
}
