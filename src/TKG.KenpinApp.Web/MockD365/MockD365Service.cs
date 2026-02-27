using TKG.KenpinApp.Web.Models.Dto;

namespace TKG.KenpinApp.Web.MockD365;

/// <summary>
/// D365モックAPIサービス（開発用）
/// 本番環境ではD365 APIに接続する実装に差し替える
/// </summary>
public class MockD365Service : ID365Service
{
    private readonly ILogger<MockD365Service> _logger;

    /// <summary>
    /// モック社員マスタ
    /// </summary>
    private static readonly List<D365Employee> _employees = new()
    {
        new() { EmpNo = "1055", Name = "下原 太郎", Department = "物流部" },
        new() { EmpNo = "1102", Name = "山田 花子", Department = "物流部" },
        new() { EmpNo = "1203", Name = "佐藤 一郎", Department = "倉庫管理課" },
        new() { EmpNo = "1304", Name = "田中 美咲", Department = "倉庫管理課" },
        new() { EmpNo = "1405", Name = "鈴木 健太", Department = "出荷管理課" }
    };

    /// <summary>
    /// モック出荷指示データ
    /// </summary>
    private static readonly List<D365ShippingOrder> _orders = new()
    {
        // 整理番号1001 — シングルピッキング（PC）
        new()
        {
            SeiriNo = "1001",
            ShukkaDate = "2026-02-27",
            CustomerCode = "C001",
            CustomerName = "○○商事",
            KoguchiSu = 3,
            WarehouseCode = "TOA-K",
            DeliveryDate = "2026-03-01",
            ShipDate = "2026-02-28",
            CarrierName = "ヤマト運輸",
            PickType = "SINGLE",
            Status = "未検品",
            Items = new()
            {
                new() { LineNo = 1, ItemCode = "GQ41FJ", ItemName = "浄水器カートリッジA", LocationNo = "A-01", ShipQty = 10, BaraShukkaSu = 10, UnitShukkaSu = 0, UchibakoIrisu = 1, UnitIrisu = 1, JanCode = "4975373000100", KenpinCategory = "BARA" },
                new() { LineNo = 2, ItemCode = "GQ52KL", ItemName = "浄水器本体B", LocationNo = "A-02", ShipQty = 5, BaraShukkaSu = 5, UnitShukkaSu = 0, UchibakoIrisu = 1, UnitIrisu = 1, JanCode = "4975373000201", KenpinCategory = "BARA" },
                new() { LineNo = 3, ItemCode = "GQ63MN", ItemName = "シャワーヘッドC", LocationNo = "B-03", ShipQty = 3, BaraShukkaSu = 3, UnitShukkaSu = 0, UchibakoIrisu = 1, UnitIrisu = 1, JanCode = "4975373000302", KenpinCategory = "BARA" }
            }
        },
        // 整理番号1002 — トータルバラ（PC）
        new()
        {
            SeiriNo = "1002",
            ShukkaDate = "2026-02-27",
            CustomerCode = "C002",
            CustomerName = "△△工業",
            KoguchiSu = 1,
            WarehouseCode = "TOA-K",
            DeliveryDate = "2026-03-01",
            ShipDate = "2026-02-28",
            CarrierName = "佐川急便",
            PickType = "TOTAL_BARA",
            Status = "未検品",
            Items = new()
            {
                new() { LineNo = 1, ItemCode = "GQ41FJ", ItemName = "浄水器カートリッジA", LocationNo = "A-01", ShipQty = 20, BaraShukkaSu = 20, UnitShukkaSu = 0, UchibakoIrisu = 1, UnitIrisu = 1, JanCode = "4975373000100", KenpinCategory = "BARA" },
                new() { LineNo = 2, ItemCode = "GQ74PQ", ItemName = "蛇口パーツD", LocationNo = "C-04", ShipQty = 8, BaraShukkaSu = 8, UnitShukkaSu = 0, UchibakoIrisu = 1, UnitIrisu = 1, JanCode = "4975373000403", KenpinCategory = "BARA" }
            }
        },
        // 整理番号1003 — トータルケース（モバイル）
        new()
        {
            SeiriNo = "1003",
            ShukkaDate = "2026-02-27",
            CustomerCode = "C003",
            CustomerName = "◇◇物流",
            KoguchiSu = 5,
            WarehouseCode = "TSUKUBA",
            DeliveryDate = "2026-03-02",
            ShipDate = "2026-03-01",
            CarrierName = "西濃運輸",
            PickType = "TOTAL_CASE",
            Status = "未検品",
            Items = new()
            {
                new() { LineNo = 1, ItemCode = "GQ41FJ", ItemName = "浄水器カートリッジA", LocationNo = "D-02", ShipQty = 30, BaraShukkaSu = 0, UnitShukkaSu = 5, UchibakoIrisu = 6, UnitIrisu = 6, JanCode = "4975373000100", KenpinCategory = "CASE" },
                new() { LineNo = 2, ItemCode = "GQ85RS", ItemName = "フィルターセットE", LocationNo = "D-03", ShipQty = 24, BaraShukkaSu = 0, UnitShukkaSu = 4, UchibakoIrisu = 6, UnitIrisu = 6, JanCode = "4975373000504", KenpinCategory = "CASE" }
            }
        },
        // 整理番号1004〜1006 — ステータス確認用
        new()
        {
            SeiriNo = "1004",
            ShukkaDate = "2026-02-27",
            CustomerCode = "C004",
            CustomerName = "□□電機",
            KoguchiSu = 2,
            WarehouseCode = "TOA-K",
            DeliveryDate = "2026-03-01",
            ShipDate = "2026-02-28",
            CarrierName = "ヤマト運輸",
            PickType = "SINGLE",
            Status = "検品中",
            Items = new()
        },
        new()
        {
            SeiriNo = "1005",
            ShukkaDate = "2026-02-27",
            CustomerCode = "C005",
            CustomerName = "××産業",
            KoguchiSu = 1,
            WarehouseCode = "TSUKUBA",
            DeliveryDate = "2026-03-01",
            ShipDate = "2026-02-28",
            CarrierName = "佐川急便",
            PickType = "TOTAL_BARA",
            Status = "検品完了",
            Items = new()
        },
        new()
        {
            SeiriNo = "1006",
            ShukkaDate = "2026-02-26",
            CustomerCode = "C006",
            CustomerName = "☆☆商会",
            KoguchiSu = 4,
            WarehouseCode = "HORIKOSHI",
            DeliveryDate = "2026-02-28",
            ShipDate = "2026-02-27",
            CarrierName = "西濃運輸",
            PickType = "TOTAL_CASE",
            Status = "出荷済",
            Items = new()
        }
    };

    /// <summary>
    /// 拠点名マッピング
    /// </summary>
    private static readonly Dictionary<string, string> _siteNames = new()
    {
        { "TOA-K", "東亜（川崎）" },
        { "TSUKUBA", "つくば" },
        { "HORIKOSHI", "堀越" }
    };

    public MockD365Service(ILogger<MockD365Service> logger)
    {
        _logger = logger;
    }

    /// <inheritdoc/>
    public Task<D365Employee?> GetEmployeeAsync(string empNo)
    {
        _logger.LogInformation("モック: 社員マスタ照会 empNo={EmpNo}", empNo);
        var employee = _employees.FirstOrDefault(e => e.EmpNo == empNo);
        return Task.FromResult(employee);
    }

    /// <inheritdoc/>
    public Task<List<D365ShippingOrder>> GetShippingOrdersAsync(
        string? dateFrom, string? dateTo, string? status, string? siteCode)
    {
        _logger.LogInformation(
            "モック: 出荷指示一覧取得 dateFrom={DateFrom}, dateTo={DateTo}, status={Status}, siteCode={SiteCode}",
            dateFrom, dateTo, status, siteCode);

        var query = _orders.AsEnumerable();

        // 日付フィルタ
        if (!string.IsNullOrEmpty(dateFrom))
        {
            query = query.Where(o => string.Compare(o.ShukkaDate, dateFrom, StringComparison.Ordinal) >= 0);
        }
        if (!string.IsNullOrEmpty(dateTo))
        {
            query = query.Where(o => string.Compare(o.ShukkaDate, dateTo, StringComparison.Ordinal) <= 0);
        }

        // ステータスフィルタ
        if (!string.IsNullOrEmpty(status))
        {
            query = query.Where(o => o.Status == status);
        }

        // 拠点フィルタ
        if (!string.IsNullOrEmpty(siteCode))
        {
            query = query.Where(o => o.WarehouseCode == siteCode);
        }

        return Task.FromResult(query.ToList());
    }

    /// <inheritdoc/>
    public Task<D365ShippingOrder?> GetShippingOrderAsync(string seiriNo)
    {
        _logger.LogInformation("モック: 出荷指示詳細取得 seiriNo={SeiriNo}", seiriNo);
        var order = _orders.FirstOrDefault(o => o.SeiriNo == seiriNo);
        return Task.FromResult(order);
    }

    /// <inheritdoc/>
    public Task<List<InspectionItem>> GetShippingOrderItemsAsync(string seiriNo)
    {
        _logger.LogInformation("モック: 出荷指示明細取得 seiriNo={SeiriNo}", seiriNo);
        var order = _orders.FirstOrDefault(o => o.SeiriNo == seiriNo);
        return Task.FromResult(order?.Items ?? new List<InspectionItem>());
    }

    /// <inheritdoc/>
    public Task<D365ItemInfo?> LookupBarcodeAsync(string barcode)
    {
        _logger.LogInformation("モック: バーコード照合 barcode={Barcode}", barcode);

        // 全出荷指示の明細からバーコード（JANコード）で品目を検索
        var item = _orders
            .SelectMany(o => o.Items)
            .FirstOrDefault(i => i.JanCode == barcode);

        if (item == null)
            return Task.FromResult<D365ItemInfo?>(null);

        var info = new D365ItemInfo
        {
            ItemCode = item.ItemCode,
            ItemName = item.ItemName,
            JanCode = item.JanCode,
            KenpinCategory = item.KenpinCategory,
            UchibakoIrisu = item.UchibakoIrisu,
            UnitIrisu = item.UnitIrisu
        };

        return Task.FromResult<D365ItemInfo?>(info);
    }

    /// <inheritdoc/>
    public Task<PackingSlipPostResponse> PostPackingSlipAsync(string[] seiriNos, string orderType)
    {
        _logger.LogInformation(
            "モック: 梱包明細転記 seiriNos={SeiriNos}, orderType={OrderType}",
            string.Join(",", seiriNos), orderType);

        // モックでは常に成功を返す
        var response = new PackingSlipPostResponse
        {
            JobId = $"JOB-{DateTime.Now:yyyyMMddHHmmss}",
            Status = "Accepted"
        };

        return Task.FromResult(response);
    }

    /// <inheritdoc/>
    public string GetSiteName(string siteCode)
    {
        return _siteNames.TryGetValue(siteCode, out var name) ? name : siteCode;
    }
}
