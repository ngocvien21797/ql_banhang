using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using QuanLyBanHang.Data;
using QuanLyBanHang.Models;
using QuanLyBanHang.ViewModels;

namespace QuanLyBanHang.Areas.Admin.Controllers;

[Area("Admin")]
[Authorize(Roles = "1")]
public class BannersController : Controller
{
    private readonly SalesDbContext _db;
    private readonly IWebHostEnvironment _env;

    public BannersController(SalesDbContext db, IWebHostEnvironment env)
    {
        _db = db;
        _env = env;
    }

    public async Task<IActionResult> Index(string? search, int page = 1, int pageSize = 10)
    {
        var query = _db.Banners.AsQueryable();

        if (!string.IsNullOrWhiteSpace(search))
            query = query.Where(b => b.Title.Contains(search));

        query = query.OrderBy(b => b.SortOrder).ThenByDescending(b => b.CreatedAt);

        var total = await query.CountAsync();
        var items = await query.Skip((page - 1) * pageSize).Take(pageSize).ToListAsync();

        var routeValues = new Dictionary<string, string>();
        if (!string.IsNullOrWhiteSpace(search)) routeValues["search"] = search;

        ViewBag.Pagination = new PaginationModel
        {
            Page = page,
            PageSize = pageSize,
            TotalItems = total,
            Action = "Index",
            Controller = "Banners",
            RouteValues = routeValues
        };

        return View(items);
    }

    public IActionResult Create() => View();

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Create(Banner banner, IFormFile? imageFile)
    {
        if (!ModelState.IsValid) return View(banner);

        if (imageFile != null && imageFile.Length > 0)
        {
            var saveResult = await SaveImageAsync(imageFile);
            if (!saveResult.Success)
            {
                ModelState.AddModelError("", saveResult.ErrorMessage ?? "Upload ảnh thất bại.");
                return View(banner);
            }
            banner.ImagePath = saveResult.RelativePath;
        }

        _db.Banners.Add(banner);
        await _db.SaveChangesAsync();
        TempData["Success"] = "Thêm banner thành công.";
        return RedirectToAction(nameof(Index));
    }

    public async Task<IActionResult> Edit(long id)
    {
        var banner = await _db.Banners.FindAsync(id);
        if (banner == null) return NotFound();
        return View(banner);
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Edit(long id, Banner banner, IFormFile? imageFile)
    {
        if (id != banner.Id) return BadRequest();
        if (!ModelState.IsValid) return View(banner);

        var dbBanner = await _db.Banners.FirstOrDefaultAsync(b => b.Id == id);
        if (dbBanner == null) return NotFound();

        dbBanner.Title = banner.Title;
        dbBanner.SortOrder = banner.SortOrder;
        dbBanner.IsActive = banner.IsActive;

        if (imageFile != null && imageFile.Length > 0)
        {
            var saveResult = await SaveImageAsync(imageFile);
            if (!saveResult.Success)
            {
                ModelState.AddModelError("", saveResult.ErrorMessage ?? "Upload ảnh thất bại.");
                return View(banner);
            }
            DeleteImageIfExists(dbBanner.ImagePath);
            dbBanner.ImagePath = saveResult.RelativePath;
        }

        await _db.SaveChangesAsync();
        TempData["Success"] = "Cập nhật banner thành công.";
        return RedirectToAction(nameof(Index));
    }

    public async Task<IActionResult> Delete(long id)
    {
        var banner = await _db.Banners.FindAsync(id);
        if (banner == null) return NotFound();
        return View(banner);
    }

    [HttpPost, ActionName("Delete")]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> DeleteConfirmed(long id)
    {
        var banner = await _db.Banners.FindAsync(id);
        if (banner == null) return NotFound();

        DeleteImageIfExists(banner.ImagePath);
        _db.Banners.Remove(banner);
        await _db.SaveChangesAsync();
        TempData["Success"] = "Xóa banner thành công.";
        return RedirectToAction(nameof(Index));
    }

    private async Task<(bool Success, string? RelativePath, string? ErrorMessage)> SaveImageAsync(IFormFile file)
    {
        const long maxBytes = 10 * 1024 * 1024;
        if (file.Length > maxBytes)
            return (false, null, "Ảnh quá lớn (tối đa 10MB).");

        var ext = Path.GetExtension(file.FileName).ToLowerInvariant();
        var allowed = new[] { ".jpg", ".jpeg", ".png", ".webp" };
        if (!allowed.Contains(ext))
            return (false, null, "Chỉ cho phép JPG/JPEG/PNG/WEBP.");

        var dir = Path.Combine(_env.WebRootPath, "uploads", "banners");
        Directory.CreateDirectory(dir);

        var fileName = $"{Guid.NewGuid():N}{ext}";
        var physicalPath = Path.Combine(dir, fileName);

        using (var stream = new FileStream(physicalPath, FileMode.Create))
        {
            await file.CopyToAsync(stream);
        }

        var relative = $"/uploads/banners/{fileName}";
        return (true, relative, null);
    }

    private void DeleteImageIfExists(string? imagePath)
    {
        if (string.IsNullOrWhiteSpace(imagePath)) return;
        var rel = imagePath.TrimStart('/');
        var full = Path.GetFullPath(Path.Combine(_env.WebRootPath, rel));
        var root = Path.GetFullPath(_env.WebRootPath);
        if (!full.StartsWith(root, StringComparison.OrdinalIgnoreCase)) return;
        if (System.IO.File.Exists(full))
            System.IO.File.Delete(full);
    }
}
