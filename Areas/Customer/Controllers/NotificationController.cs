using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using QuanLyBanHang.Data;
using System.Security.Claims;

namespace QuanLyBanHang.Areas.Customer.Controllers;

[Area("Customer")]
[Authorize(Roles = "2")]
public class NotificationController : Controller
{
    private readonly SalesDbContext _db;
    public NotificationController(SalesDbContext db) => _db = db;

    [HttpGet]
    public async Task<IActionResult> Index()
    {
        var customerId = await GetCustomerIdAsync();
        if (customerId == null) return Forbid();

        var list = await _db.Notifications
            .Where(x => x.CustomerId == customerId)
            .OrderByDescending(x => x.CreatedAt)
            .ToListAsync();

        return View(list);
    }

    [HttpPost]
    public async Task<IActionResult> MarkRead(long id)
    {
        var customerId = await GetCustomerIdAsync();
        if (customerId == null) return Forbid();

        var noti = await _db.Notifications
            .FirstOrDefaultAsync(x => x.Id == id && x.CustomerId == customerId);
        if (noti != null)
        {
            noti.IsRead = true;
            await _db.SaveChangesAsync();
        }

        return RedirectToAction(nameof(Index));
    }

    [HttpPost]
    public async Task<IActionResult> MarkAllRead()
    {
        var customerId = await GetCustomerIdAsync();
        if (customerId == null) return Forbid();

        var unread = await _db.Notifications
            .Where(x => x.CustomerId == customerId && !x.IsRead)
            .ToListAsync();

        foreach (var n in unread) n.IsRead = true;
        await _db.SaveChangesAsync();

        return RedirectToAction(nameof(Index));
    }

    [HttpGet]
    public async Task<IActionResult> GetUnreadCount()
    {
        var customerId = await GetCustomerIdAsync();
        if (customerId == null) return Json(new { count = 0 });

        var count = await _db.Notifications
            .CountAsync(x => x.CustomerId == customerId && !x.IsRead);

        return Json(new { count });
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
