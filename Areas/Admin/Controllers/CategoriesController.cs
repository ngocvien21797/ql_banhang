using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using QuanLyBanHang.Data;
using QuanLyBanHang.Models;
namespace QuanLyBanHang.Areas.Admin.Controllers;
[Area("Admin")]
[Authorize(Roles = "1")]
public class CategoriesController : Controller
{
    private readonly SalesDbContext _db;

    public CategoriesController(SalesDbContext db)
    {
        _db = db;
    }

    public async Task<IActionResult> Index(string? search, int page = 1, int pageSize = 10)
    {
        var query = _db.Categories.Include(c => c.Products).Select(c => new CategoryViewModel
        {
            Id = c.Id,
            Name = c.Name,
            ProductCount = c.Products.Count,
            TotalStock = c.Products.Sum(p => p.Stock),
            TotalValue = c.Products.Sum(p => p.Price * p.Stock)
        });

        if (!string.IsNullOrWhiteSpace(search))
            query = query.Where(x => x.Name.Contains(search));
        query = query.OrderBy(x => x.Name);

        var total = await query.CountAsync();
        var items = await query.Skip((page - 1) * pageSize).Take(pageSize).ToListAsync();

        var routeValues = new Dictionary<string, string>();
        if (!string.IsNullOrWhiteSpace(search)) routeValues["search"] = search;

        ViewBag.Pagination = new QuanLyBanHang.ViewModels.PaginationModel
        {
            Page = page,
            PageSize = pageSize,
            TotalItems = total,
            Action = "Index",
            Controller = "Categories",
            RouteValues = routeValues
        };

        return View(items);
    }

    public IActionResult Create() => View();

    [HttpPost]
    public async Task<IActionResult> Create(Category category)
    {
        if (!ModelState.IsValid) return View(category);
        _db.Categories.Add(category);
        await _db.SaveChangesAsync();
        return RedirectToAction(nameof(Index));
    }

    public async Task<IActionResult> Edit(long id)
    {
        var category = await _db.Categories.FindAsync(id);
        if (category == null) return NotFound();
        return View(category);
    }

    [HttpPost]
    public async Task<IActionResult> Edit(long id, Category category)
    {
        if (id != category.Id) return BadRequest();
        if (!ModelState.IsValid) return View(category);

        _db.Update(category);
        await _db.SaveChangesAsync();
        return RedirectToAction(nameof(Index));
    }

    public async Task<IActionResult> Delete(long id)
    {
        var category = await _db.Categories.Include(c => c.Products).FirstOrDefaultAsync(c => c.Id == id);
        if (category == null) return NotFound();
        return View(category);
    }

    [HttpPost, ActionName("Delete")]
    public async Task<IActionResult> DeleteConfirmed(long id)
    {
        var category = await _db.Categories.FindAsync(id);
        if (category == null) return NotFound();

        _db.Categories.Remove(category);
        await _db.SaveChangesAsync();
        return RedirectToAction(nameof(Index));
    }

    public async Task<IActionResult> Details(long id)
    {
        var category = await _db.Categories
            .Include(c => c.Products)
            .FirstOrDefaultAsync(c => c.Id == id);
        if (category == null) return NotFound();
        return View(category);
    }

    public class CategoryViewModel
    {
        public long Id { get; set; }
        public string Name { get; set; } = "";
        public int ProductCount { get; set; }
        public int TotalStock { get; set; }
        public decimal TotalValue { get; set; }
    }
}
