using TKG.KenpinApp.Web.Models.Dto;

namespace TKG.KenpinApp.Web.Services;

/// <summary>
/// 認証サービスインターフェース
/// </summary>
public interface IAuthService
{
    /// <summary>
    /// ログイン処理
    /// </summary>
    Task<LoginResponse> LoginAsync(LoginRequest request);

    /// <summary>
    /// ログアウト処理
    /// </summary>
    Task<bool> LogoutAsync(string sessionToken);

    /// <summary>
    /// セッション検証
    /// </summary>
    Task<Models.UserSession?> ValidateSessionAsync(string sessionToken);
}
