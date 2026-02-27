using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace TKG.KenpinApp.Web.Models;

/// <summary>
/// アプリケーションログ
/// </summary>
[Table("T_APP_LOG")]
public class AppLog
{
    [Key]
    [Column("log_id")]
    public long LogId { get; set; }

    [Column("log_datetime")]
    public DateTime LogDatetime { get; set; }

    [MaxLength(20)]
    [Column("user_code")]
    public string? UserCode { get; set; }

    /// <summary>
    /// LOGIN / LOGOUT / SCAN / CANCEL / COMPLETE / ERROR
    /// </summary>
    [MaxLength(30)]
    [Column("action_type")]
    public string? ActionType { get; set; }

    [MaxLength(20)]
    [Column("screen_id")]
    public string? ScreenId { get; set; }

    /// <summary>
    /// JSON形式の詳細情報
    /// </summary>
    [Column("detail")]
    public string? Detail { get; set; }
}
