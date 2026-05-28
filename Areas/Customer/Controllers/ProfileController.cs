using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using QuanLyBanHang.Data;
using QuanLyBanHang.Models;
using System.Security.Claims;
using CustomerEntity = QuanLyBanHang.Models.Customer;

namespace QuanLyBanHang.Areas.Customer.Controllers;
[Area("Customer")]
[Authorize(Roles = "2")]
public class ProfileController : Controller
{
    private readonly SalesDbContext _db;
    public ProfileController(SalesDbContext db) => _db = db;

    [HttpGet]
    public async Task<IActionResult> Index()
    {
        var uid = GetUserId();
        var customer = await _db.Users.Where(u => u.Id == uid).Select(u => u.Customer).FirstOrDefaultAsync();
        if (customer == null) return NotFound();

        return View(customer);
    }

    [HttpPost]
    public async Task<IActionResult> Index(CustomerEntity model)
    {
        var uid = GetUserId();
        var customerId = await _db.Users.Where(u => u.Id == uid).Select(u => u.CustomerId).FirstOrDefaultAsync();
        if (customerId == null) return Forbid();

        var customer = await _db.Customers.FindAsync(customerId);
        if (customer == null) return NotFound();

        customer.Name = model.Name?.Trim() ?? customer.Name;
        customer.Phone = string.IsNullOrWhiteSpace(model.Phone) ? null : model.Phone.Trim();
        customer.Address = string.IsNullOrWhiteSpace(model.Address) ? null : model.Address.Trim();

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

        if (user.Password != currentPassword)
        {
            TempData["Error"] = "Mật khẩu hiện tại không đúng.";
            return RedirectToAction(nameof(Index));
        }

        user.Password = newPassword;
        await _db.SaveChangesAsync();

        TempData["Ok"] = "Đổi mật khẩu thành công!";
        return RedirectToAction(nameof(Index));
    }

    private long GetUserId()
    {
        var s = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
        return long.TryParse(s, out var id) ? id : 0;
    }
}
