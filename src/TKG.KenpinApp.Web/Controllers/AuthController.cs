using Microsoft.AspNetCore.Mvc;
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
    private readonly ILogger<AuthController> _logger;

    public AuthController(IAuthService authService, ILogger<AuthController> logger)
    {
        _authService = authService;
        _logger = logger;
    }

    /// <summary>
    /// API-1: ログイン
    /// </summary>
    [HttpPost("login")]
    public async Task<ActionResult<LoginResponse>> Login([FromBody] LoginRequest request)
    {
        try
        {
            var result = await _authService.LoginAsync(request);
            return Ok(result);
        }
        catch (InvalidOperationException ex)
        {
            _logger.LogWarning("ログイン失敗: {Message}", ex.Message);
            return BadRequest(new { error = ex.Message });
        }
    }

    /// <summary>
    /// API-2: ログアウト
    /// </summary>
    [HttpPost("logout")]
    public async Task<ActionResult<SuccessResponse>> Logout([FromBody] LogoutRequest request)
    {
        var result = await _authService.LogoutAsync(request.SessionToken);
        return Ok(new SuccessResponse { Success = result });
    }
}
