# TKG検品アプリ テスト結果レポート

実行日時: 2026-02-27 22:50
テスト環境: ローカル（InMemoryDatabase）
テストフレームワーク: xUnit 2.5.3 + WebApplicationFactory (ASP.NET Core 8.0)

## サマリー

- 合計: 29件
- 成功: 29件
- 失敗: 0件
- スキップ: 0件

## 詳細結果

### 認証テスト (AuthTests)

| No | テストケース | 結果 | 実行時間 | 備考 |
|----|-------------|------|---------|------|
| T01 | 正常ログイン（1055, TOA-K） | PASS | 1.0s | sessionToken, userName, siteName返却確認 |
| T02 | 存在しない社員番号でログイン | PASS | 217ms | E-AUTH-002 エラーコード確認 |
| T03 | 社員番号空でログイン | PASS | 95ms | E-AUTH-001 エラーコード確認 |
| T04 | ログアウト | PASS | 177ms | success=true 確認 |

### 出荷管理テスト (ShippingTests)

| No | テストケース | 結果 | 実行時間 | 備考 |
|----|-------------|------|---------|------|
| T05 | 出荷対象一覧取得（全件） | PASS | 1.0s | 6件返却確認 |
| T06 | 出荷対象一覧取得（拠点フィルタ: TOA-K） | PASS | 135ms | TOA-Kのみ返却確認 |
| T07 | 出荷指示詳細取得（1001） | PASS | 146ms | SeiriNo, PickType確認 |
| T08 | 存在しない整理番号で詳細取得 | PASS | 205ms | 404 Not Found確認 |

### 検品フローテスト (InspectionTests) — 最重要

| No | テストケース | 結果 | 実行時間 | 備考 |
|----|-------------|------|---------|------|
| T09 | シングルピッキング: 検品開始 | PASS | 75ms | sessionId > 0, items=3件 |
| T10 | シングルピッキング: スキャン1回 | PASS | 66ms | addedQty=1, newScannedQty=1 |
| T11 | シングルピッキング: 全品完了 | PASS | 177ms | isAllComplete=true（18回スキャン） |
| T12 | トータルバラ: スキャン | PASS | 78ms | addedQty=1 |
| T13 | トータルケース: スキャン | PASS | 68ms | addedQty=6（uchibako_irisu=6） |
| T14 | トータルケース: 端数処理 | PASS | 140ms | 5回目スキャンで残6→6加算、完了 |
| T15 | スキャン取消 | PASS | 91ms | newScannedQty=1（2→1に戻る） |
| T16 | 検品中断 | PASS | 116ms | success=true |
| T17 | 中断後再開 | PASS | 117ms | 同一sessionId、検品数引継ぎ |
| T18 | 検品完了 → D365連携 | PASS | 95ms | d365SyncStatus=Synced |
| T19 | 存在しないバーコードスキャン | PASS | 75ms | E-KNP-001 確認 |
| T20 | 出荷数超過スキャン | PASS | 113ms | E-KNP-003/E-KNP-004 確認 |
| T21 | 完了済品目の再スキャン | PASS | 92ms | E-KNP-004 確認 |
| T22 | 伝票照合（正しいバーコード） | PASS | 1.0s | result=OK |
| T23 | 伝票照合（不正バーコード） | PASS | 119ms | result=NG |

### 排他制御テスト (LockTests)

| No | テストケース | 結果 | 実行時間 | 備考 |
|----|-------------|------|---------|------|
| T24 | ロック取得 | PASS | 69ms | IsLocked=true確認 |
| T25 | 同じ整理番号の二重ロック | PASS | 87ms | E-LOCK-001 確認 |
| T26 | 検品完了後のロック解放 | PASS | 1.0s | IsLocked=false確認 |
| T27 | ステータス不正遷移（COMPLETED→SCANNING） | PASS | 100ms | E-KNP-005 確認 |

### エラーハンドリングテスト (ErrorHandlingTests)

| No | テストケース | 結果 | 実行時間 | 備考 |
|----|-------------|------|---------|------|
| T28 | 無効なセッショントークンでAPI呼出 | PASS | 1.0s | 401, E-AUTH-003 確認 |
| T29 | 不正なリクエストBody | PASS | 231ms | 400 Bad Request 確認 |

## テスト実行時に発見・修正したバグ

### BUG-001: トータルケース検品時の加算数量不正

- **発見テスト**: T13, T14
- **概要**: `InspectionService.ScanAsync` でトータルケース検品時の加算数量を `LookupBarcodeAsync` の結果（全注文横断で最初にヒットした品目の `UchibakoIrisu`）から取得していた
- **影響**: 同一品目が複数注文にまたがり、注文ごとに内箱入数が異なる場合に不正な加算数量となる
- **修正**: `targetItem.UchibakoIrisu`（注文固有の出荷明細の値）を使用するよう修正
- **ファイル**: `src/TKG.KenpinApp.Web/Services/InspectionService.cs` L234

## 手動テスト対象（UI操作が必要）

以下のテストケースはUI操作が必要なため、自動テストの対象外です。
別途手動テストで確認してください。

| No | テストケース | 確認ポイント |
|----|-------------|-------------|
| M01 | PC版ログイン画面表示・操作 | 画面遷移、入力バリデーション |
| M02 | PC版出荷一覧画面のフィルタ操作 | 絞り込み、ページング |
| M03 | PC版検品画面のバーコードスキャン | スキャナ入力、進捗表示 |
| M04 | モバイル版ログイン画面 | レスポンシブ表示 |
| M05 | モバイル版検品画面 | タッチ操作、PDAスキャン |
| M06 | オフライン時のエラー表示 | ネットワーク断時の挙動 |
| M07 | 複数ブラウザでの排他制御確認 | ロック競合メッセージ表示 |
| M08 | セッションタイムアウト時の挙動 | 自動ログアウト、再認証 |
| M09 | D365連携失敗時のリトライ確認 | リトライキュー動作 |

## テスト環境情報

- .NET SDK: 8.0.x
- OS: Windows 11 Pro
- テストDB: InMemoryDatabase（テストごとに独立したインスタンス）
- D365モック失敗率: 0%（テスト安定性のため無効化）
- バックグラウンドサービス: 無効化（LockTimeoutService, D365SyncRetryService）
