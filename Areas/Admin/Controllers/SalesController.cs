using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.EntityFrameworkCore;
using QuanLyBanHang.Data;
using QuanLyBanHang.Models;
using QuanLyBanHang.ViewModels;
using System.Security.Claims;

namespace QuanLyBanHang.Areas.Admin.Controllers;
[Area("Admin")]
[Authorize(Roles = "1")]
public class SalesController : Controller
{
    private readonly SalesDbContext _db;

    public SalesController(SalesDbContext db)
    {
        _db = db;
    }

    public async Task<IActionResult> Index(string? search, string? status, string? paymentStatus, int page = 1, int pageSize = 10)
    {
        var query = _db.SalesInvoices.Include(x => x.Customer).AsQueryable();

        if (!string.IsNullOrWhiteSpace(search))
            query = query.Where(x => x.Code.Contains(search) || (x.Customer != null && x.Customer.Name.Contains(search)));
        if (!string.IsNullOrWhiteSpace(status))
            query = query.Where(x => x.Status == status);
        if (!string.IsNullOrWhiteSpace(paymentStatus))
            query = query.Where(x => x.PaymentStatus == paymentStatus);

        query = query.OrderByDescending(x => x.Id);

        var total = await query.CountAsync();
        var items = await query.Skip((page - 1) * pageSize).Take(pageSize).ToListAsync();

        var routeValues = new Dictionary<string, string>();
        if (!string.IsNullOrWhiteSpace(search)) routeValues["search"] = search;
        if (!string.IsNullOrWhiteSpace(status)) routeValues["status"] = status;
        if (!string.IsNullOrWhiteSpace(paymentStatus)) routeValues["paymentStatus"] = paymentStatus;

        ViewBag.Pagination = new PaginationModel
        {
            Page = page, PageSize = pageSize, TotalItems = total,
            Action = "Index", Controller = "Sales",
            RouteValues = routeValues
        };

        return View(items);
    }

    public async Task<IActionResult> Details(long id)
    {
        var inv = await _db.SalesInvoices
            .Include(x => x.Customer)
            .Include(x => x.Items).ThenInclude(i => i.Product)
            .FirstOrDefaultAsync(x => x.Id == id);

        if (inv == null) return NotFound();
        return View(inv);
    }

    public async Task<IActionResult> Create()
    {
        ViewBag.Customers = new SelectList(await _db.Customers.OrderBy(x => x.Name).ToListAsync(), "Id", "Name");
        ViewBag.Products = new SelectList(await _db.Products.Where(p => p.IsActive).OrderBy(x => x.Name).ToListAsync(), "Id", "Name");
        return View();
    }

    // Tạo đơn tại quầy (admin) – đơn giản, không cần thông tin vận chuyển
    [HttpPost]
    public async Task<IActionResult> Create(long? customerId, List<long> productIds, List<int> qtys)
    {
        if (productIds.Count == 0 || productIds.Count != qtys.Count)
            return BadRequest("Dữ liệu sản phẩm không hợp lệ.");

        using var tx = await _db.Database.BeginTransactionAsync();

        var customer = customerId.HasValue ? await _db.Customers.FindAsync(customerId.Value) : null;

        var inv = new SalesInvoice
        {
            CustomerId = customerId,
            CreatedAt = DateTime.Now,
            CreatedBy = GetUserId(),
            Status = "Confirmed",
            PaymentMethod = "COD",
            PaymentStatus = "Unpaid",
            ReceiverName = customer?.Name ?? "Khách lẻ",
            ShippingPhone = customer?.Phone,
            ShippingAddress = customer?.Address
        };

        _db.SalesInvoices.Add(inv);
        await _db.SaveChangesAsync();

        inv.Code = $"DH{inv.Id:000000}";
        await _db.SaveChangesAsync();

        decimal total = 0;

        for (int i = 0; i < productIds.Count; i++)
        {
            var pid = productIds[i];
            var q = qtys[i];

            if (q <= 0) return BadRequest("Số lượng phải > 0");

            var product = await _db.Products.FirstOrDefaultAsync(p => p.Id == pid);
            if (product == null) return BadRequest($"Không tìm thấy SP id={pid}");
            if (product.Stock < q) return BadRequest($"Không đủ tồn kho cho {product.Name}");

            product.Stock -= q;

            var lineTotal = product.Price * q;
            total += lineTotal;

            _db.SalesItems.Add(new SalesItem
            {
                SalesInvoiceId = inv.Id,
                ProductId = pid,
                Quantity = q,
                UnitPrice = product.Price,
                LineTotal = lineTotal
            });

            _db.StockLedgers.Add(new StockLedger
            {
                ProductId = pid,
                Type = "OUT",
                Quantity = q,
                RefType = "SALE",
                RefId = inv.Id,
                OccurredAt = DateTime.Now
            });
        }

        inv.Total = total;
        await _db.SaveChangesAsync();
        await tx.CommitAsync();

        return RedirectToAction(nameof(Details), new { id = inv.Id });
    }

    [HttpPost]
    public async Task<IActionResult> UpdateStatus(long id, string status)
    {
        var inv = await _db.SalesInvoices.FindAsync(id);
        if (inv == null) return NotFound();

        var allow = new[] { "Pending", "Confirmed", "Shipped", "Completed", "Cancelled" };
        status = (status ?? "").Trim();
        if (!allow.Contains(status)) status = "Pending";

        inv.Status = status;
        await _db.SaveChangesAsync();

        if (inv.CustomerId.HasValue)
        {
            var statusMsg = status switch
            {
                "Confirmed" => "Đơn hàng của bạn đã được xác nhận.",
                "Shipped" => "Đơn hàng của bạn đang được giao.",
                "Completed" => "Đơn hàng của bạn đã hoàn thành.",
                "Cancelled" => "Đơn hàng của bạn đã bị hủy.",
                _ => $"Trạng thái đơn hàng đã thay đổi thành {SalesInvoice.DisplayStatus(status)}."
            };
            _db.Notifications.Add(new QuanLyBanHang.Models.Notification
            {
                CustomerId = inv.CustomerId.Value,
                Title = $"Đơn hàng {inv.Code}",
                Message = statusMsg,
                Url = $"/Shop/Details/{inv.Id}",
                CreatedAt = DateTime.Now
            });
            await _db.SaveChangesAsync();
        }

        return RedirectToAction(nameof(Details), new { id });
    }

    [HttpPost]
    public async Task<IActionResult> MarkPaid(long id)
    {
        var inv = await _db.SalesInvoices.FindAsync(id);
        if (inv == null) return NotFound();

        inv.PaymentStatus = "Paid";
        inv.PaidAt = DateTime.Now;

        if (string.IsNullOrWhiteSpace(inv.PaymentMethod))
            inv.PaymentMethod = "BANK";

        await _db.SaveChangesAsync();

        if (inv.CustomerId.HasValue)
        {
            _db.Notifications.Add(new QuanLyBanHang.Models.Notification
            {
                CustomerId = inv.CustomerId.Value,
                Title = $"Thanh toán {inv.Code}",
                Message = $"Đơn hàng {inv.Code} đã được thanh toán.",
                Url = $"/Shop/Details/{inv.Id}",
                CreatedAt = DateTime.Now
            });
            await _db.SaveChangesAsync();
        }

        return RedirectToAction(nameof(Details), new { id });
    }

    public async Task<IActionResult> Delete(long id)
    {
        var inv = await _db.SalesInvoices.Include(x => x.Customer).FirstOrDefaultAsync(x => x.Id == id);
        if (inv == null) return NotFound();
        return View(inv);
    }

    [HttpPost, ActionName("Delete")]
    public async Task<IActionResult> DeleteConfirmed(long id)
    {
        using var tx = await _db.Database.BeginTransactionAsync();

        var inv = await _db.SalesInvoices
            .Include(x => x.Items)
            .FirstOrDefaultAsync(x => x.Id == id);

        if (inv == null) return NotFound();

        // hoàn kho
        foreach (var item in inv.Items)
        {
            var product = await _db.Products.FirstOrDefaultAsync(p => p.Id == item.ProductId);
            if (product != null)
            {
                product.Stock += item.Quantity;
            }
        }

        _db.StockLedgers.RemoveRange(_db.StockLedgers.Where(x => x.RefType == "SALE" && x.RefId == id));
        _db.SalesInvoices.Remove(inv);

        await _db.SaveChangesAsync();
        await tx.CommitAsync();

        return RedirectToAction(nameof(Index));
    }

    private long GetUserId()
    {
        var s = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
        return long.TryParse(s, out var id) ? id : 0;
    }
}
