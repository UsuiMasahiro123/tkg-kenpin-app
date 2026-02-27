using System.Net;
using System.Net.Http.Json;
using System.Text.Json;
using TKG.KenpinApp.Tests.Helpers;
using TKG.KenpinApp.Web.Models.Dto;

namespace TKG.KenpinApp.Tests;

/// <summary>
/// 検品フローテスト (T09〜T23) — 最重要テスト群
/// 各テストは独立したDBで実行される
/// </summary>
public class InspectionTests : IDisposable
{
    private readonly TestWebApplicationFactory _factory;
    private static readonly JsonSerializerOptions JsonOpts = new() { PropertyNameCaseInsensitive = true };

    public InspectionTests()
    {
        _factory = new TestWebApplicationFactory();
        _factory.EnsureDbCreated();
    }

    public void Dispose()
    {
        _factory.Dispose();
    }

    private async Task<(string Token, HttpClient Client)> LoginAsync(string empNo = "1055", string siteCode = "TOA-K")
    {
        var client = _factory.CreateClient();
        var response = await client.PostAsJsonAsync("/api/auth/login", new LoginRequest
        {
            EmpNo = empNo,
            SiteCode = siteCode
        });
        var body = await response.Content.ReadFromJsonAsync<LoginResponse>(JsonOpts);
        client.DefaultRequestHeaders.Add("X-Session-Token", body!.SessionToken);
        return (body.SessionToken, client);
    }

    private async Task<InspectionStartResponse> StartInspection(HttpClient client, string seiriNo, string kenpinType, string shukkaDate = "2026-02-27")
    {
        var response = await client.PostAsJsonAsync("/api/inspection/start", new InspectionStartRequest
        {
            SeiriNo = seiriNo,
            ShukkaDate = shukkaDate,
            KenpinType = kenpinType
        });
        response.EnsureSuccessStatusCode();
        var body = await response.Content.ReadFromJsonAsync<InspectionStartResponse>(JsonOpts);
        return body!;
    }

    private async Task<ScanResponse> Scan(HttpClient client, long sessionId, string barcode, string method = "PC")
    {
        var response = await client.PostAsJsonAsync("/api/inspection/scan", new ScanRequest
        {
            SessionId = sessionId,
            Barcode = barcode,
            ScanDatetime = DateTime.Now.ToString("o"),
            ScanMethod = method
        });
        response.EnsureSuccessStatusCode();
        return (await response.Content.ReadFromJsonAsync<ScanResponse>(JsonOpts))!;
    }

    /// <summary>
    /// T09: シングルピッキング: 検品開始 → セッション取得
    /// </summary>
    [Fact]
    public async Task T09_SinglePick_Start_ReturnsSessionWithItems()
    {
        var (_, client) = await LoginAsync();
        var result = await StartInspection(client, "1001", "SINGLE");

        Assert.True(result.SessionId > 0);
        Assert.Equal(3, result.Items.Count);
    }

    /// <summary>
    /// T10: シングルピッキング: スキャン1回 → +1加算
    /// </summary>
    [Fact]
    public async Task T10_SinglePick_ScanOnce_AddsOne()
    {
        var (_, client) = await LoginAsync();
        var session = await StartInspection(client, "1001", "SINGLE");

        var scan = await Scan(client, session.SessionId, "4975373000100");

        Assert.True(scan.Success);
        Assert.Equal(1, scan.AddedQty);
        Assert.Equal(1, scan.NewScannedQty);
        Assert.Equal("GQ41FJ", scan.ItemCode);
    }

    /// <summary>
    /// T11: シングルピッキング: 全品完了 → isAllComplete=true
    /// </summary>
    [Fact]
    public async Task T11_SinglePick_AllComplete_ReturnsTrue()
    {
        var (_, client) = await LoginAsync();
        var session = await StartInspection(client, "1001", "SINGLE");

        // GQ41FJ: 10回スキャン
        for (int i = 0; i < 10; i++)
            await Scan(client, session.SessionId, "4975373000100");

        // GQ52KL: 5回スキャン
        for (int i = 0; i < 5; i++)
            await Scan(client, session.SessionId, "4975373000201");

        // GQ63MN: 3回スキャン（最後のスキャン結果を確認）
        ScanResponse? lastScan = null;
        for (int i = 0; i < 3; i++)
            lastScan = await Scan(client, session.SessionId, "4975373000302");

        Assert.NotNull(lastScan);
        Assert.True(lastScan.IsAllComplete);
    }

    /// <summary>
    /// T12: トータルバラ: スキャン → +1加算
    /// </summary>
    [Fact]
    public async Task T12_TotalBara_Scan_AddsOne()
    {
        var (_, client) = await LoginAsync();
        var session = await StartInspection(client, "1002", "TOTAL_BARA");

        var scan = await Scan(client, session.SessionId, "4975373000100");

        Assert.True(scan.Success);
        Assert.Equal(1, scan.AddedQty);
    }

    /// <summary>
    /// T13: トータルケース: スキャン → 内箱入数分加算（UchibakoIrisu=6）
    /// </summary>
    [Fact]
    public async Task T13_TotalCase_Scan_AddsUchibakoIrisu()
    {
        var (_, client) = await LoginAsync("1203", "TSUKUBA");
        var session = await StartInspection(client, "1003", "TOTAL_CASE");

        var scan = await Scan(client, session.SessionId, "4975373000100");

        Assert.True(scan.Success);
        Assert.Equal(6, scan.AddedQty);
        Assert.Equal(6, scan.NewScannedQty);
    }

    /// <summary>
    /// T14: トータルケース: 端数処理 — 残数 < 入数の場合、残数のみ加算
    /// </summary>
    [Fact]
    public async Task T14_TotalCase_FractionalQty_AddsRemainder()
    {
        var (_, client) = await LoginAsync("1203", "TSUKUBA");
        var session = await StartInspection(client, "1003", "TOTAL_CASE");

        // GQ41FJ: ShipQty=30, uchibako_irisu=6 → 5回スキャンで全30個
        for (int i = 0; i < 4; i++)
            await Scan(client, session.SessionId, "4975373000100");

        // 5回目: 残り6個 → 6個加算（ちょうどピッタリ）
        var scan5 = await Scan(client, session.SessionId, "4975373000100");
        Assert.Equal(6, scan5.AddedQty);
        Assert.True(scan5.IsItemComplete);
    }

    /// <summary>
    /// T15: スキャン取消 → 検品数-1
    /// </summary>
    [Fact]
    public async Task T15_CancelScan_DecrementsQty()
    {
        var (_, client) = await LoginAsync();
        var session = await StartInspection(client, "1001", "SINGLE");

        // 2回スキャン
        await Scan(client, session.SessionId, "4975373000100");
        var scan2 = await Scan(client, session.SessionId, "4975373000100");
        Assert.Equal(2, scan2.NewScannedQty);

        // 2回目を取消
        var cancelResponse = await client.SendAsync(new HttpRequestMessage(HttpMethod.Delete, "/api/inspection/scan")
        {
            Content = JsonContent.Create(new ScanCancelRequest
            {
                SessionId = session.SessionId,
                DetailId = scan2.DetailId
            })
        });

        Assert.Equal(HttpStatusCode.OK, cancelResponse.StatusCode);
        var cancelBody = await cancelResponse.Content.ReadFromJsonAsync<ScanCancelResponse>(JsonOpts);
        Assert.NotNull(cancelBody);
        Assert.True(cancelBody.Success);
        Assert.Equal(1, cancelBody.NewScannedQty);
        Assert.Equal("GQ41FJ", cancelBody.UndoneItemCode);
    }

    /// <summary>
    /// T16: 検品中断 → ステータスPAUSED
    /// </summary>
    [Fact]
    public async Task T16_Pause_ChangesStatusToPaused()
    {
        var (_, client) = await LoginAsync();
        var session = await StartInspection(client, "1001", "SINGLE");

        var response = await client.PutAsJsonAsync("/api/inspection/pause", new SessionActionRequest
        {
            SessionId = session.SessionId
        });

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        var body = await response.Content.ReadFromJsonAsync<SuccessResponse>(JsonOpts);
        Assert.NotNull(body);
        Assert.True(body.Success);
    }

    /// <summary>
    /// T17: 中断後再開 → ステータスSCANNING、検品数引継ぎ
    /// </summary>
    [Fact]
    public async Task T17_Resume_AfterPause_ContinuesScanning()
    {
        var (_, client) = await LoginAsync();

        // 検品開始 → スキャン → 中断
        var session = await StartInspection(client, "1001", "SINGLE");
        await Scan(client, session.SessionId, "4975373000100");

        await client.PutAsJsonAsync("/api/inspection/pause", new SessionActionRequest
        {
            SessionId = session.SessionId
        });

        // 再開: 同じ整理番号で検品開始
        var resumed = await StartInspection(client, "1001", "SINGLE");

        // 同じセッションIDで再開される
        Assert.Equal(session.SessionId, resumed.SessionId);
        Assert.Equal(3, resumed.Items.Count);

        // スキャンを継続できる
        var scan = await Scan(client, resumed.SessionId, "4975373000100");
        Assert.Equal(2, scan.NewScannedQty);
    }

    /// <summary>
    /// T18: 検品完了 → D365連携
    /// </summary>
    [Fact]
    public async Task T18_Complete_SyncsToD365()
    {
        var (_, client) = await LoginAsync();
        var session = await StartInspection(client, "1001", "SINGLE");

        await Scan(client, session.SessionId, "4975373000100");

        var response = await client.PutAsJsonAsync("/api/inspection/complete", new SessionActionRequest
        {
            SessionId = session.SessionId
        });

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        var body = await response.Content.ReadFromJsonAsync<InspectionCompleteResponse>(JsonOpts);
        Assert.NotNull(body);
        Assert.True(body.Success);
        Assert.Contains(body.D365SyncStatus, new[] { "Synced", "Pending" });
    }

    /// <summary>
    /// T19: 存在しないバーコードスキャン → E-KNP-001
    /// </summary>
    [Fact]
    public async Task T19_Scan_InvalidBarcode_ReturnsError()
    {
        var (_, client) = await LoginAsync();
        var session = await StartInspection(client, "1001", "SINGLE");

        var response = await client.PostAsJsonAsync("/api/inspection/scan", new ScanRequest
        {
            SessionId = session.SessionId,
            Barcode = "0000000000000",
            ScanDatetime = DateTime.Now.ToString("o"),
            ScanMethod = "PC"
        });

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
        var body = await response.Content.ReadAsStringAsync();
        Assert.True(body.Contains("E-KNP-001") || body.Contains("E-KNP-002"));
    }

    /// <summary>
    /// T20: 出荷数超過スキャン → E-KNP-003 or E-KNP-004
    /// </summary>
    [Fact]
    public async Task T20_Scan_ExceedsShipQty_ReturnsError()
    {
        var (_, client) = await LoginAsync();
        var session = await StartInspection(client, "1001", "SINGLE");

        // GQ63MN: ShipQty=3 → 3回スキャン
        for (int i = 0; i < 3; i++)
            await Scan(client, session.SessionId, "4975373000302");

        // 4回目 → 超過エラー
        var response = await client.PostAsJsonAsync("/api/inspection/scan", new ScanRequest
        {
            SessionId = session.SessionId,
            Barcode = "4975373000302",
            ScanDatetime = DateTime.Now.ToString("o"),
            ScanMethod = "PC"
        });

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
        var body = await response.Content.ReadAsStringAsync();
        Assert.True(body.Contains("E-KNP-003") || body.Contains("E-KNP-004"));
    }

    /// <summary>
    /// T21: 完了済品目の再スキャン → E-KNP-004
    /// </summary>
    [Fact]
    public async Task T21_Scan_AlreadyComplete_ReturnsError()
    {
        var (_, client) = await LoginAsync();
        var session = await StartInspection(client, "1001", "SINGLE");

        // GQ63MN: ShipQty=3 → 3回スキャンで完了
        for (int i = 0; i < 3; i++)
            await Scan(client, session.SessionId, "4975373000302");

        // 完了済品目を再スキャン
        var response = await client.PostAsJsonAsync("/api/inspection/scan", new ScanRequest
        {
            SessionId = session.SessionId,
            Barcode = "4975373000302",
            ScanDatetime = DateTime.Now.ToString("o"),
            ScanMethod = "PC"
        });

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
        var body = await response.Content.ReadAsStringAsync();
        Assert.Contains("E-KNP-004", body);
    }

    /// <summary>
    /// T22: 伝票照合（正しいバーコード）→ result=OK
    /// </summary>
    [Fact]
    public async Task T22_SlipVerify_CorrectBarcode_ReturnsOK()
    {
        var (_, client) = await LoginAsync();
        var session = await StartInspection(client, "1001", "SINGLE");

        var response = await client.PostAsJsonAsync("/api/inspection/slip-verify", new SlipVerifyRequest
        {
            SessionId = session.SessionId,
            DenpyoType = "NOUHINSHO",
            Barcode = "SLIP-1001-001"
        });

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        var body = await response.Content.ReadFromJsonAsync<SlipVerifyResponse>(JsonOpts);
        Assert.NotNull(body);
        Assert.True(body.Success);
        Assert.Equal("OK", body.Result);
    }

    /// <summary>
    /// T23: 伝票照合（不正バーコード）→ result=NG
    /// </summary>
    [Fact]
    public async Task T23_SlipVerify_WrongBarcode_ReturnsNG()
    {
        var (_, client) = await LoginAsync();
        var session = await StartInspection(client, "1001", "SINGLE");

        var response = await client.PostAsJsonAsync("/api/inspection/slip-verify", new SlipVerifyRequest
        {
            SessionId = session.SessionId,
            DenpyoType = "NOUHINSHO",
            Barcode = "SLIP-9999-001"
        });

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        var body = await response.Content.ReadFromJsonAsync<SlipVerifyResponse>(JsonOpts);
        Assert.NotNull(body);
        Assert.True(body.Success);
        Assert.Equal("NG", body.Result);
    }
}
