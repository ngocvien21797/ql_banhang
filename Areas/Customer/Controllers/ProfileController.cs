using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using QuanLyBanHang.Data;
using QuanLyBanHang.Models;
using System.Security.Claims;
using CustomerEntity = QuanLyBanHang.Models.Customer;
using BCrypt.Net;

namespace QuanLyBanHang.Areas.Customer.Controllers;
[Area("Customer")]
[Authorize(Roles = "2")]
public class ProfileController : Controller
{
    private readonly SalesDbContext _db;
    private readonly IWebHostEnvironment _env;
    public ProfileController(SalesDbContext db, IWebHostEnvironment env)
    {
        _db = db;
        _env = env;
    }

    [HttpGet]
    public async Task<IActionResult> Index()
    {
        var uid = GetUserId();
        var user = await _db.Users.Include(u => u.Customer).FirstOrDefaultAsync(u => u.Id == uid);
        if (user?.Customer == null) return NotFound();

        ViewBag.Email = user.Username;
        return View(user.Customer);
    }

    [HttpPost]
    public async Task<IActionResult> Index(CustomerEntity model, IFormFile? avatarFile)
    {
        var uid = GetUserId();
        var customerId = await _db.Users.Where(u => u.Id == uid).Select(u => u.CustomerId).FirstOrDefaultAsync();
        if (customerId == null) return Forbid();

        var customer = await _db.Customers.FindAsync(customerId);
        if (customer == null) return NotFound();

        customer.Name = model.Name?.Trim() ?? customer.Name;
        customer.Phone = string.IsNullOrWhiteSpace(model.Phone) ? null : model.Phone.Trim();
        customer.Email = string.IsNullOrWhiteSpace(model.Email) ? null : model.Email.Trim();
        customer.Address = string.IsNullOrWhiteSpace(model.Address) ? null : model.Address.Trim();
        customer.DateOfBirth = model.DateOfBirth;
        customer.Gender = string.IsNullOrWhiteSpace(model.Gender) ? null : model.Gender;

        if (avatarFile != null && avatarFile.Length > 0)
        {
            var saveResult = await SaveAvatarAsync(avatarFile);
            if (!saveResult.Success)
            {
                TempData["Error"] = saveResult.ErrorMessage ?? "Upload ảnh thất bại.";
                return RedirectToAction(nameof(Index));
            }
            DeleteImageIfExists(customer.AvatarPath);
            customer.AvatarPath = saveResult.RelativePath;
        }

        await _db.SaveChangesAsync();
        TempData["Ok"] = "Cập nhật thông tin thành công!";
        return RedirectToAction(nameof(Index));
    }

    [HttpPost]
    public async Task<IActionResult> ChangePassword(string currentPassword, string newPassword)
    {
        if (string.IsNullOrWhiteSpace(newPassword) || newPassword.Length < 3)
        {
            TempData["Error"] = "Mật khẩu mới quá ngắn.";
            return RedirectToAction(nameof(Index));
        }

        var uid = GetUserId();
        var user = await _db.Users.FindAsync(uid);
        if (user == null) return NotFound();

        bool passwordOk;
        if (user.Password.StartsWith("$2"))
            passwordOk = BCrypt.Net.BCrypt.Verify(currentPassword, user.Password);
        else
            passwordOk = user.Password == currentPassword;

        if (!passwordOk)
        {
            TempData["Error"] = "Mật khẩu hiện tại không đúng.";
            return RedirectToAction(nameof(Index));
        }

        user.Password = BCrypt.Net.BCrypt.HashPassword(newPassword);
        await _db.SaveChangesAsync();

        TempData["Ok"] = "Đổi mật khẩu thành công!";
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

    private long GetUserId()
    {
        var s = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
        return long.TryParse(s, out var id) ? id : 0;
    }
}
