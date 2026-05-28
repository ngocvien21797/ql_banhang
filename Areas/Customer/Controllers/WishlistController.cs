using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using QuanLyBanHang.Data;
using QuanLyBanHang.Models;
using System.Security.Claims;

namespace QuanLyBanHang.Areas.Customer.Controllers;

[Area("Customer")]
[Authorize(Roles = "2")]
public class WishlistController : Controller
{
    private readonly SalesDbContext _db;
    public WishlistController(SalesDbContext db) => _db = db;

    [HttpGet]
    public async Task<IActionResult> Index()
    {
        var cid = await GetCustomerIdAsync();
        if (cid == null) return Forbid();

        var items = await _db.Wishlists
            .Include(x => x.Product!).ThenInclude(x => x!.Category)
            .Where(x => x.CustomerId == cid && x.Product!.IsActive)
            .OrderByDescending(x => x.CreatedAt)
            .ToListAsync();

        return View(items);
    }

    [HttpPost]
    public async Task<IActionResult> Toggle(long productId)
    {
        var customerId = await GetCustomerIdAsync();
        if (customerId == null) return Unauthorized();

        var existing = await _db.Wishlists
            .FirstOrDefaultAsync(x => x.CustomerId == customerId && x.ProductId == productId);

        if (existing != null)
        {
            _db.Wishlists.Remove(existing);
            await _db.SaveChangesAsync();
            return Json(new { liked = false, message = "Đã bỏ yêu thích" });
        }

        _db.Wishlists.Add(new Wishlist
        {
            CustomerId = customerId.Value,
            ProductId = productId,
            CreatedAt = DateTime.Now
        });
        await _db.SaveChangesAsync();

        return Json(new { liked = true, message = "Đã thêm vào yêu thích" });
    }

    [HttpPost]
    public async Task<IActionResult> Remove(long productId)
    {
        var customerId = await GetCustomerIdAsync();
        if (customerId == null) return Forbid();

        var item = await _db.Wishlists
            .FirstOrDefaultAsync(x => x.CustomerId == customerId && x.ProductId == productId);
        if (item != null)
        {
            _db.Wishlists.Remove(item);
            await _db.SaveChangesAsync();
        }

        return RedirectToAction(nameof(Index));
    }

    public async Task<IActionResult> Check(long productId)
    {
        var customerId = await GetCustomerIdAsync();
        if (customerId == null) return Json(new { liked = false });

        var exists = await _db.Wishlists
            .AnyAsync(x => x.CustomerId == customerId && x.ProductId == productId);

        return Json(new { liked = exists });
    }

    private long GetUserId()
    {
        var s = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
        return long.TryParse(s, out var id) ? id : 0;
    }

    private async Task<long?> GetCustomerIdAsync()
    {
        var uid = GetUserId();
        return await _db.Users.Where(u => u.Id == uid).Select(u => u.CustomerId).FirstOrDefaultAsync();
    }
}
