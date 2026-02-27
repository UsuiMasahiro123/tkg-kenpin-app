using System.Text.Json;
using TKG.KenpinApp.Web.Constants;
using TKG.KenpinApp.Web.Exceptions;

namespace TKG.KenpinApp.Web.Middleware;

/// <summary>
/// グローバル例外ハンドリングミドルウェア
/// 全APIリクエストの例外を一元的にキャッチし、統一フォーマットで返却する
/// </summary>
public class ExceptionHandlingMiddleware
{
    private readonly RequestDelegate _next;
    private readonly ILogger<ExceptionHandlingMiddleware> _logger;

    public ExceptionHandlingMiddleware(RequestDelegate next, ILogger<ExceptionHandlingMiddleware> logger)
    {
        _next = next;
        _logger = logger;
    }

    public async Task InvokeAsync(HttpContext context)
    {
        try
        {
            await _next(context);
        }
        catch (Exception ex) when (context.Request.Path.StartsWithSegments("/api"))
        {
            await HandleExceptionAsync(context, ex);
        }
    }

    /// <summary>
    /// 例外種別に応じたHTTPレスポンスを返却
    /// </summary>
    private async Task HandleExceptionAsync(HttpContext context, Exception ex)
    {
        context.Response.ContentType = "application/json";

        if (ex is BusinessException bizEx)
        {
            // 業務エラー → 400 Bad Request
            _logger.LogWarning("業務エラー: code={Code}, message={Message}", bizEx.Code, bizEx.Message);
            context.Response.StatusCode = StatusCodes.Status400BadRequest;
            await context.Response.WriteAsJsonAsync(new
            {
                error = bizEx.Code,
                message = bizEx.Message
            });
        }
        else if (ex is InvalidOperationException invalidEx)
        {
            // 既存の InvalidOperationException も 400 として処理（後方互換性）
            _logger.LogWarning("操作エラー: {Message}", invalidEx.Message);
            context.Response.StatusCode = StatusCodes.Status400BadRequest;
            await context.Response.WriteAsJsonAsync(new
            {
                error = invalidEx.Message
            });
        }
        else
        {
            // システムエラー → 500 Internal Server Error
            _logger.LogError(ex, "未処理の例外が発生しました");
            context.Response.StatusCode = StatusCodes.Status500InternalServerError;
            await context.Response.WriteAsJsonAsync(new
            {
                error = ErrorCodes.SYS_ERROR,
                message = "システムエラーが発生しました"
            });
        }
    }
}
