using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace TKG.KenpinApp.Web.Models;

/// <summary>
/// 検品セッション
/// </summary>
[Table("T_KENPIN_SESSION")]
public class KenpinSession
{
    [Key]
    [Column("session_id")]
    public long SessionId { get; set; }

    [Required]
    [MaxLength(20)]
    [Column("seiri_no")]
    public string SeiriNo { get; set; } = string.Empty;

    [Column("shukka_date")]
    public DateTime ShukkaDate { get; set; }

    /// <summary>
    /// SINGLE / TOTAL_BARA / TOTAL_CASE
    /// </summary>
    [Required]
    [MaxLength(20)]
    [Column("kenpin_type")]
    public string KenpinType { get; set; } = string.Empty;

    [Required]
    [MaxLength(20)]
    [Column("user_code")]
    public string UserCode { get; set; } = string.Empty;

    [MaxLength(100)]
    [Column("user_name")]
    public string? UserName { get; set; }

    /// <summary>
    /// TOA-K / TSUKUBA / HORIKOSHI
    /// </summary>
    [Required]
    [MaxLength(10)]
    [Column("site_code")]
    public string SiteCode { get; set; } = string.Empty;

    [MaxLength(20)]
    [Column("warehouse_code")]
    public string? WarehouseCode { get; set; }

    [Column("total_items")]
    public int? TotalItems { get; set; }

    [Column("total_qty")]
    public int? TotalQty { get; set; }

    [Column("scanned_qty")]
    public int ScannedQty { get; set; } = 0;

    /// <summary>
    /// SCANNING / PAUSED / COMPLETED / CANCELLED
    /// </summary>
    [Required]
    [MaxLength(20)]
    [Column("status")]
    public string Status { get; set; } = string.Empty;

    [Column("started_at")]
    public DateTime StartedAt { get; set; }

    [Column("completed_at")]
    public DateTime? CompletedAt { get; set; }

    [Column("d365_synced")]
    public bool D365Synced { get; set; } = false;

    [Column("created_at")]
    public DateTime CreatedAt { get; set; } = DateTime.Now;

    [Column("updated_at")]
    public DateTime UpdatedAt { get; set; } = DateTime.Now;

    // ナビゲーションプロパティ
    public ICollection<KenpinDetail> Details { get; set; } = new List<KenpinDetail>();
    public ICollection<DenpyoKenpin> DenpyoKenpins { get; set; } = new List<DenpyoKenpin>();
}
