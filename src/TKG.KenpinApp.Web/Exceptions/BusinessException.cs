namespace TKG.KenpinApp.Web.Exceptions;

/// <summary>
/// 業務例外クラス
/// APIで発生する業務エラーを表す（400 Bad Request として返却）
/// </summary>
public class BusinessException : Exception
{
    /// <summary>
    /// エラーコード（E-KNP-001等）
    /// </summary>
    public string Code { get; }

    public BusinessException(string code, string message) : base(message)
    {
        Code = code;
    }

    public BusinessException(string code, string message, Exception innerException) : base(message, innerException)
    {
        Code = code;
    }
}
