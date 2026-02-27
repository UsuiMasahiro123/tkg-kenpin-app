using Microsoft.EntityFrameworkCore;
using TKG.KenpinApp.Web.Data;
using TKG.KenpinApp.Web.Middleware;
using TKG.KenpinApp.Web.MockD365;
using TKG.KenpinApp.Web.Services;

var builder = WebApplication.CreateBuilder(args);

// データベース設定
builder.Services.AddDbContext<KenpinDbContext>(options =>
    options.UseSqlite(builder.Configuration.GetConnectionString("DefaultConnection")));

// DIサービス登録
builder.Services.AddSingleton<ID365Service, MockD365Service>();
builder.Services.AddScoped<IAuthService, AuthService>();
builder.Services.AddScoped<IInspectionService, InspectionService>();

// バックグラウンドジョブ
builder.Services.AddHostedService<LockTimeoutService>();
builder.Services.AddHostedService<D365SyncRetryService>();

// コントローラー登録
builder.Services.AddControllers();

// Razor Pages
builder.Services.AddRazorPages();

// Swagger（開発環境のみ）
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen(c =>
{
    c.SwaggerDoc("v1", new() { Title = "TKG検品アプリ API", Version = "v1" });
});

var app = builder.Build();

// 開発環境設定
if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI(c =>
    {
        c.SwaggerEndpoint("/swagger/v1/swagger.json", "TKG検品アプリ API v1");
    });
}
else
{
    app.UseExceptionHandler("/Error");
    app.UseHsts();
}

app.UseHttpsRedirection();
app.UseStaticFiles();

app.UseRouting();

// グローバル例外ハンドリングミドルウェア
app.UseMiddleware<ExceptionHandlingMiddleware>();

// セッション検証ミドルウェア
app.UseMiddleware<SessionValidationMiddleware>();

app.UseAuthorization();

// APIコントローラーのマッピング
app.MapControllers();

// Razor Pagesのマッピング
app.MapRazorPages();

app.Run();
