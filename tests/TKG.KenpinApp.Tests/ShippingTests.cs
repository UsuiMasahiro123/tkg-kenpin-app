using System.Net;
using System.Net.Http.Json;
using System.Text.Json;
using TKG.KenpinApp.Tests.Helpers;
using TKG.KenpinApp.Web.Models.Dto;

namespace TKG.KenpinApp.Tests;

/// <summary>
/// 出荷管理APIテスト (T05〜T08)
/// </summary>
public class ShippingTests : IDisposable
{
    private readonly TestWebApplicationFactory _factory;
    private static readonly JsonSerializerOptions JsonOpts = new() { PropertyNameCaseInsensitive = true };

    public ShippingTests()
    {
        _factory = new TestWebApplicationFactory();
        _factory.EnsureDbCreated();
    }

    public void Dispose()
    {
        _factory.Dispose();
    }

    private async Task<HttpClient> CreateAuthenticatedClient()
    {
        var client = _factory.CreateClient();
        var response = await client.PostAsJsonAsync("/api/auth/login", new LoginRequest
        {
            EmpNo = "1055",
            SiteCode = "TOA-K"
        });
        var body = await response.Content.ReadFromJsonAsync<LoginResponse>(JsonOpts);
        client.DefaultRequestHeaders.Add("X-Session-Token", body!.SessionToken);
        return client;
    }

    /// <summary>
    /// T05: 出荷対象一覧取得（全件）→ 200 OK, 6件返却
    /// </summary>
    [Fact]
    public async Task T05_GetOrders_All_Returns6Items()
    {
        var client = await CreateAuthenticatedClient();

        var response = await client.GetAsync("/api/shipping/orders");
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);

        var body = await response.Content.ReadFromJsonAsync<ShippingOrderListResponse>(JsonOpts);
        Assert.NotNull(body);
        Assert.Equal(6, body.TotalCount);
        Assert.Equal(6, body.Items.Count);
    }

    /// <summary>
    /// T06: 出荷対象一覧取得（拠点フィルタ: TOA-K）→ TOA-Kのデータのみ
    /// </summary>
    [Fact]
    public async Task T06_GetOrders_FilterBySiteCode_ReturnsTOAKOnly()
    {
        var client = await CreateAuthenticatedClient();

        var response = await client.GetAsync("/api/shipping/orders?siteCode=TOA-K");
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);

        var body = await response.Content.ReadFromJsonAsync<ShippingOrderListResponse>(JsonOpts);
        Assert.NotNull(body);
        Assert.True(body.TotalCount > 0);
        Assert.All(body.Items, item => Assert.Equal("TOA-K", item.WarehouseCode));
    }

    /// <summary>
    /// T07: 出荷指示詳細取得（1001）→ 200 OK
    /// </summary>
    [Fact]
    public async Task T07_GetOrderDetail_Existing_ReturnsDetail()
    {
        var client = await CreateAuthenticatedClient();

        var response = await client.GetAsync("/api/shipping/orders/1001");
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);

        var body = await response.Content.ReadFromJsonAsync<ShippingOrderDetailResponse>(JsonOpts);
        Assert.NotNull(body);
        Assert.Equal("1001", body.SeiriNo);
        Assert.Equal("SINGLE", body.PickType);
    }

    /// <summary>
    /// T08: 存在しない整理番号で詳細取得 → 404
    /// </summary>
    [Fact]
    public async Task T08_GetOrderDetail_NotFound_Returns404()
    {
        var client = await CreateAuthenticatedClient();

        var response = await client.GetAsync("/api/shipping/orders/9999");
        Assert.Equal(HttpStatusCode.NotFound, response.StatusCode);
    }
}
