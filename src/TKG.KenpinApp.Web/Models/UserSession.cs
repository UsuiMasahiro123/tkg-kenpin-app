using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace TKG.KenpinApp.Web.Models;

/// <summary>
/// ユーザーセッション
/// </summary>
[Table("T_USER_SESSION")]
public class UserSession
{
    [Key]
    [MaxLength(128)]
    [Column("session_token")]
    public string SessionToken { get; set; } = string.Empty;

    [Required]
    [MaxLength(20)]
    [Column("user_code")]
    public string UserCode { get; set; } = string.Empty;

    [MaxLength(100)]
    [Column("user_name")]
    public string? UserName { get; set; }

    [Required]
    [MaxLength(10)]
    [Column("site_code")]
    public string SiteCode { get; set; } = string.Empty;

    [Column("login_at")]
    public DateTime? LoginAt { get; set; }

    /// <summary>
    /// 有効期限（8時間）
    /// </summary>
    [Column("expires_at")]
    public DateTime? ExpiresAt { get; set; }
}
