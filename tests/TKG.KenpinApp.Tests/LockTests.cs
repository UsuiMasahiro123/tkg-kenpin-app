using System.Net;
using System.Net.Http.Json;
using System.Text.Json;
using TKG.KenpinApp.Tests.Helpers;
using TKG.KenpinApp.Web.Models.Dto;

namespace TKG.KenpinApp.Tests;

/// <summary>
/// 排他制御テスト (T24〜T27)
/// 各テストは独立したDBで実行される
/// </summary>
public class LockTests : IDisposable
{
    private readonly TestWebApplicationFactory _factory;
    private static readonly JsonSerializerOptions JsonOpts = new() { PropertyNameCaseInsensitive = true };

    public LockTests()
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

    /// <summary>
    /// T24: ロック取得 — 検品開始でロックレコードが作成される
    /// </summary>
    [Fact]
    public async Task T24_StartInspection_CreatesLock()
    {
        var (_, client) = await LoginAsync();

        var response = await client.PostAsJsonAsync("/api/inspection/start", new InspectionStartRequest
        {
            SeiriNo = "1001",
            ShukkaDate = "2026-02-27",
            KenpinType = "SINGLE"
        });

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        var body = await response.Content.ReadFromJsonAsync<InspectionStartResponse>(JsonOpts);
        Assert.NotNull(body);
        Assert.True(body.SessionId > 0);

        // ロック確認: 出荷指示詳細でIsLocked=trueであること
        var detailResponse = await client.GetAsync("/api/shipping/orders/1001");
        var detail = await detailResponse.Content.ReadFromJsonAsync<ShippingOrderDetailResponse>(JsonOpts);
        Assert.NotNull(detail);
        Assert.True(detail.IsLocked);
    }

    /// <summary>
    /// T25: 同じ整理番号の二重ロック → E-LOCK-001
    /// </summary>
    [Fact]
    public async Task T25_DuplicateLock_ReturnsConflict()
    {
        // ユーザー1で検品開始
        var (_, client1) = await LoginAsync("1055", "TOA-K");
        await client1.PostAsJsonAsync("/api/inspection/start", new InspectionStartRequest
        {
            SeiriNo = "1004",
            ShukkaDate = "2026-02-27",
            KenpinType = "SINGLE"
        });

        // ユーザー2で同じ整理番号の検品を開始（二重ロック）
        var (_, client2) = await LoginAsync("1102", "TOA-K");
        var response = await client2.PostAsJsonAsync("/api/inspection/start", new InspectionStartRequest
        {
            SeiriNo = "1004",
            ShukkaDate = "2026-02-27",
            KenpinType = "SINGLE"
        });

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
        var body = await response.Content.ReadAsStringAsync();
        Assert.Contains("E-LOCK-001", body);
    }

    /// <summary>
    /// T26: 検品完了後のロック解放 — ロックが解除される
    /// </summary>
    [Fact]
    public async Task T26_CompleteInspection_ReleasesLock()
    {
        var (_, client) = await LoginAsync();

        // 検品開始
        var startResponse = await client.PostAsJsonAsync("/api/inspection/start", new InspectionStartRequest
        {
            SeiriNo = "1001",
            ShukkaDate = "2026-02-27",
            KenpinType = "SINGLE"
        });
        var session = await startResponse.Content.ReadFromJsonAsync<InspectionStartResponse>(JsonOpts);
        Assert.NotNull(session);

        // スキャン
        await client.PostAsJsonAsync("/api/inspection/scan", new ScanRequest
        {
            SessionId = session.SessionId,
            Barcode = "4975373000100",
            ScanDatetime = DateTime.Now.ToString("o"),
            ScanMethod = "PC"
        });

        // 検品完了
        var completeResponse = await client.PutAsJsonAsync("/api/inspection/complete", new SessionActionRequest
        {
            SessionId = session.SessionId
        });
        Assert.Equal(HttpStatusCode.OK, completeResponse.StatusCode);

        // ロック解放確認
        var detailResponse = await client.GetAsync("/api/shipping/orders/1001");
        var detail = await detailResponse.Content.ReadFromJsonAsync<ShippingOrderDetailResponse>(JsonOpts);
        Assert.NotNull(detail);
        Assert.False(detail.IsLocked);
    }

    /// <summary>
    /// T27: ステータス不正遷移（COMPLETED→SCANNING）→ 拒否
    /// </summary>
    [Fact]
    public async Task T27_InvalidStatusTransition_Rejected()
    {
        var (_, client) = await LoginAsync();

        // 検品開始 → 完了
        var startResponse = await client.PostAsJsonAsync("/api/inspection/start", new InspectionStartRequest
        {
            SeiriNo = "1001",
            ShukkaDate = "2026-02-27",
            KenpinType = "SINGLE"
        });
        var session = await startResponse.Content.ReadFromJsonAsync<InspectionStartResponse>(JsonOpts);

        // スキャンして完了
        await client.PostAsJsonAsync("/api/inspection/scan", new ScanRequest
        {
            SessionId = session!.SessionId,
            Barcode = "4975373000100",
            ScanDatetime = DateTime.Now.ToString("o"),
            ScanMethod = "PC"
        });

        await client.PutAsJsonAsync("/api/inspection/complete", new SessionActionRequest
        {
            SessionId = session.SessionId
        });

        // 完了済みセッションを再度スキャン → エラー
        var scanResponse = await client.PostAsJsonAsync("/api/inspection/scan", new ScanRequest
        {
            SessionId = session.SessionId,
            Barcode = "4975373000100",
            ScanDatetime = DateTime.Now.ToString("o"),
            ScanMethod = "PC"
        });

        Assert.Equal(HttpStatusCode.BadRequest, scanResponse.StatusCode);
        var body = await scanResponse.Content.ReadAsStringAsync();
        Assert.Contains("E-KNP-005", body);
    }
}
