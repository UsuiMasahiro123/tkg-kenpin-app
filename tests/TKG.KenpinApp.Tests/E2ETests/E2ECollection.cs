namespace TKG.KenpinApp.Tests.E2ETests;

/// <summary>
/// E2Eテストコレクション定義
/// 同一コレクション内のテストクラスは並列実行されない
/// </summary>
[CollectionDefinition("E2E", DisableParallelization = true)]
public class E2ECollection { }
