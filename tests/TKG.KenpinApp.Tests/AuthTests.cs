using System.Net;
using System.Net.Http.Json;
using System.Text.Json;
using TKG.KenpinApp.Tests.Helpers;
using TKG.KenpinApp.Web.Models.Dto;

namespace TKG.KenpinApp.Tests;

/// <summary>
/// 認証APIテスト (T01〜T04)
/// </summary>
public class AuthTests : IDisposable
{
    private readonly TestWebApplicationFactory _factory;
    private readonly HttpClient _client;
    private static readonly JsonSerializerOptions JsonOpts = new() { PropertyNameCaseInsensitive = true };

    public AuthTests()
    {
        _factory = new TestWebApplicationFactory();
        _factory.EnsureDbCreated();
        _client = _factory.CreateClient();
    }

    public void Dispose()
    {
        _client.Dispose();
        _factory.Dispose();
    }

    /// <summary>
    /// T01: 正常ログイン（1055, TOA-K）→ 200 OK, sessionToken返却
    /// </summary>
    [Fact]
    public async Task T01_Login_ValidCredentials_ReturnsSessionToken()
    {
        var response = await _client.PostAsJsonAsync("/api/auth/login", new LoginRequest
        {
            EmpNo = "1055",
            SiteCode = "TOA-K"
        });

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);

        var body = await response.Content.ReadFromJsonAsync<LoginResponse>(JsonOpts);
        Assert.NotNull(body);
        Assert.False(string.IsNullOrEmpty(body.SessionToken));
        Assert.Equal("下原 太郎", body.UserName);
        Assert.Equal("TOA-K", body.SiteCode);
        Assert.Equal("東亜（川崎）", body.SiteName);
    }

    /// <summary>
    /// T02: 存在しない社員番号でログイン → 400, E-AUTH-002
    /// </summary>
    [Fact]
    public async Task T02_Login_InvalidEmpNo_ReturnsError()
    {
        var response = await _client.PostAsJsonAsync("/api/auth/login", new LoginRequest
        {
            EmpNo = "9999",
            SiteCode = "TOA-K"
        });

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);

        var body = await response.Content.ReadAsStringAsync();
        Assert.Contains("E-AUTH-002", body);
    }

    /// <summary>
    /// T03: 社員番号空でログイン → 400, E-AUTH-001
    /// </summary>
    [Fact]
    public async Task T03_Login_EmptyEmpNo_ReturnsError()
    {
        var response = await _client.PostAsJsonAsync("/api/auth/login", new LoginRequest
        {
            EmpNo = "",
            SiteCode = "TOA-K"
        });

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);

        var body = await response.Content.ReadAsStringAsync();
        Assert.Contains("E-AUTH-001", body);
    }

    /// <summary>
    /// T04: ログアウト → 200 OK, success=true
    /// </summary>
    [Fact]
    public async Task T04_Logout_ValidSession_ReturnsSuccess()
    {
        // まずログインしてトークンを取得
        var loginResponse = await _client.PostAsJsonAsync("/api/auth/login", new LoginRequest
        {
            EmpNo = "1055",
            SiteCode = "TOA-K"
        });
        var loginBody = await loginResponse.Content.ReadFromJsonAsync<LoginResponse>(JsonOpts);
        Assert.NotNull(loginBody);

        // ログアウト（ログアウトにはセッション検証不要 — SkipPathに/api/auth/loginのみだが、logoutもAPIなのでセッションヘッダーが必要）
        _client.DefaultRequestHeaders.Add("X-Session-Token", loginBody.SessionToken);
        var logoutResponse = await _client.PostAsJsonAsync("/api/auth/logout", new LogoutRequest
        {
            SessionToken = loginBody.SessionToken
        });

        Assert.Equal(HttpStatusCode.OK, logoutResponse.StatusCode);

        var body = await logoutResponse.Content.ReadFromJsonAsync<SuccessResponse>(JsonOpts);
        Assert.NotNull(body);
        Assert.True(body.Success);
    }
}
