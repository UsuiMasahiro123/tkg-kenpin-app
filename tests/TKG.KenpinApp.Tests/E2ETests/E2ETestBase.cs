using System.Net.Http.Json;
using System.Text.Json;

namespace TKG.KenpinApp.Tests.E2ETests;

/// <summary>
/// E2Eテスト共通基盤
/// 実サーバー（localhost:5079）に対してHTTPリクエストを送信する
/// </summary>
public abstract class E2ETestBase : IAsyncLifetime
{
    protected static readonly string BaseUrl = "http://localhost:5079";
    protected HttpClient Client { get; private set; } = null!;
    protected string SessionToken { get; private set; } = string.Empty;

    protected static readonly JsonSerializerOptions JsonOptions = new()
    {
        PropertyNameCaseInsensitive = true
    };

    public async Task InitializeAsync()
    {
        Client = new HttpClient { BaseAddress = new Uri(BaseUrl) };

        // モックデータリロード（DB初期化）
        var reloadRes = await Client.PostAsync("/api/dev/reload-mockdata", null);
        reloadRes.EnsureSuccessStatusCode();

        // ログインしてセッショントークン取得
        var loginRes = await Client.PostAsJsonAsync("/api/auth/login", new
        {
            empNo = "1055",
            siteCode = "TOA-K"
        });
        loginRes.EnsureSuccessStatusCode();
        var loginBody = await loginRes.Content.ReadFromJsonAsync<JsonElement>();
        SessionToken = loginBody.GetProperty("sessionToken").GetString()!;

        // 以降のリクエストにセッショントークンを付与
        Client.DefaultRequestHeaders.Add("X-Session-Token", SessionToken);
    }

    public Task DisposeAsync()
    {
        Client?.Dispose();
        return Task.CompletedTask;
    }

    /// <summary>
    /// 検品セッションを開始する
    /// </summary>
    protected async Task<(long sessionId, JsonElement body)> StartInspection(
        string seiriNo, string shukkaDate = "2026-02-27", string kenpinType = "")
    {
        var res = await Client.PostAsJsonAsync("/api/inspection/start", new
        {
            seiriNo,
            shukkaDate,
            kenpinType
        });
        res.EnsureSuccessStatusCode();
        var body = await res.Content.ReadFromJsonAsync<JsonElement>();
        var sessionId = body.GetProperty("sessionId").GetInt64();
        return (sessionId, body);
    }

    /// <summary>
    /// バーコードスキャンを実行する
    /// </summary>
    protected async Task<(HttpResponseMessage response, JsonElement body)> Scan(
        long sessionId, string barcode)
    {
        var res = await Client.PostAsJsonAsync("/api/inspection/scan", new
        {
            sessionId,
            barcode,
            scanDatetime = DateTime.Now.ToString("yyyy-MM-ddTHH:mm:ss"),
            scanMethod = "PC"
        });
        var body = await res.Content.ReadFromJsonAsync<JsonElement>();
        return (res, body);
    }

    /// <summary>
    /// 指定回数バーコードスキャンを繰り返す
    /// </summary>
    protected async Task<JsonElement> ScanMultiple(long sessionId, string barcode, int count)
    {
        JsonElement lastBody = default;
        for (int i = 0; i < count; i++)
        {
            var (res, body) = await Scan(sessionId, barcode);
            res.EnsureSuccessStatusCode();
            lastBody = body;
        }
        return lastBody;
    }

    /// <summary>
    /// 検品完了
    /// </summary>
    protected async Task<JsonElement> Complete(long sessionId)
    {
        var res = await Client.PutAsJsonAsync("/api/inspection/complete", new { sessionId });
        res.EnsureSuccessStatusCode();
        return await res.Content.ReadFromJsonAsync<JsonElement>();
    }

    /// <summary>
    /// 検品中断
    /// </summary>
    protected async Task<JsonElement> Pause(long sessionId)
    {
        var res = await Client.PutAsJsonAsync("/api/inspection/pause", new { sessionId });
        res.EnsureSuccessStatusCode();
        return await res.Content.ReadFromJsonAsync<JsonElement>();
    }
}
