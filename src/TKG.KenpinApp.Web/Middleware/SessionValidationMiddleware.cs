using Microsoft.EntityFrameworkCore;
using TKG.KenpinApp.Web.Constants;
using TKG.KenpinApp.Web.Data;

namespace TKG.KenpinApp.Web.Middleware;

/// <summary>
/// セッション検証ミドルウェア
/// API呼出時にセッショントークンの有効性を検証する
/// </summary>
public class SessionValidationMiddleware
{
    private readonly RequestDelegate _next;
    private readonly ILogger<SessionValidationMiddleware> _logger;

    /// <summary>
    /// セッション検証をスキップするAPIパス
    /// </summary>
    private static readonly string[] SkipPaths = new[]
    {
        "/api/auth/login"
    };

    public SessionValidationMiddleware(RequestDelegate next, ILogger<SessionValidationMiddleware> logger)
    {
        _next = next;
        _logger = logger;
    }

    public async Task InvokeAsync(HttpContext context, KenpinDbContext db)
    {
        // API以外のリクエスト、またはスキップ対象パスはそのまま通す
        if (!context.Request.Path.StartsWithSegments("/api")
            || SkipPaths.Any(p => context.Request.Path.StartsWithSegments(p)))
        {
            await _next(context);
            return;
        }

        // Swagger UIのパスもスキップ
        if (context.Request.Path.StartsWithSegments("/swagger"))
        {
            await _next(context);
            return;
        }

        // セッショントークン取得（ヘッダー優先、Cookie fallback）
        var token = GetSessionToken(context);

        if (string.IsNullOrEmpty(token))
        {
            _logger.LogWarning("セッショントークンなし: {Path}", context.Request.Path);
            context.Response.StatusCode = StatusCodes.Status401Unauthorized;
            await context.Response.WriteAsJsonAsync(new
            {
                error = ErrorCodes.AUTH_EXPIRED,
                message = "セッションが切れました。再ログインしてください"
            });
            return;
        }

        // セッション検証
        var session = await db.UserSessions
            .FirstOrDefaultAsync(s => s.SessionToken == token);

        if (session == null)
        {
            _logger.LogWarning("セッション不明: token={Token}", token[..Math.Min(8, token.Length)] + "...");
            context.Response.StatusCode = StatusCodes.Status401Unauthorized;
            await context.Response.WriteAsJsonAsync(new
            {
                error = ErrorCodes.AUTH_EXPIRED,
                message = "セッションが切れました。再ログインしてください"
            });
            return;
        }

        // 有効期限チェック
        if (session.ExpiresAt.HasValue && session.ExpiresAt.Value < DateTime.Now)
        {
            _logger.LogWarning("セッション期限切れ: userCode={UserCode}", session.UserCode);
            db.UserSessions.Remove(session);
            await db.SaveChangesAsync();

            context.Response.StatusCode = StatusCodes.Status401Unauthorized;
            await context.Response.WriteAsJsonAsync(new
            {
                error = ErrorCodes.AUTH_EXPIRED,
                message = "セッションが切れました。再ログインしてください"
            });
            return;
        }

        // ユーザー情報をHttpContext.Itemsに格納（後続のコントローラーで使用）
        context.Items["UserCode"] = session.UserCode;
        context.Items["UserName"] = session.UserName;
        context.Items["SiteCode"] = session.SiteCode;

        await _next(context);
    }

    /// <summary>
    /// リクエストからセッショントークンを取得
    /// X-Session-Token ヘッダー → Cookie の順で探索
    /// </summary>
    private static string? GetSessionToken(HttpContext context)
    {
        // ヘッダーから取得
        if (context.Request.Headers.TryGetValue("X-Session-Token", out var headerToken)
            && !string.IsNullOrEmpty(headerToken.ToString()))
        {
            return headerToken.ToString();
        }

        // Cookieから取得
        if (context.Request.Cookies.TryGetValue("sessionToken", out var cookieToken)
            && !string.IsNullOrEmpty(cookieToken))
        {
            return cookieToken;
        }

        return null;
    }
}
