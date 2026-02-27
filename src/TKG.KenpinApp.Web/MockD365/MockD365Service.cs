using TKG.KenpinApp.Web.Models.Dto;

namespace TKG.KenpinApp.Web.MockD365;

/// <summary>
/// D365モックAPIサービス（開発用）
/// 本番環境ではD365 APIに接続する実装に差し替える
/// </summary>
public class MockD365Service : ID365Service
{
    // タスク6-2で実装予定
    public Task<D365Employee?> GetEmployeeAsync(string empNo) => Task.FromResult<D365Employee?>(null);
    public Task<List<D365ShippingOrder>> GetShippingOrdersAsync(string? dateFrom, string? dateTo, string? status, string? siteCode) => Task.FromResult(new List<D365ShippingOrder>());
    public Task<D365ShippingOrder?> GetShippingOrderAsync(string seiriNo) => Task.FromResult<D365ShippingOrder?>(null);
    public Task<List<InspectionItem>> GetShippingOrderItemsAsync(string seiriNo) => Task.FromResult(new List<InspectionItem>());
    public Task<D365ItemInfo?> LookupBarcodeAsync(string barcode) => Task.FromResult<D365ItemInfo?>(null);
    public Task<PackingSlipPostResponse> PostPackingSlipAsync(string[] seiriNos, string orderType) => Task.FromResult(new PackingSlipPostResponse());
    public string GetSiteName(string siteCode) => siteCode;
}
