namespace TKG.KenpinApp.Web.Models.Dto;

/// <summary>
/// 検品セッション開始リクエスト
/// </summary>
public class InspectionStartRequest
{
    public string SeiriNo { get; set; } = string.Empty;
    public string ShukkaDate { get; set; } = string.Empty;
    public string KenpinType { get; set; } = string.Empty;
}

/// <summary>
/// 検品セッション開始レスポンス
/// </summary>
public class InspectionStartResponse
{
    public long SessionId { get; set; }
    public List<InspectionItem> Items { get; set; } = new();
}

/// <summary>
/// 検品対象商品
/// </summary>
public class InspectionItem
{
    public int LineNo { get; set; }
    public string ItemCode { get; set; } = string.Empty;
    public string ItemName { get; set; } = string.Empty;
    public string LocationNo { get; set; } = string.Empty;
    public int ShipQty { get; set; }
    public int BaraShukkaSu { get; set; }
    public int UnitShukkaSu { get; set; }
    public int UchibakoIrisu { get; set; }
    public int UnitIrisu { get; set; }
    public string JanCode { get; set; } = string.Empty;
    public string KenpinCategory { get; set; } = string.Empty;
}

/// <summary>
/// スキャンリクエスト
/// </summary>
public class ScanRequest
{
    public long SessionId { get; set; }
    public string Barcode { get; set; } = string.Empty;
    public string ScanDatetime { get; set; } = string.Empty;
    public string ScanMethod { get; set; } = string.Empty;
}

/// <summary>
/// スキャンレスポンス
/// </summary>
public class ScanResponse
{
    public bool Success { get; set; }
    public long DetailId { get; set; }
    public string ItemCode { get; set; } = string.Empty;
    public int AddedQty { get; set; }
    public int NewScannedQty { get; set; }
    public int RemainQty { get; set; }
    public bool IsItemComplete { get; set; }
    public bool IsAllComplete { get; set; }
}

/// <summary>
/// スキャン取消リクエスト
/// </summary>
public class ScanCancelRequest
{
    public long SessionId { get; set; }
    public long DetailId { get; set; }
}

/// <summary>
/// スキャン取消レスポンス
/// </summary>
public class ScanCancelResponse
{
    public bool Success { get; set; }
    public string UndoneItemCode { get; set; } = string.Empty;
    public int NewScannedQty { get; set; }
}

/// <summary>
/// 検品中断/完了リクエスト
/// </summary>
public class SessionActionRequest
{
    public long SessionId { get; set; }
}

/// <summary>
/// 検品完了レスポンス
/// </summary>
public class InspectionCompleteResponse
{
    public bool Success { get; set; }
    public string D365SyncStatus { get; set; } = string.Empty;
}

/// <summary>
/// 伝票照合リクエスト
/// </summary>
public class SlipVerifyRequest
{
    public long SessionId { get; set; }
    public string DenpyoType { get; set; } = string.Empty;
    public string Barcode { get; set; } = string.Empty;
}

/// <summary>
/// 伝票照合レスポンス
/// </summary>
public class SlipVerifyResponse
{
    public bool Success { get; set; }
    public string Result { get; set; } = string.Empty;
}
