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

            _db.StockLedgers.Add(new StockLedger
            {
                ProductId = item.ProductId,
                Type = "IN",
                Quantity = item.Quantity,
                RefType = "SALE_CANCEL",
                RefId = inv.Id,
                OccurredAt = DateTime.Now
            });
        }

        _db.SalesInvoices.Remove(inv);

        await _db.SaveChangesAsync();
        await tx.CommitAsync();

        return RedirectToAction(nameof(Index));
    }

    public async Task<IActionResult> Create()
    {
        ViewBag.Customers = new SelectList(await _db.Customers.OrderBy(x => x.Name).ToListAsync(), "Id", "Name");
        ViewBag.Products = new SelectList(await _db.Products.Where(x => x.IsActive).OrderBy(x => x.Name).ToListAsync(), "Id", "Name");
        return View();
    }

    [HttpPost]
    public async Task<IActionResult> Create(
        long? customerId, string receiverName, string? shippingPhone, string? shippingAddress,
        string paymentMethod, string? note,
        List<long> productIds, List<int> qtys, List<decimal> unitPrices)
    {
        if (productIds.Count == 0 || productIds.Count != qtys.Count || productIds.Count != unitPrices.Count)
        {
            TempData["Error"] = "Dữ liệu sản phẩm không hợp lệ.";
            return RedirectToAction(nameof(Create));
        }

        using var tx = await _db.Database.BeginTransactionAsync();

        var inv = new SalesInvoice
        {
            CustomerId = customerId,
            ReceiverName = receiverName ?? "",
            ShippingPhone = shippingPhone,
            ShippingAddress = shippingAddress,
            PaymentMethod = paymentMethod,
            Note = note,
            Status = "Pending",
            PaymentStatus = "Unpaid",
            CreatedAt = DateTime.Now
        };

        var userIdStr = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
        if (long.TryParse(userIdStr, out var uid)) inv.CreatedBy = uid;

        _db.SalesInvoices.Add(inv);
        await _db.SaveChangesAsync();

        inv.Code = $"DH{inv.Id:000000}";

        decimal total = 0;

        for (int i = 0; i < productIds.Count; i++)
        {
            var pid = productIds[i];
            var q = qtys[i];
            var price = unitPrices[i];

            if (q <= 0)
            {
                await tx.RollbackAsync();
                TempData["Error"] = "Số lượng phải > 0.";
                return RedirectToAction(nameof(Create));
            }
            if (price < 0)
            {
                await tx.RollbackAsync();
                TempData["Error"] = "Đơn giá phải >= 0.";
                return RedirectToAction(nameof(Create));
            }

            var product = await _db.Products.FirstOrDefaultAsync(p => p.Id == pid);
            if (product == null)
            {
                await tx.RollbackAsync();
                TempData["Error"] = $"Không tìm thấy sản phẩm ID={pid}.";
                return RedirectToAction(nameof(Create));
            }

            if (product.Stock < q)
            {
                await tx.RollbackAsync();
                TempData["Error"] = $"Sản phẩm \"{product.Name}\" không đủ hàng (còn {product.Stock}).";
                return RedirectToAction(nameof(Create));
            }

            product.Stock -= q;

            var lineTotal = price * q;
            total += lineTotal;

            _db.SalesItems.Add(new SalesItem
            {
                SalesInvoiceId = inv.Id,
                ProductId = pid,
                Quantity = q,
                UnitPrice = price,
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

        TempData["Ok"] = $"Đã tạo đơn hàng {inv.Code} thành công.";
        return RedirectToAction(nameof(Details), new { id = inv.Id });
    }

    private long GetUserId()
    {
        var s = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
        return long.TryParse(s, out var id) ? id : 0;
    }
}
