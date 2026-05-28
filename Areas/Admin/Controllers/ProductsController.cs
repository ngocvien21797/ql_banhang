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
public class ProductsController : Controller
{
    private readonly SalesDbContext _db;
    private readonly IWebHostEnvironment _env;
    public ProductsController(SalesDbContext db, IWebHostEnvironment env)
    {
        _db = db;
        _env = env;
    }

    public async Task<IActionResult> Index(string? search, long? categoryId, int page = 1, int pageSize = 10)
    {
        var query = _db.Products.Include(x => x.Category).AsQueryable();

        if (!string.IsNullOrWhiteSpace(search))
            query = query.Where(p => p.Name.Contains(search) || p.Sku.Contains(search));
        if (categoryId.HasValue)
            query = query.Where(p => p.CategoryId == categoryId.Value);

        query = query.OrderBy(x => x.Name);

        var total = await query.CountAsync();
        var items = await query.Skip((page - 1) * pageSize).Take(pageSize).ToListAsync();

        var routeValues = new Dictionary<string, string>();
        if (!string.IsNullOrWhiteSpace(search)) routeValues["search"] = search;
        if (categoryId.HasValue) routeValues["categoryId"] = categoryId.Value.ToString();

        ViewBag.Pagination = new PaginationModel
        {
            Page = page, PageSize = pageSize, TotalItems = total,
            Action = "Index", Controller = "Products",
            RouteValues = routeValues
        };

        ViewBag.Categories = new SelectList(await _db.Categories.OrderBy(c => c.Name).ToListAsync(), "Id", "Name");

        return View(items);
    }

    public async Task<IActionResult> Details(long id)
    {
        var product = await _db.Products
            .Include(x => x.Category)
            .FirstOrDefaultAsync(x => x.Id == id);

        if (product == null) return NotFound();
        return View(product);
    }

    public async Task<IActionResult> Create()
    {
        ViewBag.Categories = new SelectList(
            await _db.Categories.OrderBy(x => x.Name).ToListAsync(),
            "Id", "Name"
        );
        return View();
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Create(Product product, IFormFile? imageFile)
    {
        if (!ModelState.IsValid)
        {
            ViewBag.Categories = new SelectList(
                await _db.Categories.OrderBy(x => x.Name).ToListAsync(),
                "Id", "Name", product.CategoryId
            );
            return View(product);
        }

        // Upload ảnh (nếu có)
        if (imageFile != null && imageFile.Length > 0)
        {
            var saveResult = await SaveProductImageAsync(imageFile);
            if (!saveResult.Success)
            {
                ModelState.AddModelError("", saveResult.ErrorMessage ?? "Upload ảnh thất bại.");
                ViewBag.Categories = new SelectList(
                    await _db.Categories.OrderBy(x => x.Name).ToListAsync(),
                    "Id", "Name", product.CategoryId
                );
                return View(product);
            }

            product.ImagePath = saveResult.RelativePath; // ví dụ: /uploads/products/xxx.jpg
        }

        _db.Products.Add(product);
        await _db.SaveChangesAsync();
        return RedirectToAction(nameof(Index));
    }

    public async Task<IActionResult> Edit(long id)
    {
        var product = await _db.Products.FindAsync(id);
        if (product == null) return NotFound();

        ViewBag.Categories = new SelectList(
            await _db.Categories.OrderBy(x => x.Name).ToListAsync(),
            "Id", "Name", product.CategoryId
        );

        return View(product);
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Edit(long id, Product product, IFormFile? imageFile)
    {
        if (id != product.Id) return BadRequest();

        if (!ModelState.IsValid)
        {
            ViewBag.Categories = new SelectList(
                await _db.Categories.OrderBy(x => x.Name).ToListAsync(),
                "Id", "Name", product.CategoryId
            );
            return View(product);
        }

        // IMPORTANT: Không _db.Update(product) vì sẽ làm mất ImagePath nếu form không gửi field này.
        var dbProduct = await _db.Products.FirstOrDefaultAsync(x => x.Id == id);
        if (dbProduct == null) return NotFound();

        // Update các field
        dbProduct.Sku = product.Sku;
        dbProduct.Name = product.Name;
        dbProduct.CategoryId = product.CategoryId;
        dbProduct.Price = product.Price;
        dbProduct.Stock = product.Stock;
        dbProduct.IsActive = product.IsActive;

        // Nếu có ảnh mới → upload + xoá ảnh cũ
        if (imageFile != null && imageFile.Length > 0)
        {
            var saveResult = await SaveProductImageAsync(imageFile);
            if (!saveResult.Success)
            {
                ModelState.AddModelError("", saveResult.ErrorMessage ?? "Upload ảnh thất bại.");
                ViewBag.Categories = new SelectList(
                    await _db.Categories.OrderBy(x => x.Name).ToListAsync(),
                    "Id", "Name", product.CategoryId
                );
                return View(product);
            }

            // Xoá ảnh cũ (nếu có)
            DeleteImageIfExists(dbProduct.ImagePath);

            dbProduct.ImagePath = saveResult.RelativePath;
        }

        await _db.SaveChangesAsync();
        return RedirectToAction(nameof(Index));
    }

    public async Task<IActionResult> Delete(long id)
    {
        var product = await _db.Products
            .Include(x => x.Category)
            .FirstOrDefaultAsync(x => x.Id == id);

        if (product == null) return NotFound();
        return View(product);
    }

    [HttpPost, ActionName("Delete")]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> DeleteConfirmed(long id)
    {
        var product = await _db.Products.FindAsync(id);
        if (product == null) return NotFound();

        // Xoá ảnh file trước
        DeleteImageIfExists(product.ImagePath);

        _db.Products.Remove(product);
        await _db.SaveChangesAsync();
        return RedirectToAction(nameof(Index));
    }

    // =========================
    // Helpers
    // =========================

    private async Task<(bool Success, string? RelativePath, string? ErrorMessage)> SaveProductImageAsync(IFormFile file)
    {
        // Validate size (ví dụ 5MB)
        const long maxBytes = 5 * 1024 * 1024;
        if (file.Length > maxBytes)
            return (false, null, "Ảnh quá lớn (tối đa 5MB).");

        var ext = Path.GetExtension(file.FileName).ToLowerInvariant();
        var allowed = new[] { ".jpg", ".jpeg", ".png", ".webp" };
        if (!allowed.Contains(ext))
            return (false, null, "Chỉ cho phép JPG/JPEG/PNG/WEBP.");

        var dir = Path.Combine(_env.WebRootPath, "uploads", "products");
        Directory.CreateDirectory(dir);

        var fileName = $"{Guid.NewGuid():N}{ext}";
        var physicalPath = Path.Combine(dir, fileName);

        using (var stream = new FileStream(physicalPath, FileMode.Create))
        {
            await file.CopyToAsync(stream);
        }

        var relative = $"/uploads/products/{fileName}";
        return (true, relative, null);
    }

    private void DeleteImageIfExists(string? imagePath)
    {
        if (string.IsNullOrWhiteSpace(imagePath)) return;

        // Chỉ xử lý ảnh nằm trong wwwroot (đường dẫn dạng /uploads/...)
        var rel = imagePath.TrimStart('/');

        // Chặn path traversal
        var full = Path.GetFullPath(Path.Combine(_env.WebRootPath, rel));
        var root = Path.GetFullPath(_env.WebRootPath);
        if (!full.StartsWith(root, StringComparison.OrdinalIgnoreCase)) return;

        if (System.IO.File.Exists(full))
            System.IO.File.Delete(full);
    }
}
