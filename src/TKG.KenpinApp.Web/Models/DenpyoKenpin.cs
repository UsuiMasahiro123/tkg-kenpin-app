using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace TKG.KenpinApp.Web.Models;

/// <summary>
/// 伝票検品実績
/// </summary>
[Table("T_DENPYO_KENPIN")]
public class DenpyoKenpin
{
    [Key]
    [Column("denpyo_id")]
    public long DenpyoId { get; set; }

    [Column("session_id")]
    public long SessionId { get; set; }

    /// <summary>
    /// NOUHINSHO / OKURIJOU
    /// </summary>
    [MaxLength(20)]
    [Column("denpyo_type")]
    public string? DenpyoType { get; set; }

    [MaxLength(50)]
    [Column("barcode")]
    public string? Barcode { get; set; }

    [Column("verified_at")]
    public DateTime? VerifiedAt { get; set; }

    /// <summary>
    /// OK / NG
    /// </summary>
    [MaxLength(10)]
    [Column("result")]
    public string? Result { get; set; }

    // ナビゲーションプロパティ
    [ForeignKey("SessionId")]
    public KenpinSession? Session { get; set; }
}
