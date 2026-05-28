using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using QuanLyBanHang.Data;

namespace QuanLyBanHang.Areas.Admin.Controllers;
[Area("Admin")]
[Authorize(Roles = "1")]
public class AdminController : Controller
{
    private readonly SalesDbContext _db;
    public AdminController(SalesDbContext db) => _db = db;

    public async Task<IActionResult> Index()
    {
        ViewBag.ProductCount = await _db.Products.CountAsync();
        ViewBag.CustomerCount = await _db.Customers.CountAsync();
        ViewBag.SalesCount = await _db.SalesInvoices.CountAsync();
        ViewBag.Revenue = await _db.SalesInvoices.SumAsync(x => (decimal?)x.Total) ?? 0;

        var today = DateTime.Today;
        var yesterday = today.AddDays(-1);

        ViewBag.OrdersToday = await _db.SalesInvoices.CountAsync(x => x.CreatedAt >= today);
        ViewBag.OrdersYesterday = await _db.SalesInvoices.CountAsync(x => x.CreatedAt >= yesterday && x.CreatedAt < today);
        ViewBag.UnpaidCount = await _db.SalesInvoices.CountAsync(x => x.PaymentStatus != "Paid");

        ViewBag.RevenueToday = await _db.SalesInvoices.Where(x => x.CreatedAt >= today).SumAsync(x => (decimal?)x.Total) ?? 0;
        ViewBag.RevenueYesterday = await _db.SalesInvoices.Where(x => x.CreatedAt >= yesterday && x.CreatedAt < today).SumAsync(x => (decimal?)x.Total) ?? 0;

        ViewBag.LowStock = await _db.Products
            .Where(p => p.Stock <= 5 && p.IsActive)
            .OrderBy(p => p.Stock)
            .Take(8)
            .ToListAsync();

        ViewBag.RecentOrders = await _db.SalesInvoices
            .Include(x => x.Customer)
            .OrderByDescending(x => x.Id)
            .Take(8)
            .ToListAsync();

        // Chart data: Revenue last 7 days
        var sevenDaysAgo = DateTime.Today.AddDays(-6);
        var dailyRevenue = await _db.SalesInvoices
            .Where(x => x.CreatedAt >= sevenDaysAgo && x.PaymentStatus == "Paid")
            .GroupBy(x => x.CreatedAt.Date)
            .Select(g => new { Date = g.Key, Total = g.Sum(x => (decimal?)x.Total) ?? 0, Count = g.Count() })
            .ToListAsync();

        var chartLabels = new List<string>();
        var chartData = new List<decimal>();
        var chartCounts = new List<int>();
        for (int i = 6; i >= 0; i--)
        {
            var d = DateTime.Today.AddDays(-i);
            chartLabels.Add(d.ToString("dd/MM"));
            var day = dailyRevenue.FirstOrDefault(x => x.Date == d);
            chartData.Add(day?.Total ?? 0);
            chartCounts.Add(day?.Count ?? 0);
        }
        ViewBag.ChartLabels = chartLabels;
        ViewBag.ChartData = chartData;
        ViewBag.ChartCounts = chartCounts;

        // Top 5 products by revenue
        var thirtyDaysAgo = DateTime.Today.AddDays(-30);
        var topProducts = await _db.SalesItems
            .Include(i => i.Product)
            .Where(i => i.Invoice!.CreatedAt >= thirtyDaysAgo && i.Invoice.PaymentStatus == "Paid")
            .GroupBy(i => i.Product!.Name)
            .Select(g => new { Name = g.Key, Qty = g.Sum(i => i.Quantity), Revenue = g.Sum(i => (decimal?)i.LineTotal) ?? 0 })
            .OrderByDescending(x => x.Revenue)
            .Take(5)
            .ToListAsync();

        ViewBag.TopProducts = topProducts;

        // Revenue by category (pie chart)
        var revenueByCategory = await _db.SalesItems
            .Include(i => i.Product)
            .ThenInclude(p => p!.Category)
            .Where(i => i.Invoice!.CreatedAt >= thirtyDaysAgo && i.Invoice.PaymentStatus == "Paid")
            .GroupBy(i => i.Product!.Category!.Name)
            .Select(g => new { Category = g.Key, Total = g.Sum(i => (decimal?)i.LineTotal) ?? 0 })
            .OrderByDescending(x => x.Total)
            .ToListAsync();

        ViewBag.RevenueByCategory = revenueByCategory;

        return View();
    }
}
