namespace TKG.KenpinApp.Web.Constants;

/// <summary>
/// アプリケーション全体で使用するエラーコード定数
/// </summary>
public static class ErrorCodes
{
    // --- 認証系 ---
    /// <summary>社員番号未入力</summary>
    public const string AUTH_EMPTY = "E-AUTH-001";

    /// <summary>社員番号不一致</summary>
    public const string AUTH_NOT_FOUND = "E-AUTH-002";

    /// <summary>セッション期限切れ</summary>
    public const string AUTH_EXPIRED = "E-AUTH-003";

    // --- 出荷系 ---
    /// <summary>検索結果0件</summary>
    public const string SHP_NO_DATA = "E-SHP-001";

    // --- 検品系 ---
    /// <summary>バーコード不正</summary>
    public const string KNP_INVALID_BARCODE = "E-KNP-001";

    /// <summary>品目不一致</summary>
    public const string KNP_ITEM_NOT_FOUND = "E-KNP-002";

    /// <summary>数量超過</summary>
    public const string KNP_OVER_QTY = "E-KNP-003";

    /// <summary>完了済品目</summary>
    public const string KNP_ALREADY_DONE = "E-KNP-004";

    /// <summary>不正なステータス遷移</summary>
    public const string KNP_INVALID_STATUS = "E-KNP-005";

    /// <summary>セッションが見つからない</summary>
    public const string KNP_SESSION_NOT_FOUND = "E-KNP-006";

    // --- ロック系 ---
    /// <summary>ロック競合</summary>
    public const string LOCK_CONFLICT = "E-LOCK-001";

    /// <summary>ロックタイムアウト</summary>
    public const string LOCK_TIMEOUT = "E-LOCK-002";

    // --- 通信系 ---
    /// <summary>通信断</summary>
    public const string NET_OFFLINE = "E-NET-001";

    /// <summary>APIタイムアウト</summary>
    public const string NET_TIMEOUT = "E-NET-002";

    // --- D365連携系 ---
    /// <summary>D365連携失敗</summary>
    public const string D365_SYNC_FAIL = "E-D365-001";

    // --- システム系 ---
    /// <summary>システムエラー</summary>
    public const string SYS_ERROR = "E-SYS-001";
}
