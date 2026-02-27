# TKG 出荷管理検品アプリ

TKG出荷管理業務の検品工程をデジタル化するWebアプリケーションです。

## 技術スタック

- **フロントエンド**: ASP.NET Razor Pages + HTML5/CSS3/JavaScript
- **バックエンド**: ASP.NET Core Web API (.NET 8)
- **データベース**: SQLite（開発環境） / Azure SQL Database（本番環境）
- **UIフレームワーク**: Material Design 3（MudBlazor）

## セットアップ手順

### 前提条件

- .NET 8 SDK
- dotnet-ef ツール (`dotnet tool install --global dotnet-ef`)

### 起動方法

```bash
# パッケージ復元
dotnet restore

# DBマイグレーション適用
dotnet ef database update --project src/TKG.KenpinApp.Web

# アプリケーション起動
dotnet run --project src/TKG.KenpinApp.Web
```

起動後、ブラウザで `https://localhost:5001/swagger` にアクセスするとSwagger UIが表示されます。

## プロジェクト構成

```
tkg-kenpin-app/
├── src/
│   └── TKG.KenpinApp.Web/       # Webアプリケーション
│       ├── Controllers/          # APIコントローラー
│       ├── Models/               # DTOとドメインモデル
│       ├── Services/             # ビジネスロジック
│       ├── Data/                 # DbContext, Migrations
│       ├── MockD365/             # D365モックAPIサービス
│       └── Pages/                # Razor Pages
├── tests/                        # テストプロジェクト
├── docs/                         # 設計書
└── azure/                        # Azure設定
```

## モック社員データ

| 社員番号 | 氏名 | 部署 |
|----------|------|------|
| 1055 | 下原 太郎 | 物流部 |
| 1102 | 山田 花子 | 物流部 |
| 1203 | 佐藤 一郎 | 倉庫管理課 |
| 1304 | 田中 美咲 | 倉庫管理課 |
| 1405 | 鈴木 健太 | 出荷管理課 |
