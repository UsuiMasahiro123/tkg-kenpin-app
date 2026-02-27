using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace TKG.KenpinApp.Web.Models;

/// <summary>
/// 検品実績明細
/// </summary>
[Table("T_KENPIN_DETAIL")]
public class KenpinDetail
{
    [Key]
    [Column("detail_id")]
    public long DetailId { get; set; }

    [Column("session_id")]
    public long SessionId { get; set; }

    [Required]
    [MaxLength(50)]
    [Column("item_code")]
    public string ItemCode { get; set; } = string.Empty;

    [Required]
    [MaxLength(50)]
    [Column("barcode")]
    public string Barcode { get; set; } = string.Empty;

    /// <summary>
    /// BARA / CASE
    /// </summary>
    [MaxLength(20)]
    [Column("kenpin_category")]
    public string? KenpinCategory { get; set; }

    [Column("scan_qty")]
    public int ScanQty { get; set; } = 1;

    [Column("scan_datetime")]
    public DateTime ScanDatetime { get; set; }

    /// <summary>
    /// PC / PDA
    /// </summary>
    [MaxLength(10)]
    [Column("scan_method")]
    public string? ScanMethod { get; set; }

    [Column("cancel_flg")]
    public bool CancelFlg { get; set; } = false;

    [Column("cancel_datetime")]
    public DateTime? CancelDatetime { get; set; }

    // ナビゲーションプロパティ
    [ForeignKey("SessionId")]
    public KenpinSession? Session { get; set; }
}
