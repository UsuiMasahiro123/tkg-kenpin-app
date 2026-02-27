using System.Security.Cryptography;
using Microsoft.EntityFrameworkCore;
using TKG.KenpinApp.Web.Constants;
using TKG.KenpinApp.Web.Data;
using TKG.KenpinApp.Web.Exceptions;
using TKG.KenpinApp.Web.Models;
using TKG.KenpinApp.Web.Models.Dto;
using TKG.KenpinApp.Web.MockD365;

namespace TKG.KenpinApp.Web.Services;

/// <summary>
/// 認証サービス実装
/// </summary>
public class AuthService : IAuthService
{
    private readonly KenpinDbContext _db;
    private readonly ID365Service _d365;
    private readonly ILogger<AuthService> _logger;

    /// <summary>
    /// セッション有効期限（8時間）
    /// </summary>
    private const int SessionExpirationHours = 8;

    public AuthService(KenpinDbContext db, ID365Service d365, ILogger<AuthService> logger)
    {
        _db = db;
        _d365 = d365;
        _logger = logger;
    }

    /// <inheritdoc/>
    public async Task<LoginResponse> LoginAsync(LoginRequest request)
    {
        if (string.IsNullOrWhiteSpace(request.EmpNo))
        {
            throw new BusinessException(ErrorCodes.AUTH_EMPTY, "社員番号を入力してください");
        }

        // D365モック社員マスタ照会
        var employee = await _d365.GetEmployeeAsync(request.EmpNo);
        if (employee == null)
        {
            throw new BusinessException(ErrorCodes.AUTH_NOT_FOUND, "社員番号が見つかりません");
        }

        // セッショントークン生成
        var sessionToken = GenerateSessionToken();
        var now = DateTime.Now;

        // T_USER_SESSION INSERT
        var userSession = new UserSession
        {
            SessionToken = sessionToken,
            UserCode = request.EmpNo,
            UserName = employee.Name,
            SiteCode = request.SiteCode,
            LoginAt = now,
            ExpiresAt = now.AddHours(SessionExpirationHours)
        };
        _db.UserSessions.Add(userSession);

        // T_APP_LOG INSERT
        _db.AppLogs.Add(new AppLog
        {
            LogDatetime = now,
            UserCode = request.EmpNo,
            ActionType = "LOGIN",
            ScreenId = "LOGIN",
            Detail = System.Text.Json.JsonSerializer.Serialize(new
            {
                siteCode = request.SiteCode,
                userName = employee.Name
            })
        });

        await _db.SaveChangesAsync();

        _logger.LogInformation("ログイン成功: empNo={EmpNo}, site={SiteCode}", request.EmpNo, request.SiteCode);

        return new LoginResponse
        {
            SessionToken = sessionToken,
            UserName = employee.Name,
            SiteCode = request.SiteCode,
            SiteName = _d365.GetSiteName(request.SiteCode)
        };
    }

    /// <inheritdoc/>
    public async Task<bool> LogoutAsync(string sessionToken)
    {
        var session = await _db.UserSessions.FindAsync(sessionToken);
        if (session == null)
        {
            return false;
        }

        // セッション削除
        _db.UserSessions.Remove(session);

        // T_APP_LOG INSERT
        _db.AppLogs.Add(new AppLog
        {
            LogDatetime = DateTime.Now,
            UserCode = session.UserCode,
            ActionType = "LOGOUT",
            ScreenId = "LOGIN",
            Detail = System.Text.Json.JsonSerializer.Serialize(new
            {
                userCode = session.UserCode
            })
        });

        await _db.SaveChangesAsync();

        _logger.LogInformation("ログアウト: userCode={UserCode}", session.UserCode);

        return true;
    }

    /// <inheritdoc/>
    public async Task<UserSession?> ValidateSessionAsync(string sessionToken)
    {
        var session = await _db.UserSessions.FindAsync(sessionToken);

        if (session == null)
            return null;

        // 有効期限チェック
        if (session.ExpiresAt.HasValue && session.ExpiresAt.Value < DateTime.Now)
        {
            _logger.LogWarning("セッション期限切れ: userCode={UserCode}", session.UserCode);
            _db.UserSessions.Remove(session);
            await _db.SaveChangesAsync();
            return null;
        }

        return session;
    }

    /// <summary>
    /// セッショントークン生成（暗号論的に安全なランダム文字列）
    /// </summary>
    private static string GenerateSessionToken()
    {
        var bytes = RandomNumberGenerator.GetBytes(64);
        return Convert.ToBase64String(bytes);
    }
}
