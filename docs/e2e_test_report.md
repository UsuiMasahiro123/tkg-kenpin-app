# E2Eテスト結果レポート

実行日時: 2026-02-28 テスト実行
テスト環境: ローカル（http://localhost:5079）
テストフレームワーク: xUnit + HttpClient（実サーバーへのHTTPリクエスト）

## サマリー

| 項目 | 値 |
|------|-----|
| シナリオ数 | 6件 |
| テストメソッド数 | 15件 |
| 成功 | 15件 |
| 失敗 | 0件 |
| 合計実行時間 | 2.50秒 |

## シナリオ別結果

### シナリオ1: シングルピッキング完全フロー（整理番号1001）

| # | テスト名 | 操作 | 期待結果 | 実際結果 | 判定 |
|---|---------|------|---------|---------|------|
| 1 | S1_01_Login | POST /api/auth/login | sessionToken取得 | sessionToken取得 | PASS |
| 2 | S1_02_GetOrders | GET /api/shipping/orders | 6件返却 | totalCount=6 | PASS |
| 3 | S1_03_GetOrderDetail | GET /api/shipping/orders/1001 | pickType=SINGLE | pickType=SINGLE | PASS |
| 4 | S1_04_FullSinglePickingFlow | 検品開始→全品スキャン→完了→伝票照合 | 全ステップ成功 | 全ステップ成功 | PASS |
| 5 | S1_05_LastScan_IsAllComplete | 最後のスキャンでisAllComplete=true | isAllComplete=true | isAllComplete=true | PASS |

**詳細（S1_04）:**
- Step 4: POST /api/inspection/start → sessionId取得、items=3品目
- Step 5: GQ41FJ（4975373000100）×10回スキャン → 各回addedQty=1、最終isItemComplete=true
- Step 6: GQ52KL（4975373000201）×5回スキャン → 各回addedQty=1、最終isItemComplete=true
- Step 7: GQ63MN（4975373000302）×3回スキャン → 各回addedQty=1、最終isItemComplete=true
- Step 8: 最終スキャンでisAllComplete=true確認
- Step 9: PUT /api/inspection/complete → success=true
- Step 10: POST /api/inspection/slip-verify（barcode=1001）→ result=OK

### シナリオ2: トータルバラ完全フロー（整理番号1002）

| # | テスト名 | 操作 | 期待結果 | 実際結果 | 判定 |
|---|---------|------|---------|---------|------|
| 1 | S2_FullTotalBaraFlow | 全フロー実行 | 全ステップ成功 | 全ステップ成功 | PASS |
| 2 | S2_IsAllComplete_True | 最後のスキャンでisAllComplete=true | isAllComplete=true | isAllComplete=true | PASS |

**詳細（S2_FullTotalBaraFlow）:**
- 検品開始（TOTAL_BARA）→ sessionId取得、items=2品目
- GQ41FJ（4975373000100）×20回スキャン → 各回addedQty=1、最終20/20完了
- GQ74PQ（4975373000403）×8回スキャン → 各回addedQty=1、最終8/8完了
- isAllComplete=true確認
- 検品完了 → success=true

### シナリオ3: トータルケース完全フロー（整理番号1003）

| # | テスト名 | 操作 | 期待結果 | 実際結果 | 判定 |
|---|---------|------|---------|---------|------|
| 1 | S3_FullTotalCaseFlow | 全フロー実行 | 全ステップ成功 | 全ステップ成功 | PASS |
| 2 | S3_IsAllComplete_True | 最後のスキャンでisAllComplete=true | isAllComplete=true | isAllComplete=true | PASS |

**詳細（S3_FullTotalCaseFlow）:**
- 検品開始（TOTAL_CASE）→ sessionId取得、items=2品目
- GQ41FJ（4975373000100）×5回スキャン → 各回addedQty=6（uchibako_irisu）、6×5=30/30完了
- GQ85RS（4975373000504）×4回スキャン → 各回addedQty=6、6×4=24/24完了
- isAllComplete=true確認
- 検品完了 → success=true

### シナリオ4: エラー系テスト

| # | テスト名 | 操作 | 期待結果 | 実際結果 | 判定 |
|---|---------|------|---------|---------|------|
| 1 | S4_01_UnknownBarcode | 存在しないバーコードでスキャン | E-KNP-001 or E-KNP-002 | 400 + E-KNP-001 | PASS |
| 2 | S4_02_OverQuantity | 出荷数完了後に追加スキャン | E-KNP-003 or E-KNP-004 | 400 + E-KNP-004 | PASS |
| 3 | S4_03_InvalidSession | 無効セッショントークン | 401 + E-AUTH-003 | 401 + E-AUTH-003 | PASS |
| 4 | S4_04_DoubleLock | 同一整理番号の二重ロック | 400 + E-LOCK-001 | 400 + E-LOCK-001 | PASS |

### シナリオ5: 中断・再開フロー（整理番号1004）

| # | テスト名 | 操作 | 期待結果 | 実際結果 | 判定 |
|---|---------|------|---------|---------|------|
| 1 | S5_PauseAndResumeFlow | 全フロー実行 | 全ステップ成功 | 全ステップ成功 | PASS |

**詳細:**
- 検品開始（SINGLE）→ sessionId取得
- GQ41FJ×3回スキャン（途中: 3/5）
- PUT /api/inspection/pause → success=true
- 再度 POST /api/inspection/start（同一整理番号）→ 同一sessionId（再開確認）
- GQ41FJ 4回目スキャン → newScannedQty=4（検品数引継ぎ確認）
- 残りGQ41FJ 1回 + GQ52KL 2回スキャン → isAllComplete=true
- 検品完了 → success=true

### シナリオ6: 取消テスト（整理番号1005）

| # | テスト名 | 操作 | 期待結果 | 実際結果 | 判定 |
|---|---------|------|---------|---------|------|
| 1 | S6_CancelScanFlow | 全フロー実行 | 全ステップ成功 | 全ステップ成功 | PASS |

**詳細:**
- 検品開始（TOTAL_BARA）→ sessionId取得
- GQ74PQ×2回スキャン → newScannedQty=2
- DELETE /api/inspection/scan（直前取消）→ undoneItemCode=GQ74PQ、newScannedQty=1
- 検品数が2→1に減算されたことを確認
- 残りGQ74PQ×11回スキャン → 12/12完了、isAllComplete=true
- 検品完了 → success=true

## テスト実行ログ

```
テストの合計数: 15
     成功: 15
合計時間: 2.4957 秒

成功 Scenario1_SinglePickingTests.S1_01_Login_ReturnsSessionToken [< 1 ms]
成功 Scenario1_SinglePickingTests.S1_02_GetOrders_Returns6Items [2 ms]
成功 Scenario1_SinglePickingTests.S1_03_GetOrderDetail_Returns3Items [2 ms]
成功 Scenario1_SinglePickingTests.S1_04_FullSinglePickingFlow [222 ms]
成功 Scenario1_SinglePickingTests.S1_05_LastScan_IsAllComplete_True [149 ms]
成功 Scenario2_TotalBaraTests.S2_FullTotalBaraFlow [220 ms]
成功 Scenario2_TotalBaraTests.S2_IsAllComplete_True_OnLastScan [184 ms]
成功 Scenario3_TotalCaseTests.S3_FullTotalCaseFlow [46 ms]
成功 Scenario3_TotalCaseTests.S3_IsAllComplete_True_OnLastScan [53 ms]
成功 Scenario4_ErrorTests.S4_01_UnknownBarcode_ReturnsError [13 ms]
成功 Scenario4_ErrorTests.S4_02_OverQuantity_ReturnsError [45 ms]
成功 Scenario4_ErrorTests.S4_03_InvalidSession_Returns401 [5 ms]
成功 Scenario4_ErrorTests.S4_04_DoubleLock_ReturnsLockConflict [13 ms]
成功 Scenario5_PauseResumeTests.S5_PauseAndResumeFlow [77 ms]
成功 Scenario6_CancelTests.S6_CancelScanFlow [108 ms]
```
