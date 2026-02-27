using System.Net;
using System.Net.Http.Json;
using System.Text.Json;

namespace TKG.KenpinApp.Tests.E2ETests;

/// <summary>
/// シナリオ4: エラー系テスト
/// </summary>
[Collection("E2E")]
public class Scenario4_ErrorTests : E2ETestBase
{
    [Fact]
    public async Task S4_01_UnknownBarcode_ReturnsError()
    {
        // 検品開始
        var (sessionId, _) = await StartInspection("1001", "2026-02-27", "SINGLE");

        // 存在しないバーコードでスキャン → E-KNP-001 or E-KNP-002
        var res = await Client.PostAsJsonAsync("/api/inspection/scan", new
        {
            sessionId,
            barcode = "9999999999999",
            scanDatetime = DateTime.Now.ToString("yyyy-MM-ddTHH:mm:ss"),
            scanMethod = "PC"
        });

        Assert.Equal(HttpStatusCode.BadRequest, res.StatusCode);
        var body = await res.Content.ReadFromJsonAsync<JsonElement>();
        var errorCode = body.GetProperty("error").GetString();
        Assert.Contains(errorCode, new[] { "E-KNP-001", "E-KNP-002" });
    }

    [Fact]
    public async Task S4_02_OverQuantity_ReturnsError()
    {
        // 検品開始（1001: SINGLE, GQ41FJ=10個）
        var (sessionId, _) = await StartInspection("1001", "2026-02-27", "SINGLE");

        // GQ41FJを10回スキャン → 10/10完了
        await ScanMultiple(sessionId, "4975373000100", 10);

        // 11回目のスキャン → E-KNP-003（超過）or E-KNP-004（完了済み）
        var res = await Client.PostAsJsonAsync("/api/inspection/scan", new
        {
            sessionId,
            barcode = "4975373000100",
            scanDatetime = DateTime.Now.ToString("yyyy-MM-ddTHH:mm:ss"),
            scanMethod = "PC"
        });

        Assert.Equal(HttpStatusCode.BadRequest, res.StatusCode);
        var body = await res.Content.ReadFromJsonAsync<JsonElement>();
        var errorCode = body.GetProperty("error").GetString();
        Assert.Contains(errorCode, new[] { "E-KNP-003", "E-KNP-004" });
    }

    [Fact]
    public async Task S4_03_InvalidSession_Returns401()
    {
        // 無効なセッショントークンでリクエスト
        using var noAuthClient = new HttpClient { BaseAddress = new Uri(BaseUrl) };
        noAuthClient.DefaultRequestHeaders.Add("X-Session-Token", "invalid-token-12345");

        var res = await noAuthClient.GetAsync("/api/shipping/orders");
        Assert.Equal(HttpStatusCode.Unauthorized, res.StatusCode);
        var body = await res.Content.ReadFromJsonAsync<JsonElement>();
        var errorCode = body.GetProperty("error").GetString();
        Assert.Equal("E-AUTH-003", errorCode);
    }

    [Fact]
    public async Task S4_04_DoubleLock_ReturnsLockConflict()
    {
        // 1人目がseiriNo=1004で検品開始
        var (sessionId1, _) = await StartInspection("1004", "2026-02-27", "SINGLE");
        Assert.True(sessionId1 > 0);

        // 2人目のログイン（別ユーザー）
        using var client2 = new HttpClient { BaseAddress = new Uri(BaseUrl) };
        var loginRes = await client2.PostAsJsonAsync("/api/auth/login", new
        {
            empNo = "1102",
            siteCode = "TOA-K"
        });
        loginRes.EnsureSuccessStatusCode();
        var loginBody = await loginRes.Content.ReadFromJsonAsync<JsonElement>();
        var token2 = loginBody.GetProperty("sessionToken").GetString()!;
        client2.DefaultRequestHeaders.Add("X-Session-Token", token2);

        // 2人目が同じ整理番号で検品開始 → E-LOCK-001
        var res = await client2.PostAsJsonAsync("/api/inspection/start", new
        {
            seiriNo = "1004",
            shukkaDate = "2026-02-27",
            kenpinType = "SINGLE"
        });

        Assert.Equal(HttpStatusCode.BadRequest, res.StatusCode);
        var body = await res.Content.ReadFromJsonAsync<JsonElement>();
        Assert.Equal("E-LOCK-001", body.GetProperty("error").GetString());
    }
}
