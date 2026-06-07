using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using QuanLyBanHang.Data;
using QuanLyBanHang.Models;
using CustomerEntity = QuanLyBanHang.Models.Customer;

namespace QuanLyBanHang.Areas.Admin.Controllers;
[Area("Admin")]
[Authorize(Roles = "1")]
public class CustomersController : Controller
{
    private readonly SalesDbContext _db;
    private readonly IWebHostEnvironment _env;

    public CustomersController(SalesDbContext db, IWebHostEnvironment env)
    {
        _db = db;
        _env = env;
    }

    public async Task<IActionResult> Index(string? search, int page = 1, int pageSize = 10)
    {
        var query = _db.Customers.Select(c => new CustomerViewModel
        {
            Id = c.Id,
            Name = c.Name,
            Phone = c.Phone,
            Email = c.Email,
            Address = c.Address,
            AvatarPath = c.AvatarPath,
            WalletBalance = c.WalletBalance,
            OrderCount = _db.SalesInvoices.Count(s => s.CustomerId == c.Id),
            TotalSpent = _db.SalesInvoices.Where(s => s.CustomerId == c.Id && s.PaymentStatus == "Paid").Sum(s => (decimal?)s.Total) ?? 0,
            LastOrderDate = _db.SalesInvoices.Where(s => s.CustomerId == c.Id).Max(s => (DateTime?)s.CreatedAt)
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
            Action = "Index", Controller = "Customers",
            RouteValues = routeValues
        };

        return View(items);
    }

    public async Task<IActionResult> Details(long id)
    {
        var c = await _db.Customers.FindAsync(id);
        if (c == null) return NotFound();

        ViewBag.OrderCount = await _db.SalesInvoices.CountAsync(x => x.CustomerId == id);
        ViewBag.TotalSpent = await _db.SalesInvoices.Where(x => x.CustomerId == id && x.PaymentStatus == "Paid").SumAsync(x => (decimal?)x.Total) ?? 0;
        ViewBag.LastOrder = await _db.SalesInvoices.Where(x => x.CustomerId == id).OrderByDescending(x => x.CreatedAt).Select(x => (DateTime?)x.CreatedAt).FirstOrDefaultAsync();
        ViewBag.RecentOrders = await _db.SalesInvoices.Where(x => x.CustomerId == id).OrderByDescending(x => x.CreatedAt).Take(5).ToListAsync();

        return View(c);
    }

    public async Task<IActionResult> Edit(long id)
    {
        var c = await _db.Customers.FindAsync(id);
        if (c == null) return NotFound();
        return View(c);
    }

    [HttpPost]
    public async Task<IActionResult> Edit(long id, CustomerEntity customer, IFormFile? avatarFile)
    {
        if (id != customer.Id) return BadRequest();
        if (!ModelState.IsValid) return View(customer);

        var dbCustomer = await _db.Customers.FirstOrDefaultAsync(c => c.Id == id);
        if (dbCustomer == null) return NotFound();

        dbCustomer.Name = customer.Name;
        dbCustomer.Phone = customer.Phone;
        dbCustomer.Email = customer.Email;
        dbCustomer.Address = customer.Address;
        dbCustomer.DateOfBirth = customer.DateOfBirth;
        dbCustomer.Gender = customer.Gender;

        if (avatarFile != null && avatarFile.Length > 0)
        {
            var saveResult = await SaveAvatarAsync(avatarFile);
            if (!saveResult.Success)
            {
                ModelState.AddModelError("", saveResult.ErrorMessage ?? "Upload ảnh thất bại.");
                return View(customer);
            }
            DeleteImageIfExists(dbCustomer.AvatarPath);
            dbCustomer.AvatarPath = saveResult.RelativePath;
        }

        await _db.SaveChangesAsync();
        TempData["Success"] = "Cập nhật khách hàng thành công.";
        return RedirectToAction(nameof(Index));
    }

    public async Task<IActionResult> Delete(long id)
    {
        var c = await _db.Customers.FindAsync(id);
        if (c == null) return NotFound();
        return View(c);
    }

    [HttpPost, ActionName("Delete")]
    public async Task<IActionResult> DeleteConfirmed(long id)
    {
        var c = await _db.Customers.FindAsync(id);
        if (c == null) return NotFound();

        DeleteImageIfExists(c.AvatarPath);
        _db.Customers.Remove(c);
        await _db.SaveChangesAsync();
        TempData["Success"] = "Xóa khách hàng thành công.";
        return RedirectToAction(nameof(Index));
    }

    private async Task<(bool Success, string? RelativePath, string? ErrorMessage)> SaveAvatarAsync(IFormFile file)
    {
        const long maxBytes = 5 * 1024 * 1024;
        if (file.Length > maxBytes)
            return (false, null, "Ảnh quá lớn (tối đa 5MB).");

        var ext = Path.GetExtension(file.FileName).ToLowerInvariant();
        var allowed = new[] { ".jpg", ".jpeg", ".png", ".webp" };
        if (!allowed.Contains(ext))
            return (false, null, "Chỉ cho phép JPG/JPEG/PNG/WEBP.");

        var dir = Path.Combine(_env.WebRootPath, "uploads", "avatars");
        Directory.CreateDirectory(dir);

        var fileName = $"{Guid.NewGuid():N}{ext}";
        var physicalPath = Path.Combine(dir, fileName);

        using (var stream = new FileStream(physicalPath, FileMode.Create))
        {
            await file.CopyToAsync(stream);
        }

        var relative = $"/uploads/avatars/{fileName}";
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

    public class CustomerViewModel
    {
        public long Id { get; set; }
        public string Name { get; set; } = "";
        public string? Phone { get; set; }
        public string? Email { get; set; }
        public string? AvatarPath { get; set; }
        public string? Address { get; set; }
        public decimal WalletBalance { get; set; }
        public int OrderCount { get; set; }
        public decimal TotalSpent { get; set; }
        public DateTime? LastOrderDate { get; set; }
    }
}
