namespace TKG.KenpinApp.Web.Models.Dto;

/// <summary>
/// 出荷対象一覧レスポンス
/// </summary>
public class ShippingOrderListResponse
{
    public int TotalCount { get; set; }
    public List<ShippingOrderSummary> Items { get; set; } = new();
}

/// <summary>
/// 出荷指示サマリ
/// </summary>
public class ShippingOrderSummary
{
    public string SeiriNo { get; set; } = string.Empty;
    public int KoguchiSu { get; set; }
    public string CustomerCode { get; set; } = string.Empty;
    public string CustomerName { get; set; } = string.Empty;
    public string WarehouseCode { get; set; } = string.Empty;
    public string DeliveryDate { get; set; } = string.Empty;
    public string ShipDate { get; set; } = string.Empty;
    public string CarrierName { get; set; } = string.Empty;
    public string PickType { get; set; } = string.Empty;
    public string Status { get; set; } = string.Empty;
    public string? Note { get; set; }
}

/// <summary>
/// 出荷指示詳細レスポンス
/// </summary>
public class ShippingOrderDetailResponse
{
    public string SeiriNo { get; set; } = string.Empty;
    public string CustomerName { get; set; } = string.Empty;
    public int KoguchiSu { get; set; }
    public string PickType { get; set; } = string.Empty;
    public string WarehouseCode { get; set; } = string.Empty;
    public bool IsLocked { get; set; }
    public string? LockedBy { get; set; }
}

/// <summary>
/// 梱包明細転記リクエスト
/// </summary>
public class PackingSlipPostRequest
{
    public string[] SeiriNos { get; set; } = Array.Empty<string>();
    public string OrderType { get; set; } = string.Empty;
}

/// <summary>
/// 梱包明細転記レスポンス
/// </summary>
public class PackingSlipPostResponse
{
    public string JobId { get; set; } = string.Empty;
    public string Status { get; set; } = string.Empty;
}
