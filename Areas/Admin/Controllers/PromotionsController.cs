using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using QuanLyBanHang.Data;
using QuanLyBanHang.Models;

namespace QuanLyBanHang.Areas.Admin.Controllers;

[Area("Admin")]
[Authorize(Roles = "1")]
public class PromotionsController : Controller
{
    private readonly SalesDbContext _db;
    public PromotionsController(SalesDbContext db) => _db = db;

    public async Task<IActionResult> Index(int page = 1, int pageSize = 10)
    {
        var query = _db.Promotions.Include(p => p.PromotionProducts).OrderByDescending(p => p.CreatedAt).AsQueryable();
        var total = await query.CountAsync();
        var items = await query.Skip((page - 1) * pageSize).Take(pageSize).ToListAsync();

        ViewBag.Pagination = new QuanLyBanHang.ViewModels.PaginationModel
        {
            Page = page, PageSize = pageSize, TotalItems = total,
            Action = "Index", Controller = "Promotions"
        };

        return View(items);
    }

    public IActionResult Create()
    {
        ViewBag.Products = _db.Products.Where(p => p.IsActive).OrderBy(p => p.Name).ToList();
        return View();
    }

    [HttpPost]
    public async Task<IActionResult> Create(Promotion promotion, List<long> productIds)
    {
        if (!ModelState.IsValid) { ViewBag.Products = _db.Products.Where(p => p.IsActive).OrderBy(p => p.Name).ToList(); return View(promotion); }

        promotion.CreatedAt = DateTime.Now;
        if (string.IsNullOrWhiteSpace(promotion.Code))
            promotion.Code = $"KM{DateTime.Now:yyyyMMddHHmmss}";
        _db.Promotions.Add(promotion);
        await _db.SaveChangesAsync();

        if (productIds != null)
        {
            foreach (var pid in productIds)
            {
                _db.PromotionProducts.Add(new PromotionProduct { PromotionId = promotion.Id, ProductId = pid });
            }
            await _db.SaveChangesAsync();
        }

        TempData["Success"] = "Tạo khuyến mãi thành công!";
        return RedirectToAction(nameof(Index));
    }

    public async Task<IActionResult> Edit(long id)
    {
        var p = await _db.Promotions.Include(x => x.PromotionProducts).FirstOrDefaultAsync(x => x.Id == id);
        if (p == null) return NotFound();
        ViewBag.Products = _db.Products.Where(p => p.IsActive).OrderBy(p => p.Name).ToList();
        ViewBag.SelectedIds = p.PromotionProducts.Select(x => x.ProductId).ToList();
        return View(p);
    }

    [HttpPost]
    public async Task<IActionResult> Edit(long id, Promotion promotion, List<long> productIds)
    {
        if (id != promotion.Id) return BadRequest();
        if (!ModelState.IsValid) { ViewBag.Products = _db.Products.Where(p => p.IsActive).OrderBy(p => p.Name).ToList(); return View(promotion); }

        var existing = await _db.Promotions.Include(x => x.PromotionProducts).FirstOrDefaultAsync(x => x.Id == id);
        if (existing == null) return NotFound();

        existing.Name = promotion.Name;
        if (string.IsNullOrWhiteSpace(promotion.Code))
            existing.Code = $"KM{DateTime.Now:yyyyMMddHHmmss}";
        else
            existing.Code = promotion.Code;
        existing.Description = promotion.Description;
        existing.DiscountType = promotion.DiscountType;
        existing.DiscountValue = promotion.DiscountValue;
        existing.MinOrderValue = promotion.MinOrderValue;
        existing.StartDate = promotion.StartDate;
        existing.EndDate = promotion.EndDate;
        existing.IsActive = promotion.IsActive;

        _db.PromotionProducts.RemoveRange(existing.PromotionProducts);
        if (productIds != null)
        {
            foreach (var pid in productIds)
                _db.PromotionProducts.Add(new PromotionProduct { PromotionId = id, ProductId = pid });
        }

        await _db.SaveChangesAsync();
        TempData["Success"] = "Cập nhật khuyến mãi thành công!";
        return RedirectToAction(nameof(Index));
    }

    public async Task<IActionResult> Delete(long id)
    {
        var p = await _db.Promotions.Include(x => x.PromotionProducts).FirstOrDefaultAsync(x => x.Id == id);
        if (p == null) return NotFound();
        return View(p);
    }

    [HttpPost, ActionName("Delete")]
    public async Task<IActionResult> DeleteConfirmed(long id)
    {
        var p = await _db.Promotions.FindAsync(id);
        if (p == null) return NotFound();
        _db.Promotions.Remove(p);
        await _db.SaveChangesAsync();
        TempData["Success"] = "Đã xóa khuyến mãi.";
        return RedirectToAction(nameof(Index));
    }
}
