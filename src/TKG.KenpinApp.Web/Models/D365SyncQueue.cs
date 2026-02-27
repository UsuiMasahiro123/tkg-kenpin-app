using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace TKG.KenpinApp.Web.Models;

/// <summary>
/// D365連携リトライキュー
/// D365連携が失敗した場合にリトライ対象として登録される
/// </summary>
[Table("T_D365_SYNC_QUEUE")]
public class D365SyncQueue
{
    [Key]
    [Column("queue_id")]
    public long QueueId { get; set; }

    /// <summary>
    /// 対象セッションID
    /// </summary>
    [Column("session_id")]
    public long SessionId { get; set; }

    /// <summary>
    /// 連携種別: KENPIN_RESULT / STATUS_UPDATE
    /// </summary>
    [Required]
    [MaxLength(30)]
    [Column("sync_type")]
    public string SyncType { get; set; } = string.Empty;

    /// <summary>
    /// JSON形式のリクエストデータ
    /// </summary>
    [Column("payload")]
    public string Payload { get; set; } = string.Empty;

    /// <summary>
    /// PENDING / PROCESSING / SUCCESS / FAILED
    /// </summary>
    [Required]
    [MaxLength(20)]
    [Column("status")]
    public string Status { get; set; } = "PENDING";

    /// <summary>
    /// 現在のリトライ回数
    /// </summary>
    [Column("retry_count")]
    public int RetryCount { get; set; } = 0;

    /// <summary>
    /// 最大リトライ回数
    /// </summary>
    [Column("max_retries")]
    public int MaxRetries { get; set; } = 3;

    /// <summary>
    /// 次回リトライ予定日時
    /// </summary>
    [Column("next_retry_at")]
    public DateTime? NextRetryAt { get; set; }

    /// <summary>
    /// 最後のエラーメッセージ
    /// </summary>
    [Column("last_error")]
    public string? LastError { get; set; }

    [Column("created_at")]
    public DateTime CreatedAt { get; set; } = DateTime.Now;

    /// <summary>
    /// 連携完了日時
    /// </summary>
    [Column("completed_at")]
    public DateTime? CompletedAt { get; set; }

    // ナビゲーションプロパティ
    [ForeignKey("SessionId")]
    public KenpinSession? Session { get; set; }
}
