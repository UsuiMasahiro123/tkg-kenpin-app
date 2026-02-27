using TKG.KenpinApp.Web.Data;
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

    public AuthService(KenpinDbContext db, ID365Service d365, ILogger<AuthService> logger)
    {
        _db = db;
        _d365 = d365;
        _logger = logger;
    }

    // タスク6-2で本実装
    public Task<LoginResponse> LoginAsync(LoginRequest request) => throw new NotImplementedException();
    public Task<bool> LogoutAsync(string sessionToken) => throw new NotImplementedException();
    public Task<Models.UserSession?> ValidateSessionAsync(string sessionToken) => throw new NotImplementedException();
}
