using System.Net;
using System.Net.Http.Json;
using System.Text;
using System.Text.Json;
using TKG.KenpinApp.Tests.Helpers;

namespace TKG.KenpinApp.Tests;

/// <summary>
/// エラーハンドリングテスト (T28〜T29)
/// </summary>
public class ErrorHandlingTests : IDisposable
{
    private readonly TestWebApplicationFactory _factory;
    private readonly HttpClient _client;

    public ErrorHandlingTests()
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
    /// T28: 無効なセッショントークンでAPI呼出 → 401, E-AUTH-003
    /// </summary>
    [Fact]
    public async Task T28_InvalidSessionToken_Returns401()
    {
        _client.DefaultRequestHeaders.Add("X-Session-Token", "invalid-token-12345");

        var response = await _client.GetAsync("/api/shipping/orders");

        Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
        var body = await response.Content.ReadAsStringAsync();
        Assert.Contains("E-AUTH-003", body);
    }

    /// <summary>
    /// T29: 不正なリクエストBody → 400
    /// </summary>
    [Fact]
    public async Task T29_InvalidRequestBody_Returns400()
    {
        var content = new StringContent("{invalid_json}", Encoding.UTF8, "application/json");
        var response = await _client.PostAsync("/api/auth/login", content);

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
    }
}
