using TKG.KenpinApp.Web.Models.Dto;

namespace TKG.KenpinApp.Web.MockD365;

/// <summary>
/// D365連携サービスインターフェース
/// 本番環境ではD365 APIに接続する実装に差し替え可能
/// </summary>
public interface ID365Service
{
    /// <summary>
    /// 社員マスタから社員情報を取得
    /// </summary>
    Task<D365Employee?> GetEmployeeAsync(string empNo);

    /// <summary>
    /// 出荷指示一覧を取得
    /// </summary>
    Task<List<D365ShippingOrder>> GetShippingOrdersAsync(
        string? dateFrom, string? dateTo, string? status, string? siteCode);

    /// <summary>
    /// 出荷指示詳細を取得
    /// </summary>
    Task<D365ShippingOrder?> GetShippingOrderAsync(string seiriNo);

    /// <summary>
    /// 出荷指示明細を取得
    /// </summary>
    Task<List<InspectionItem>> GetShippingOrderItemsAsync(string seiriNo);

    /// <summary>
    /// バーコードから品目コードを照合
    /// </summary>
    Task<D365ItemInfo?> LookupBarcodeAsync(string barcode);

    /// <summary>
    /// 梱包明細転記を実行
    /// </summary>
    Task<PackingSlipPostResponse> PostPackingSlipAsync(string[] seiriNos, string orderType);

    /// <summary>
    /// 拠点コードから拠点名を取得
    /// </summary>
    string GetSiteName(string siteCode);

    /// <summary>
    /// 検品結果をD365に連携する
    /// 失敗時は例外をスロー
    /// </summary>
    Task SyncInspectionResultAsync(long sessionId, string payload);
}

/// <summary>
/// D365社員情報
/// </summary>
public class D365Employee
{
    public string EmpNo { get; set; } = string.Empty;
    public string Name { get; set; } = string.Empty;
    public string Department { get; set; } = string.Empty;
}

/// <summary>
/// D365出荷指示情報
/// </summary>
public class D365ShippingOrder
{
    public string SeiriNo { get; set; } = string.Empty;
    public string ShukkaDate { get; set; } = string.Empty;
    public string CustomerCode { get; set; } = string.Empty;
    public string CustomerName { get; set; } = string.Empty;
    public int KoguchiSu { get; set; }
    public string WarehouseCode { get; set; } = string.Empty;
    public string DeliveryDate { get; set; } = string.Empty;
    public string ShipDate { get; set; } = string.Empty;
    public string CarrierName { get; set; } = string.Empty;
    public string PickType { get; set; } = string.Empty;
    public string Status { get; set; } = string.Empty;
    public string? Note { get; set; }
    public List<InspectionItem> Items { get; set; } = new();
}

/// <summary>
/// D365品目情報（バーコード照合結果）
/// </summary>
public class D365ItemInfo
{
    public string ItemCode { get; set; } = string.Empty;
    public string ItemName { get; set; } = string.Empty;
    public string JanCode { get; set; } = string.Empty;
    public string KenpinCategory { get; set; } = string.Empty;
    public int UchibakoIrisu { get; set; }
    public int UnitIrisu { get; set; }
}
