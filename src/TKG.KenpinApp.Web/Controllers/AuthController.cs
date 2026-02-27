using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using TKG.KenpinApp.Web.Constants;
using TKG.KenpinApp.Web.Data;
using TKG.KenpinApp.Web.Exceptions;
using TKG.KenpinApp.Web.Models;
using TKG.KenpinApp.Web.Models.Dto;
using TKG.KenpinApp.Web.Services;

namespace TKG.KenpinApp.Web.Controllers;

/// <summary>
/// 認証APIコントローラー
/// </summary>
[ApiController]
[Route("api/auth")]
public class AuthController : ControllerBase
{
    private readonly IAuthService _authService;
    private readonly KenpinDbContext _db;
    private readonly ILogger<AuthController> _logger;

    public AuthController(IAuthService authService, KenpinDbContext db, ILogger<AuthController> logger)
    {
        _authService = authService;
        _db = db;
        _logger = logger;
    }

    /// <summary>
    /// API-1: ログイン
    /// </summary>
    [HttpPost("login")]
    public async Task<ActionResult<LoginResponse>> Login([FromBody] LoginRequest request)
    {
        if (string.IsNullOrWhiteSpace(request.EmpNo))
        {
            throw new BusinessException(ErrorCodes.AUTH_EMPTY, "社員番号を入力してください");
        }

        var result = await _authService.LoginAsync(request);
        return Ok(result);
    }

    /// <summary>
    /// API-2: ログアウト
    /// ログアウト時に検品中のロックも解放する
    /// </summary>
    [HttpPost("logout")]
    public async Task<ActionResult<SuccessResponse>> Logout([FromBody] LogoutRequest request)
    {
        // セッション取得
        var session = await _db.UserSessions.FindAsync(request.SessionToken);
        if (session != null)
        {
            // 検品中のロックがあれば解放
            var activeLocks = await _db.KenpinLocks
                .Where(l => l.LockedBy == session.UserCode && l.ReleasedAt == null)
                .ToListAsync();

            foreach (var lk in activeLocks)
            {
                lk.ReleasedAt = DateTime.Now;
                _logger.LogInformation("ログアウトによるロック解放: seiriNo={SeiriNo}, user={User}", lk.SeiriNo, lk.LockedBy);
            }

            // 検品中セッションをPAUSEDに変更
            var activeSessions = await _db.KenpinSessions
                .Where(s => s.UserCode == session.UserCode && s.Status == "SCANNING")
                .ToListAsync();

            foreach (var ks in activeSessions)
            {
                ks.Status = "PAUSED";
                ks.UpdatedAt = DateTime.Now;
            }
        }

        var result = await _authService.LogoutAsync(request.SessionToken);
        return Ok(new SuccessResponse { Success = result });
    }
}
