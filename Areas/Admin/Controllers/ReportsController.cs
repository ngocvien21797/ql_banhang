using System;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using QuanLyBanHang.Data;

namespace QuanLyBanHang.Areas.Admin.Controllers
{
    [Area("Admin")]
    [Authorize(Roles = "1")]
    public class ReportsController : Controller
    {
        private readonly SalesDbContext _db;

        public ReportsController(SalesDbContext db) => _db = db;

        public IActionResult Index() => RedirectToAction(nameof(RevenueByDate));

        public async Task<IActionResult> RevenueByDate(DateTime? from, DateTime? to)
        {
            var f = from ?? DateTime.Today.AddDays(-7);
            var t = to ?? DateTime.Today;

            var raw = await _db.SalesInvoices
                .Where(x => x.CreatedAt.Date >= f.Date && x.CreatedAt.Date <= t.Date && x.Status != "Cancelled")
                .GroupBy(x => x.CreatedAt.Date)
                .Select(g => new RevenueRow
                {
                    Date = g.Key,
                    Total = g.Sum(x => x.Total),
                    Count = g.Count()
                })
                .OrderBy(x => x.Date)
                .ToListAsync();

            // Fill missing dates
            var data = new List<RevenueRow>();
            var current = f.Date;
            while (current <= t.Date)
            {
                var match = raw.FirstOrDefault(r => r.Date == current);
                data.Add(new RevenueRow
                {
                    Date = current,
                    Total = match?.Total ?? 0,
                    Count = match?.Count ?? 0
                });
                current = current.AddDays(1);
            }

            ViewBag.From = f.ToString("yyyy-MM-dd");
            ViewBag.To = t.ToString("yyyy-MM-dd");
            ViewBag.TotalRevenue = data.Sum(x => x.Total);
            ViewBag.TotalOrders = data.Sum(x => x.Count);
            ViewBag.AvgOrder = data.Sum(x => x.Count) > 0 ? data.Sum(x => x.Total) / data.Sum(x => x.Count) : 0;
            ViewBag.BestDay = data.Any() ? data.Max(x => x.Total) : 0;

            return View(data);
        }

        public async Task<IActionResult> TopProducts(DateTime? from, DateTime? to)
        {
            var f = from ?? DateTime.Today.AddDays(-30);
            var t = to ?? DateTime.Today;

            var data = await _db.SalesItems
                .Include(i => i.Product)
                .Include(i => i.Invoice)
                .Where(i => i.Invoice != null && i.Invoice.Status != "Cancelled" && i.Invoice.CreatedAt.Date >= f.Date && i.Invoice.CreatedAt.Date <= t.Date)
                .GroupBy(i => new { i.ProductId, ProductName = i.Product != null ? i.Product.Name : "" })
                .Select(g => new TopProductRow
                {
                    ProductName = g.Key.ProductName,
                    Quantity = g.Sum(x => x.Quantity),
                    Revenue = g.Sum(x => x.LineTotal)
                })
                .OrderByDescending(x => x.Revenue)
                .Take(20)
                .ToListAsync();

            ViewBag.From = f.ToString("yyyy-MM-dd");
            ViewBag.To = t.ToString("yyyy-MM-dd");
            ViewBag.TotalRevenue = data.Sum(x => x.Revenue);
            ViewBag.TotalQty = data.Sum(x => x.Quantity);

            return View(data);
        }

        public class RevenueRow
        {
            public DateTime Date { get; set; }
            public decimal Total { get; set; }
            public int Count { get; set; }
        }

        public class TopProductRow
        {
            public string ProductName { get; set; } = string.Empty;
            public int Quantity { get; set; }
            public decimal Revenue { get; set; }
        }
    }
}
