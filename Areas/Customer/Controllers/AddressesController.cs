using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using QuanLyBanHang.Data;
using QuanLyBanHang.Models;
using System.Security.Claims;

namespace QuanLyBanHang.Areas.Customer.Controllers;

[Area("Customer")]
[Authorize(Roles = "2")]
public class AddressesController : Controller
{
    private readonly SalesDbContext _db;
    public AddressesController(SalesDbContext db) => _db = db;

    private async Task<long?> GetCustomerIdAsync()
    {
        var uid = GetUserId();
        return await _db.Users.Where(u => u.Id == uid).Select(u => u.CustomerId).FirstOrDefaultAsync();
    }

    [HttpGet]
    public async Task<IActionResult> Index()
    {
        var customerId = await GetCustomerIdAsync();
        if (customerId == null) return Forbid();

        var list = await _db.CustomerAddresses
            .Where(a => a.CustomerId == customerId)
            .OrderByDescending(a => a.IsDefault)
            .ThenBy(a => a.Id)
            .ToListAsync();

        return View(list);
    }

    [HttpGet]
    public IActionResult Create()
    {
        return View();
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Create(CustomerAddress model)
    {
        var customerId = await GetCustomerIdAsync();
        if (customerId == null) return Forbid();

        if (!ModelState.IsValid)
            return View(model);

        var count = await _db.CustomerAddresses.CountAsync(a => a.CustomerId == customerId);

        model.CustomerId = customerId.Value;
        model.IsDefault = count == 0;

        _db.CustomerAddresses.Add(model);
        await _db.SaveChangesAsync();

        TempData["Ok"] = "Thêm địa chỉ thành công!";
        return RedirectToAction(nameof(Index));
    }

    [HttpGet]
    public async Task<IActionResult> Edit(long id)
    {
        var customerId = await GetCustomerIdAsync();
        if (customerId == null) return Forbid();

        var addr = await _db.CustomerAddresses.FirstOrDefaultAsync(a => a.Id == id && a.CustomerId == customerId);
        if (addr == null) return NotFound();

        return View(addr);
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Edit(long id, CustomerAddress model)
    {
        if (id != model.Id) return BadRequest();

        var customerId = await GetCustomerIdAsync();
        if (customerId == null) return Forbid();

        var addr = await _db.CustomerAddresses.FirstOrDefaultAsync(a => a.Id == id && a.CustomerId == customerId);
        if (addr == null) return NotFound();

        if (!ModelState.IsValid)
            return View(model);

        addr.Label = model.Label;
        addr.ReceiverName = model.ReceiverName;
        addr.Phone = model.Phone;
        addr.Province = model.Province;
        addr.District = model.District;
        addr.Ward = model.Ward;
        addr.Address = model.Address;

        await _db.SaveChangesAsync();

        TempData["Ok"] = "Cập nhật địa chỉ thành công!";
        return RedirectToAction(nameof(Index));
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Delete(long id)
    {
        var customerId = await GetCustomerIdAsync();
        if (customerId == null) return Forbid();

        var addr = await _db.CustomerAddresses.FirstOrDefaultAsync(a => a.Id == id && a.CustomerId == customerId);
        if (addr == null) return NotFound();

        var wasDefault = addr.IsDefault;

        _db.CustomerAddresses.Remove(addr);
        await _db.SaveChangesAsync();

        if (wasDefault)
        {
            var next = await _db.CustomerAddresses
                .Where(a => a.CustomerId == customerId)
                .OrderBy(a => a.Id)
                .FirstOrDefaultAsync();

            if (next != null)
            {
                next.IsDefault = true;
                await _db.SaveChangesAsync();
            }
        }

        TempData["Ok"] = "Xóa địa chỉ thành công!";
        return RedirectToAction(nameof(Index));
    }

    [HttpPost]
    public async Task<IActionResult> SetDefault(long id)
    {
        var customerId = await GetCustomerIdAsync();
        if (customerId == null) return Json(new { success = false });

        var addr = await _db.CustomerAddresses.FirstOrDefaultAsync(a => a.Id == id && a.CustomerId == customerId);
        if (addr == null) return Json(new { success = false });

        var old = await _db.CustomerAddresses.Where(a => a.CustomerId == customerId && a.IsDefault).ToListAsync();
        foreach (var o in old) o.IsDefault = false;

        addr.IsDefault = true;
        await _db.SaveChangesAsync();

        return Json(new { success = true });
    }

    private long GetUserId()
    {
        var s = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
        return long.TryParse(s, out var id) ? id : 0;
    }
}
