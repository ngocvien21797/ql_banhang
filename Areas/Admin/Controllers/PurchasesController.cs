using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.EntityFrameworkCore;
using QuanLyBanHang.Data;
using QuanLyBanHang.Models;
using QuanLyBanHang.ViewModels;

namespace QuanLyBanHang.Areas.Admin.Controllers;
[Area("Admin")]
[Authorize(Roles = "1")]
public class PurchasesController : Controller
{
    private readonly SalesDbContext _db;
    public PurchasesController(SalesDbContext db) => _db = db;

    public async Task<IActionResult> Index(string? search, int page = 1, int pageSize = 10)
    {
        var query = _db.PurchaseInvoices.Include(x => x.Supplier).Include(x => x.Items).AsQueryable();

        if (!string.IsNullOrWhiteSpace(search))
            query = query.Where(x => x.Supplier != null && x.Supplier.Name.Contains(search));

        query = query.OrderByDescending(x => x.Id);

        var total = await query.CountAsync();
        var items = await query.Skip((page - 1) * pageSize).Take(pageSize).ToListAsync();

        var routeValues = new Dictionary<string, string>();
        if (!string.IsNullOrWhiteSpace(search)) routeValues["search"] = search;

        ViewBag.Pagination = new PaginationModel
        {
            Page = page, PageSize = pageSize, TotalItems = total,
            Action = "Index", Controller = "Purchases",
            RouteValues = routeValues
        };

        return View(items);
    }

    public async Task<IActionResult> Details(long id)
    {
        var inv = await _db.PurchaseInvoices
            .Include(x => x.Supplier)
            .Include(x => x.Items).ThenInclude(i => i.Product)
            .FirstOrDefaultAsync(x => x.Id == id);

        if (inv == null) return NotFound();
        return View(inv);
    }

    public async Task<IActionResult> Create()
    {
        ViewBag.Suppliers = new SelectList(await _db.Suppliers.OrderBy(x => x.Name).ToListAsync(), "Id", "Name");
        ViewBag.Products = new SelectList(await _db.Products.OrderBy(x => x.Name).ToListAsync(), "Id", "Name");
        return View();
    }

    [HttpPost]
    public async Task<IActionResult> Create(long? supplierId, List<long> productIds, List<int> qtys, List<decimal> unitCosts)
    {
        if (productIds.Count == 0 || productIds.Count != qtys.Count || productIds.Count != unitCosts.Count)
            return BadRequest("Dữ liệu sản phẩm không hợp lệ.");

        using var tx = await _db.Database.BeginTransactionAsync();

        var inv = new PurchaseInvoice
        {
            SupplierId = supplierId,
            CreatedAt = DateTime.Now
        };

        var userIdStr = User.FindFirst(System.Security.Claims.ClaimTypes.NameIdentifier)?.Value;
        if (long.TryParse(userIdStr, out var uid)) inv.CreatedBy = uid;

        _db.PurchaseInvoices.Add(inv);
        await _db.SaveChangesAsync();

        decimal total = 0;

        for (int i = 0; i < productIds.Count; i++)
        {
            var pid = productIds[i];
            var q = qtys[i];
            var cost = unitCosts[i];

            if (q <= 0) return BadRequest("Số lượng phải > 0");
            if (cost < 0) return BadRequest("Đơn giá phải >= 0");

            var product = await _db.Products.FirstOrDefaultAsync(p => p.Id == pid);
            if (product == null) return BadRequest($"Không tìm thấy SP id={pid}");

            product.Stock += q;

            var lineTotal = cost * q;
            total += lineTotal;

            _db.PurchaseItems.Add(new PurchaseItem
            {
                PurchaseInvoiceId = inv.Id,
                ProductId = pid,
                Quantity = q,
                UnitCost = cost,
                LineTotal = lineTotal
            });

            _db.StockLedgers.Add(new StockLedger
            {
                ProductId = pid,
                Type = "IN",
                Quantity = q,
                RefType = "PURCHASE",
                RefId = inv.Id,
                OccurredAt = DateTime.Now
            });
        }

        inv.Total = total;

        await _db.SaveChangesAsync();
        await tx.CommitAsync();

        return RedirectToAction(nameof(Details), new { id = inv.Id });
    }

    public async Task<IActionResult> Delete(long id)
    {
        var inv = await _db.PurchaseInvoices.Include(x => x.Supplier).Include(x => x.Items).FirstOrDefaultAsync(x => x.Id == id);
        if (inv == null) return NotFound();
        return View(inv);
    }

    [HttpPost, ActionName("Delete")]
    public async Task<IActionResult> DeleteConfirmed(long id)
    {
        using var tx = await _db.Database.BeginTransactionAsync();

        var inv = await _db.PurchaseInvoices
            .Include(x => x.Items)
            .FirstOrDefaultAsync(x => x.Id == id);

        if (inv == null) return NotFound();

        foreach (var item in inv.Items)
        {
            var product = await _db.Products.FirstOrDefaultAsync(p => p.Id == item.ProductId);
            if (product != null)
                product.Stock -= item.Quantity;
        }

        _db.StockLedgers.RemoveRange(_db.StockLedgers.Where(x => x.RefType == "PURCHASE" && x.RefId == id));
        _db.PurchaseInvoices.Remove(inv);

        await _db.SaveChangesAsync();
        await tx.CommitAsync();

        return RedirectToAction(nameof(Index));
    }
}
