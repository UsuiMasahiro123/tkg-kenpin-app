# TKG検品アプリ Azure デプロイ手順書

## 1. 前提条件

- Azure CLI インストール済み（`az --version` で確認）
- Azure サブスクリプション有効
- .NET 8 SDK インストール済み（`dotnet --version` で確認）
- Free App Service のクォータが1以上（Azure ポータル → クォータ で確認）

## 2. 初回セットアップ

### 2-1. Azure CLI 認証

```bash
# 認証状態確認
az account show

# 未認証の場合（ブラウザが開くので認証）
az login

# サブスクリプション一覧
az account list --output table

# 使用するサブスクリプションを選択（複数ある場合）
az account set --subscription "<サブスクリプション名またはID>"
```

### 2-2. リソースグループ作成

```bash
az group create --name rg-tkg-kenpin-sim --location japaneast
```

### 2-3. App Service Plan 作成

```bash
az appservice plan create \
  --name plan-tkg-kenpin-sim \
  --resource-group rg-tkg-kenpin-sim \
  --sku F1
```

> **注意:** F1（無料）プランが使えない場合はクォータ増加をリクエストするか、B1（Basic）に変更してください。

### 2-4. App Service 作成

```bash
az webapp create \
  --name tkg-kenpin-app \
  --resource-group rg-tkg-kenpin-sim \
  --plan plan-tkg-kenpin-sim \
  --runtime "dotnet:8"
```

> `tkg-kenpin-app` が既に使われている場合は末尾にランダム文字列を付与してください（例: `tkg-kenpin-app-a1b2`）。

### 2-5. アプリ設定

```bash
az webapp config appsettings set \
  --name tkg-kenpin-app \
  --resource-group rg-tkg-kenpin-sim \
  --settings \
    ASPNETCORE_ENVIRONMENT=Production \
    DOTNET_ENVIRONMENT=Production
```

## 3. デプロイ手順

### 3-1. ビルド

```bash
cd src/TKG.KenpinApp.Web
dotnet publish -c Release -o ./publish
```

### 3-2. ZIPパッケージ作成

**PowerShell の場合:**

```powershell
cd publish
Compress-Archive -Path * -DestinationPath ../deploy.zip -Force
cd ..
```

**bash / Git Bash の場合:**

```bash
cd publish
zip -r ../deploy.zip .
cd ..
```

### 3-3. デプロイ実行

```bash
az webapp deploy \
  --name tkg-kenpin-app \
  --resource-group rg-tkg-kenpin-sim \
  --src-path deploy.zip \
  --type zip
```

### 3-4. 動作確認

```bash
# アプリURLを取得
az webapp show \
  --name tkg-kenpin-app \
  --resource-group rg-tkg-kenpin-sim \
  --query defaultHostName -o tsv
```

表示されたURL（`https://tkg-kenpin-app.azurewebsites.net`）にブラウザでアクセスし、以下を確認:

1. ログイン画面が表示される
2. 社員番号 `1055`、拠点 `TOA-K` でログインできる
3. 出荷対象一覧が表示される
4. 検品開始→バーコードスキャン→検品完了ができる
5. 完了音が鳴る

> **注意:** F1プランは初回アクセス時にコールドスタートで30秒程度かかることがあります。

## 4. 更新デプロイ（2回目以降）

コードを変更した場合は、ビルド＆デプロイのみ実行します。

```bash
cd src/TKG.KenpinApp.Web

# ビルド
dotnet publish -c Release -o ./publish

# ZIPパッケージ作成（PowerShell）
cd publish
Compress-Archive -Path * -DestinationPath ../deploy.zip -Force
cd ..

# デプロイ
az webapp deploy \
  --name tkg-kenpin-app \
  --resource-group rg-tkg-kenpin-sim \
  --src-path deploy.zip \
  --type zip
```

## 5. リソース削除（検証終了時）

**重要: 検証終了後は必ずリソースを削除してください。**

```bash
az group delete --name rg-tkg-kenpin-sim --yes --no-wait
```

リソースグループを削除すると、配下のApp Service Plan・App Serviceもすべて削除されます。

## 6. 本番環境への移行時の変更点

### 6-1. Azure SQL Database への切替

SQLiteからAzure SQL Databaseに切り替える場合:

1. Azure SQL Database リソースを作成
2. NuGetパッケージを変更:
   - `Microsoft.EntityFrameworkCore.Sqlite` → `Microsoft.EntityFrameworkCore.SqlServer`
3. 接続文字列を変更:
   ```json
   {
     "ConnectionStrings": {
       "DefaultConnection": "Server=tcp:<server>.database.windows.net,1433;Database=kenpin;..."
     }
   }
   ```
4. Program.cs の `UseSqlite()` を `UseSqlServer()` に変更
5. マイグレーションを再生成

### 6-2. D365 API 接続設定

モックサービスから実際のD365 API接続に切り替える場合:

1. `ID365Service` の実装クラスを実D365 API接続用に作成
2. DI登録を `MockD365Service` から実装クラスに変更
3. Azure App Service のアプリ設定にD365接続情報を登録:
   - `D365__BaseUrl`
   - `D365__TenantId`
   - `D365__ClientId`
   - `D365__ClientSecret`

### 6-3. 認証方式の変更（Azure AD統合等）

1. Microsoft.Identity.Web パッケージを追加
2. Azure AD認証の設定を追加
3. 現在の社員番号＋拠点ログインをAzure AD認証に置き換え

### 6-4. App Service Plan のスケールアップ

| 項目 | 検証環境（現在） | 本番環境（推奨） |
|------|-----------------|-----------------|
| SKU | F1 (Free) | S1 以上 (Standard) |
| カスタムドメイン | なし | 設定推奨 |
| SSL | Azure既定 | カスタムSSL証明書 |
| スケーリング | 1インスタンス | オートスケール |
| バックアップ | なし | 自動バックアップ設定 |

## トラブルシューティング

### アプリが起動しない場合

```bash
# ログをリアルタイムで確認
az webapp log tail \
  --name tkg-kenpin-app \
  --resource-group rg-tkg-kenpin-sim
```

### よくあるエラーと対処

| エラー | 原因 | 対処 |
|--------|------|------|
| 500 Internal Server Error | DB初期化失敗 | ログ確認。MockDataフォルダがpublishに含まれているか確認 |
| 404 | ルーティング不備 | UseStaticFiles()とMapRazorPages()の順序確認 |
| 30秒タイムアウト | F1コールドスタート | 初回は時間がかかる。再アクセスで解消 |
| Application Error | ランタイム不一致 | `--runtime "dotnet:8"` で作成しているか確認 |

### F1プラン作成時のクォータエラー

```
ERROR: Operation cannot be completed without additional quota.
```

Azure ポータルでクォータ増加をリクエスト:
1. Azure ポータル → 検索「クォータ」→ コンピューティング
2. リージョンフィルターで「Japan East」を選択
3. 「Free App Service」の行をチェック → クォータ調整リクエスト
4. 新しい制限値に「1」を入力して送信
