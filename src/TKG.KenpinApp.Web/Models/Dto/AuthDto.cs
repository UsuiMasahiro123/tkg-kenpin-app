namespace TKG.KenpinApp.Web.Models.Dto;

/// <summary>
/// ログインリクエスト
/// </summary>
public class LoginRequest
{
    public string EmpNo { get; set; } = string.Empty;
    public string SiteCode { get; set; } = string.Empty;
}

/// <summary>
/// ログインレスポンス
/// </summary>
public class LoginResponse
{
    public string SessionToken { get; set; } = string.Empty;
    public string UserName { get; set; } = string.Empty;
    public string SiteCode { get; set; } = string.Empty;
    public string SiteName { get; set; } = string.Empty;
}

/// <summary>
/// ログアウトリクエスト
/// </summary>
public class LogoutRequest
{
    public string SessionToken { get; set; } = string.Empty;
}

/// <summary>
/// 汎用成功レスポンス
/// </summary>
public class SuccessResponse
{
    public bool Success { get; set; }
}
