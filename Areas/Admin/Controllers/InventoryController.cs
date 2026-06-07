using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.EntityFrameworkCore;
using QuanLyBanHang.Data;
using QuanLyBanHang.ViewModels;

namespace QuanLyBanHang.Areas.Admin.Controllers;
[Area("Admin")]
[Authorize(Roles = "1")]
public class InventoryController : Controller
{
    private readonly SalesDbContext _db;
    public InventoryController(SalesDbContext db) => _db = db;

    public async Task<IActionResult> Stock(string? search, int? categoryId, string? stockFilter, int page = 1, int pageSize = 10)
    {
        var query = _db.Products.Include(p => p.Category).AsQueryable();

        if (!string.IsNullOrWhiteSpace(search))
            query = query.Where(p => p.Name.Contains(search) || p.Sku.Contains(search));

        if (categoryId > 0)
            query = query.Where(p => p.CategoryId == categoryId);

        if (!string.IsNullOrWhiteSpace(stockFilter))
        {
            query = stockFilter switch
            {
                "low" => query.Where(p => p.Stock <= 5),
                "warning" => query.Where(p => p.Stock <= 20),
                "instock" => query.Where(p => p.Stock > 0),
                "out" => query.Where(p => p.Stock == 0),
                _ => query
            };
        }

        ViewBag.Categories = new SelectList(await _db.Categories.OrderBy(x => x.Name).ToListAsync(), "Id", "Name");

        var total = await query.CountAsync();

        ViewBag.TotalProducts = total;
        ViewBag.TotalStock = await query.SumAsync(p => p.Stock);
        ViewBag.TotalValue = await query.SumAsync(p => p.Price * p.Stock);
        ViewBag.LowStockCount = await query.CountAsync(p => p.Stock <= 5);

        var items = await query.OrderBy(x => x.Name).Skip((page - 1) * pageSize).Take(pageSize).ToListAsync();

        var routeValues = new Dictionary<string, string>();
        if (!string.IsNullOrWhiteSpace(search)) routeValues["search"] = search;
        if (categoryId > 0) routeValues["categoryId"] = categoryId.ToString();
        if (!string.IsNullOrWhiteSpace(stockFilter)) routeValues["stockFilter"] = stockFilter;

        ViewBag.Pagination = new PaginationModel
        {
            Page = page, PageSize = pageSize, TotalItems = total,
            Action = "Stock", Controller = "Inventory",
            RouteValues = routeValues
        };

        return View(items);
    }

    public async Task<IActionResult> Ledger(DateTime? from, DateTime? to, int page = 1, int pageSize = 20)
    {
        var fromDate = from ?? DateTime.Today.AddDays(-30);
        var toDate = to ?? DateTime.Today;

        ViewBag.From = fromDate.ToString("yyyy-MM-dd");
        ViewBag.To = toDate.ToString("yyyy-MM-dd");

        var query = _db.StockLedgers
            .Include(x => x.Product)
            .Where(x => x.OccurredAt.Date >= fromDate && x.OccurredAt.Date <= toDate)
            .OrderByDescending(x => x.OccurredAt)
            .AsQueryable();

        var total = await query.CountAsync();
        ViewBag.TotalIn = await query.Where(x => x.Type == "IN").SumAsync(x => (int?)x.Quantity) ?? 0;
        ViewBag.TotalOut = await query.Where(x => x.Type == "OUT").SumAsync(x => (int?)x.Quantity) ?? 0;

        var items = await query.Skip((page - 1) * pageSize).Take(pageSize).ToListAsync();

        ViewBag.Pagination = new PaginationModel
        {
            Page = page, PageSize = pageSize, TotalItems = total,
            Action = "Ledger", Controller = "Inventory",
            RouteValues = new Dictionary<string, string> { { "from", ViewBag.From }, { "to", ViewBag.To } }
        };

        return View(items);
    }
}
