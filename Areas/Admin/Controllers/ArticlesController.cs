using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using QuanLyBanHang.Data;
using QuanLyBanHang.Models;
using QuanLyBanHang.ViewModels;
using System.Text.RegularExpressions;

namespace QuanLyBanHang.Areas.Admin.Controllers;

[Area("Admin")]
[Authorize(Roles = "1")]
public class ArticlesController : Controller
{
    private readonly SalesDbContext _db;
    private readonly IWebHostEnvironment _env;

    public ArticlesController(SalesDbContext db, IWebHostEnvironment env)
    {
        _db = db;
        _env = env;
    }

    public async Task<IActionResult> Index(string? search, int page = 1, int pageSize = 10)
    {
        var query = _db.Articles.AsQueryable();

        if (!string.IsNullOrWhiteSpace(search))
            query = query.Where(a => a.Title.Contains(search) || (a.Summary != null && a.Summary.Contains(search)));

        query = query.OrderByDescending(a => a.CreatedAt);

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
            Controller = "Articles",
            RouteValues = routeValues
        };

        return View(items);
    }

    public IActionResult Create() => View();

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Create(Article article, IFormFile? imageFile)
    {
        if (!ModelState.IsValid) return View(article);

        if (string.IsNullOrWhiteSpace(article.Slug))
            article.Slug = GenerateSlug(article.Title);

        if (imageFile != null && imageFile.Length > 0)
        {
            var saveResult = await SaveImageAsync(imageFile);
            if (!saveResult.Success)
            {
                ModelState.AddModelError("", saveResult.ErrorMessage ?? "Upload ảnh thất bại.");
                return View(article);
            }
            article.ImagePath = saveResult.RelativePath;
        }

        article.CreatedBy = User.Identity?.Name;
        _db.Articles.Add(article);
        await _db.SaveChangesAsync();
        TempData["Success"] = "Thêm bài viết thành công.";
        return RedirectToAction(nameof(Index));
    }

    public async Task<IActionResult> Edit(long id)
    {
        var article = await _db.Articles.FindAsync(id);
        if (article == null) return NotFound();
        return View(article);
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Edit(long id, Article article, IFormFile? imageFile)
    {
        if (id != article.Id) return BadRequest();
        if (!ModelState.IsValid) return View(article);

        var dbArticle = await _db.Articles.FirstOrDefaultAsync(a => a.Id == id);
        if (dbArticle == null) return NotFound();

        dbArticle.Title = article.Title;
        dbArticle.Slug = string.IsNullOrWhiteSpace(article.Slug) ? GenerateSlug(article.Title) : article.Slug;
        dbArticle.Summary = article.Summary;
        dbArticle.Content = article.Content;
        dbArticle.IsActive = article.IsActive;

        if (imageFile != null && imageFile.Length > 0)
        {
            var saveResult = await SaveImageAsync(imageFile);
            if (!saveResult.Success)
            {
                ModelState.AddModelError("", saveResult.ErrorMessage ?? "Upload ảnh thất bại.");
                return View(article);
            }
            DeleteImageIfExists(dbArticle.ImagePath);
            dbArticle.ImagePath = saveResult.RelativePath;
        }

        await _db.SaveChangesAsync();
        TempData["Success"] = "Cập nhật bài viết thành công.";
        return RedirectToAction(nameof(Index));
    }

    public async Task<IActionResult> Delete(long id)
    {
        var article = await _db.Articles.FindAsync(id);
        if (article == null) return NotFound();
        return View(article);
    }

    [HttpPost, ActionName("Delete")]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> DeleteConfirmed(long id)
    {
        var article = await _db.Articles.FindAsync(id);
        if (article == null) return NotFound();

        DeleteImageIfExists(article.ImagePath);
        _db.Articles.Remove(article);
        await _db.SaveChangesAsync();
        TempData["Success"] = "Xóa bài viết thành công.";
        return RedirectToAction(nameof(Index));
    }

    private static string GenerateSlug(string title)
    {
        var str = title.ToLowerInvariant().Normalize(System.Text.NormalizationForm.FormD);
        var sb = new System.Text.StringBuilder();
        foreach (var c in str)
        {
            var uc = System.Globalization.CharUnicodeInfo.GetUnicodeCategory(c);
            if (uc != System.Globalization.UnicodeCategory.NonSpacingMark)
                sb.Append(c);
        }
        var slug = sb.ToString().Normalize(System.Text.NormalizationForm.FormC);
        slug = Regex.Replace(slug, @"[^a-z0-9\s-]", "");
        slug = Regex.Replace(slug, @"\s+", "-").Trim('-');
        slug = Regex.Replace(slug, @"-+", "-");
        return slug.Length > 200 ? slug[..200] : slug;
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

        var dir = Path.Combine(_env.WebRootPath, "uploads", "articles");
        Directory.CreateDirectory(dir);

        var fileName = $"{Guid.NewGuid():N}{ext}";
        var physicalPath = Path.Combine(dir, fileName);

        using (var stream = new FileStream(physicalPath, FileMode.Create))
        {
            await file.CopyToAsync(stream);
        }

        var relative = $"/uploads/articles/{fileName}";
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
