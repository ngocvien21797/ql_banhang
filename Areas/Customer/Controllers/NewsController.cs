using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using QuanLyBanHang.Data;

namespace QuanLyBanHang.Areas.Customer.Controllers;

[Area("Customer")]
public class NewsController : Controller
{
    private readonly SalesDbContext _db;

    public NewsController(SalesDbContext db)
    {
        _db = db;
    }

    public async Task<IActionResult> Index(int page = 1, int pageSize = 9)
    {
        var query = _db.Articles
            .Where(a => a.IsActive)
            .OrderByDescending(a => a.CreatedAt);

        var total = await query.CountAsync();
        var articles = await query
            .Skip((page - 1) * pageSize)
            .Take(pageSize)
            .ToListAsync();

        ViewBag.Page = page;
        ViewBag.TotalPages = (int)Math.Ceiling((double)total / pageSize);
        ViewBag.Total = total;

        return View(articles);
    }

    public async Task<IActionResult> Detail(long id, string? slug)
    {
        var article = await _db.Articles
            .FirstOrDefaultAsync(a => a.Id == id && a.IsActive);

        if (article == null) return NotFound();

        if (!string.IsNullOrWhiteSpace(article.Slug) && slug != article.Slug)
            return RedirectToAction("Detail", new { id, slug = article.Slug });

        return View(article);
    }
}
