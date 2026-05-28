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

    public CustomersController(SalesDbContext db)
    {
        _db = db;
    }

    public async Task<IActionResult> Index(string? search, int page = 1, int pageSize = 10)
    {
        var query = _db.Customers.Select(c => new CustomerViewModel
        {
            Id = c.Id,
            Name = c.Name,
            Phone = c.Phone,
            Address = c.Address,
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

    public IActionResult Create() => View();

    [HttpPost]
    public async Task<IActionResult> Create(CustomerEntity customer)
    {
        if (!ModelState.IsValid) return View(customer);
        _db.Customers.Add(customer);
        await _db.SaveChangesAsync();
        return RedirectToAction(nameof(Index));
    }

    public async Task<IActionResult> Edit(long id)
    {
        var c = await _db.Customers.FindAsync(id);
        if (c == null) return NotFound();
        return View(c);
    }

    [HttpPost]
    public async Task<IActionResult> Edit(long id, CustomerEntity customer)
    {
        if (id != customer.Id) return BadRequest();
        if (!ModelState.IsValid) return View(customer);
        _db.Update(customer);
        await _db.SaveChangesAsync();
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
        _db.Customers.Remove(c);
        await _db.SaveChangesAsync();
        return RedirectToAction(nameof(Index));
    }

    public class CustomerViewModel
    {
        public long Id { get; set; }
        public string Name { get; set; } = "";
        public string? Phone { get; set; }
        public string? Address { get; set; }
        public decimal WalletBalance { get; set; }
        public int OrderCount { get; set; }
        public decimal TotalSpent { get; set; }
        public DateTime? LastOrderDate { get; set; }
    }
}
