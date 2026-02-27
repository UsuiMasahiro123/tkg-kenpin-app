using Microsoft.AspNetCore.Mvc;
using TKG.KenpinApp.Web.Data;
using TKG.KenpinApp.Web.Models.Dto;
using TKG.KenpinApp.Web.MockD365;

namespace TKG.KenpinApp.Web.Controllers;

/// <summary>
/// 出荷管理APIコントローラー
/// </summary>
[ApiController]
[Route("api/shipping")]
public class ShippingController : ControllerBase
{
    private readonly ID365Service _d365;
    private readonly KenpinDbContext _db;
    private readonly ILogger<ShippingController> _logger;

    public ShippingController(ID365Service d365, KenpinDbContext db, ILogger<ShippingController> logger)
    {
        _d365 = d365;
        _db = db;
        _logger = logger;
    }

    /// <summary>
    /// API-3: 出荷対象一覧取得
    /// </summary>
    [HttpGet("orders")]
    public async Task<ActionResult<ShippingOrderListResponse>> GetOrders(
        [FromQuery] string? dateFrom,
        [FromQuery] string? dateTo,
        [FromQuery] string? status,
        [FromQuery] string? siteCode,
        [FromQuery] int page = 1,
        [FromQuery] int pageSize = 20)
    {
        var orders = await _d365.GetShippingOrdersAsync(dateFrom, dateTo, status, siteCode);

        var totalCount = orders.Count;
        var pagedOrders = orders
            .Skip((page - 1) * pageSize)
            .Take(pageSize)
            .Select(o => new ShippingOrderSummary
            {
                SeiriNo = o.SeiriNo,
                KoguchiSu = o.KoguchiSu,
                CustomerCode = o.CustomerCode,
                CustomerName = o.CustomerName,
                WarehouseCode = o.WarehouseCode,
                DeliveryDate = o.DeliveryDate,
                ShipDate = o.ShipDate,
                CarrierName = o.CarrierName,
                PickType = o.PickType,
                Status = o.Status,
                Note = o.Note
            })
            .ToList();

        return Ok(new ShippingOrderListResponse
        {
            TotalCount = totalCount,
            Items = pagedOrders
        });
    }

    /// <summary>
    /// API-4: 出荷指示詳細取得
    /// </summary>
    [HttpGet("orders/{seiriNo}")]
    public async Task<ActionResult<ShippingOrderDetailResponse>> GetOrderDetail(
        string seiriNo,
        [FromQuery] string? shukkaDate)
    {
        var order = await _d365.GetShippingOrderAsync(seiriNo);
        if (order == null)
        {
            return NotFound(new { error = "出荷指示が見つかりません" });
        }

        // ロック状態を確認
        var now = DateTime.Now;
        var activeLock = _db.KenpinLocks
            .Where(l => l.SeiriNo == seiriNo && l.ReleasedAt == null && l.TimeoutAt > now)
            .FirstOrDefault();

        return Ok(new ShippingOrderDetailResponse
        {
            SeiriNo = order.SeiriNo,
            CustomerName = order.CustomerName,
            KoguchiSu = order.KoguchiSu,
            PickType = order.PickType,
            WarehouseCode = order.WarehouseCode,
            IsLocked = activeLock != null,
            LockedBy = activeLock?.LockedBy
        });
    }

    /// <summary>
    /// API-11: 梱包明細転記トリガー
    /// </summary>
    [HttpPost("packingslip/post")]
    public async Task<ActionResult<PackingSlipPostResponse>> PostPackingSlip(
        [FromBody] PackingSlipPostRequest request)
    {
        try
        {
            var result = await _d365.PostPackingSlipAsync(request.SeiriNos, request.OrderType);
            _logger.LogInformation(
                "梱包明細転記: jobId={JobId}, seiriNos={SeiriNos}",
                result.JobId, string.Join(",", request.SeiriNos));
            return Ok(result);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "梱包明細転記エラー");
            return StatusCode(500, new { error = "梱包明細転記に失敗しました" });
        }
    }
}
