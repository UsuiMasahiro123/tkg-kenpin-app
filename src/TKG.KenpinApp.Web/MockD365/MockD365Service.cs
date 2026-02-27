using System.Text.Json;
using System.Text.Json.Serialization;
using TKG.KenpinApp.Web.Models.Dto;

namespace TKG.KenpinApp.Web.MockD365;

/// <summary>
/// D365モックAPIサービス（開発用）
/// JSONファイルからデータを読み込む。ReloadData()で再読込可能。
/// </summary>
public class MockD365Service : ID365Service
{
    private readonly ILogger<MockD365Service> _logger;
    private readonly IConfiguration _config;
    private readonly string _dataPath;
    private readonly Random _random = new();
    private readonly object _lockObj = new();

    private List<D365Employee> _employees = new();
    private List<D365ShippingOrder> _orders = new();
    private Dictionary<string, List<MockShippingItem>> _itemsMap = new();

    /// <summary>
    /// 拠点名マッピング
    /// </summary>
    private static readonly Dictionary<string, string> _siteNames = new()
    {
        { "TOA-K", "東亜（川崎）" },
        { "TSUKUBA", "つくば" },
        { "HORIKOSHI", "堀越" }
    };

    private static readonly JsonSerializerOptions _jsonOptions = new()
    {
        PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
        ReadCommentHandling = JsonCommentHandling.Skip
    };

    public MockD365Service(ILogger<MockD365Service> logger, IConfiguration config, IWebHostEnvironment env)
    {
        _logger = logger;
        _config = config;
        _dataPath = Path.Combine(env.ContentRootPath, "MockData");
        LoadData();
    }

    /// <summary>
    /// 外部JSONファイルからデータを読み込む
    /// </summary>
    private void LoadData()
    {
        lock (_lockObj)
        {
            try
            {
                // 社員マスタ
                var empJson = File.ReadAllText(Path.Combine(_dataPath, "employees.json"));
                var empList = JsonSerializer.Deserialize<List<MockEmployee>>(empJson, _jsonOptions) ?? new();
                _employees = empList.Select(e => new D365Employee
                {
                    EmpNo = e.EmpNo,
                    Name = e.Name,
                    Department = e.Department
                }).ToList();

                // 出荷指示明細マップ
                var itemsJson = File.ReadAllText(Path.Combine(_dataPath, "shipping_items.json"));
                _itemsMap = JsonSerializer.Deserialize<Dictionary<string, List<MockShippingItem>>>(itemsJson, _jsonOptions) ?? new();

                // 出荷指示ヘッダー
                var ordersJson = File.ReadAllText(Path.Combine(_dataPath, "shipping_orders.json"));
                var orderList = JsonSerializer.Deserialize<List<MockShippingOrder>>(ordersJson, _jsonOptions) ?? new();
                _orders = orderList.Select(o =>
                {
                    var items = _itemsMap.TryGetValue(o.SeiriNo, out var itemList)
                        ? itemList.Select(MapToInspectionItem).ToList()
                        : new List<InspectionItem>();

                    return new D365ShippingOrder
                    {
                        SeiriNo = o.SeiriNo,
                        ShukkaDate = o.ShukkaDate,
                        CustomerCode = o.CustomerCode,
                        CustomerName = o.CustomerName,
                        KoguchiSu = o.KoguchiSu,
                        WarehouseCode = o.WarehouseCode,
                        DeliveryDate = o.DeliveryDate,
                        ShipDate = o.ShipDate,
                        CarrierName = o.CarrierName,
                        PickType = o.PickType,
                        Status = o.Status,
                        Items = items
                    };
                }).ToList();

                _logger.LogInformation(
                    "モックデータ読込完了: 社員={EmpCount}件, 出荷指示={OrderCount}件",
                    _employees.Count, _orders.Count);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "モックデータ読込エラー: path={DataPath}", _dataPath);
                throw;
            }
        }
    }

    /// <summary>
    /// データリロード（開発用: サーバー再起動不要）
    /// </summary>
    public void ReloadData() => LoadData();

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

        if (!string.IsNullOrEmpty(dateFrom))
        {
            query = query.Where(o => string.Compare(o.ShukkaDate, dateFrom, StringComparison.Ordinal) >= 0);
        }
        if (!string.IsNullOrEmpty(dateTo))
        {
            query = query.Where(o => string.Compare(o.ShukkaDate, dateTo, StringComparison.Ordinal) <= 0);
        }
        if (!string.IsNullOrEmpty(status))
        {
            query = query.Where(o => o.Status == status);
        }
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

    /// <inheritdoc/>
    public Task SyncInspectionResultAsync(long sessionId, string payload)
    {
        _logger.LogInformation("モック: D365検品結果連携 sessionId={SessionId}", sessionId);

        var failureRate = _config.GetValue<int>("MockD365:FailureRatePercent", 20);

        if (_random.Next(100) < failureRate)
        {
            _logger.LogWarning("モック: D365連携失敗シミュレーション sessionId={SessionId}", sessionId);
            throw new Exception($"D365 API呼出エラー（モック失敗シミュレーション: {failureRate}%）");
        }

        _logger.LogInformation("モック: D365連携成功 sessionId={SessionId}", sessionId);
        return Task.CompletedTask;
    }

    private static InspectionItem MapToInspectionItem(MockShippingItem m) => new()
    {
        LineNo = m.LineNo,
        ItemCode = m.ItemCode,
        ItemName = m.ItemName,
        LocationNo = m.LocationNo,
        ShipQty = m.ShipQty,
        BaraShukkaSu = m.BaraShukkaSu,
        UnitShukkaSu = m.UnitShukkaSu,
        UchibakoIrisu = m.UchibakoIrisu,
        UnitIrisu = m.UnitIrisu,
        JanCode = m.JanCode,
        KenpinCategory = m.KenpinCategory
    };
}

#region JSON逆シリアライズ用内部モデル

internal class MockEmployee
{
    public string EmpNo { get; set; } = string.Empty;
    public string Name { get; set; } = string.Empty;
    public string Department { get; set; } = string.Empty;
}

internal class MockShippingOrder
{
    public string SeiriNo { get; set; } = string.Empty;
    public string ShukkaDate { get; set; } = string.Empty;
    public string CustomerCode { get; set; } = string.Empty;
    public string CustomerName { get; set; } = string.Empty;
    public int KoguchiSu { get; set; }
    public string WarehouseCode { get; set; } = string.Empty;
    public string DeliveryDate { get; set; } = string.Empty;
    public string ShipDate { get; set; } = string.Empty;
    public string CarrierName { get; set; } = string.Empty;
    public string PickType { get; set; } = string.Empty;
    public string Status { get; set; } = string.Empty;
}

internal class MockShippingItem
{
    public int LineNo { get; set; }
    public string ItemCode { get; set; } = string.Empty;
    public string ItemName { get; set; } = string.Empty;
    public string LocationNo { get; set; } = string.Empty;
    public int ShipQty { get; set; }
    public int BaraShukkaSu { get; set; }
    public int UnitShukkaSu { get; set; }
    [JsonPropertyName("uchibako_irisu")]
    public int UchibakoIrisu { get; set; }
    [JsonPropertyName("unit_irisu")]
    public int UnitIrisu { get; set; }
    public string JanCode { get; set; } = string.Empty;
    public string KenpinCategory { get; set; } = string.Empty;
}

#endregion
