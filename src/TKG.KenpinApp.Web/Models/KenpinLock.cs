using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace TKG.KenpinApp.Web.Models;

/// <summary>
/// 排他ロック管理
/// </summary>
[Table("T_KENPIN_LOCK")]
public class KenpinLock
{
    [Key]
    [Column("lock_id")]
    public long LockId { get; set; }

    [Required]
    [MaxLength(20)]
    [Column("seiri_no")]
    public string SeiriNo { get; set; } = string.Empty;

    [Column("shukka_date")]
    public DateTime ShukkaDate { get; set; }

    [Required]
    [MaxLength(20)]
    [Column("locked_by")]
    public string LockedBy { get; set; } = string.Empty;

    [Column("locked_at")]
    public DateTime LockedAt { get; set; }

    /// <summary>
    /// ロック取得から30分後
    /// </summary>
    [Column("timeout_at")]
    public DateTime TimeoutAt { get; set; }

    [Column("released_at")]
    public DateTime? ReleasedAt { get; set; }
}
