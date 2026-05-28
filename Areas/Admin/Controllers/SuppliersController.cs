using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using QuanLyBanHang.Data;
using QuanLyBanHang.Models;
namespace QuanLyBanHang.Areas.Admin.Controllers;
[Area("Admin")]
[Authorize(Roles = "1")]
public class SuppliersController : Controller
{
    private readonly SalesDbContext _db;

    public SuppliersController(SalesDbContext db)
    {
        _db = db;
    }

    public async Task<IActionResult> Index(string? search, int page = 1, int pageSize = 10)
    {
        var query = _db.Suppliers.Select(s => new SupplierViewModel
        {
            Id = s.Id,
            Name = s.Name,
            Phone = s.Phone,
            Address = s.Address,
            PurchaseCount = _db.PurchaseInvoices.Count(p => p.SupplierId == s.Id),
            TotalPurchase = _db.PurchaseInvoices.Where(p => p.SupplierId == s.Id).Sum(p => (decimal?)p.Total) ?? 0,
            LastPurchaseDate = _db.PurchaseInvoices.Where(p => p.SupplierId == s.Id).Max(p => (DateTime?)p.CreatedAt)
        });

        if (!string.IsNullOrWhiteSpace(search))
            query = query.Where(x => x.Name.Contains(search) || (x.Phone != null && x.Phone.Contains(search)));
        query = query.OrderBy(x => x.Name);

        var total = await query.CountAsync();
        var items = await query.Skip((page - 1) * pageSize).Take(pageSize).ToListAsync();

        var routeValues = new Dictionary<string, string>();
        if (!string.IsNullOrWhiteSpace(search)) routeValues["search"] = search;

        ViewBag.Pagination = new QuanLyBanHang.ViewModels.PaginationModel
        {
            Page = page, PageSize = pageSize, TotalItems = total,
            Action = "Index", Controller = "Suppliers",
            RouteValues = routeValues
        };

        return View(items);
    }

    public async Task<IActionResult> Details(long id)
    {
        var s = await _db.Suppliers.FindAsync(id);
        if (s == null) return NotFound();

        ViewBag.PurchaseCount = await _db.PurchaseInvoices.CountAsync(x => x.SupplierId == id);
        ViewBag.TotalPurchase = await _db.PurchaseInvoices.Where(x => x.SupplierId == id).SumAsync(x => (decimal?)x.Total) ?? 0;
        ViewBag.LastPurchase = await _db.PurchaseInvoices.Where(x => x.SupplierId == id).OrderByDescending(x => x.CreatedAt).Select(x => (DateTime?)x.CreatedAt).FirstOrDefaultAsync();
        ViewBag.RecentPurchases = await _db.PurchaseInvoices.Where(x => x.SupplierId == id).Include(x => x.Supplier).OrderByDescending(x => x.CreatedAt).Take(5).ToListAsync();

        return View(s);
    }

    public IActionResult Create() => View();

    [HttpPost]
    public async Task<IActionResult> Create(Supplier supplier)
    {
        if (!ModelState.IsValid) return View(supplier);
        _db.Suppliers.Add(supplier);
        await _db.SaveChangesAsync();
        return RedirectToAction(nameof(Index));
    }

    public async Task<IActionResult> Edit(long id)
    {
        var s = await _db.Suppliers.FindAsync(id);
        if (s == null) return NotFound();
        return View(s);
    }

    [HttpPost]
    public async Task<IActionResult> Edit(long id, Supplier supplier)
    {
        if (id != supplier.Id) return BadRequest();
        if (!ModelState.IsValid) return View(supplier);
        _db.Update(supplier);
        await _db.SaveChangesAsync();
        return RedirectToAction(nameof(Index));
    }

    public async Task<IActionResult> Delete(long id)
    {
        var s = await _db.Suppliers.FindAsync(id);
        if (s == null) return NotFound();
        return View(s);
    }

    [HttpPost, ActionName("Delete")]
    public async Task<IActionResult> DeleteConfirmed(long id)
    {
        var s = await _db.Suppliers.FindAsync(id);
        if (s == null) return NotFound();
        _db.Suppliers.Remove(s);
        await _db.SaveChangesAsync();
        return RedirectToAction(nameof(Index));
    }

    public class SupplierViewModel
    {
        public long Id { get; set; }
        public string Name { get; set; } = "";
        public string? Phone { get; set; }
        public string? Address { get; set; }
        public int PurchaseCount { get; set; }
        public decimal TotalPurchase { get; set; }
        public DateTime? LastPurchaseDate { get; set; }
    }
}
