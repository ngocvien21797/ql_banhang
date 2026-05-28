using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using QuanLyBanHang.Data;
using QuanLyBanHang.Models;
using System.Security.Claims;

namespace QuanLyBanHang.Areas.Customer.Controllers;
[Area("Customer")]
public class ShopController : Controller
{
    private readonly SalesDbContext _db;
    public ShopController(SalesDbContext db) => _db = db;

    [AllowAnonymous]
    public async Task<IActionResult> Index(
        string? q, long? categoryId,
        decimal? minPrice, decimal? maxPrice,
        string? sort,
        int page = 1, int pageSize = 20)
    {
        ViewBag.Categories = await _db.Categories.OrderBy(c => c.Name).ToListAsync();
        ViewBag.Query = q ?? "";
        ViewBag.CategoryId = categoryId;
        ViewBag.MinPrice = minPrice;
        ViewBag.MaxPrice = maxPrice;
        ViewBag.Sort = sort ?? "name";

        var query = _db.Products
            .Include(p => p.Category)
            .Where(p => p.IsActive);

        if (categoryId.HasValue)
            query = query.Where(p => p.CategoryId == categoryId);

        if (!string.IsNullOrWhiteSpace(q))
            query = query.Where(p => p.Name.Contains(q) || p.Sku.Contains(q));

        if (minPrice.HasValue && minPrice > 0)
            query = query.Where(p => p.Price >= minPrice.Value);

        if (maxPrice.HasValue && maxPrice > 0)
            query = query.Where(p => p.Price <= maxPrice.Value);

        query = sort switch
        {
            "price_asc" => query.OrderBy(p => p.Price),
            "price_desc" => query.OrderByDescending(p => p.Price),
            "newest" => query.OrderByDescending(p => p.Id),
            _ => query.OrderBy(p => p.Name),
        };

        var total = await query.CountAsync();
        var products = await query.Skip((page - 1) * pageSize).Take(pageSize).ToListAsync();

        ViewBag.Page = page;
        ViewBag.PageSize = pageSize;
        ViewBag.Total = total;
        ViewBag.TotalPages = (int)Math.Ceiling((double)total / pageSize);

        var now = DateTime.Now;
        var activePromos = await _db.Promotions
            .Include(p => p.PromotionProducts)
            .Where(p => p.IsActive && p.StartDate <= now && p.EndDate >= now)
            .ToListAsync();
        ViewBag.ActivePromotions = activePromos;

        var productIds = products.Select(p => p.Id).ToList();
        var avgRatings = await _db.Reviews
            .Where(r => productIds.Contains(r.ProductId))
            .GroupBy(r => r.ProductId)
            .Select(g => new { ProductId = g.Key, Avg = g.Average(r => (double)r.Rating), Count = g.Count() })
            .ToListAsync();

        ViewBag.AvgRatings = avgRatings.ToDictionary(x => x.ProductId, x => (Avg: x.Avg, Count: x.Count));

        var customerId = await GetCustomerIdAsync();
        HashSet<long> wishlistIds = new();
        if (customerId.HasValue)
        {
            wishlistIds = (await _db.Wishlists
                .Where(w => w.CustomerId == customerId && productIds.Contains(w.ProductId))
                .Select(w => w.ProductId)
                .ToListAsync()).ToHashSet();
        }
        ViewBag.WishlistIds = wishlistIds;

        return View(products);
    }

    [AllowAnonymous]
    public async Task<IActionResult> Product(long id)
    {
        var p = await _db.Products
            .Include(x => x.Category)
            .FirstOrDefaultAsync(x => x.Id == id && x.IsActive);
        if (p == null) return NotFound();

        var now = DateTime.Now;
        var promos = await _db.Promotions
            .Include(promo => promo.PromotionProducts)
            .Where(promo => promo.IsActive && promo.StartDate <= now && promo.EndDate >= now)
            .ToListAsync();
        ViewBag.ActivePromotions = promos;

        var reviews = await _db.Reviews
            .Include(r => r.Customer)
            .Where(r => r.ProductId == id)
            .OrderByDescending(r => r.CreatedAt)
            .ToListAsync();
        ViewBag.Reviews = reviews;

        var avgRating = reviews.Any() ? reviews.Average(r => r.Rating) : 0;
        ViewBag.AvgRating = avgRating;
        ViewBag.ReviewCount = reviews.Count;

        var related = await _db.Products
            .Where(x => x.CategoryId == p.CategoryId && x.Id != id && x.IsActive)
            .Take(4)
            .ToListAsync();
        ViewBag.RelatedProducts = related;

        var customerId = await GetCustomerIdAsync();
        ViewBag.IsWishlisted = customerId.HasValue && await _db.Wishlists
            .AnyAsync(w => w.CustomerId == customerId && w.ProductId == id);

        ViewBag.CanReview = customerId.HasValue && await _db.SalesInvoices
            .AnyAsync(inv => inv.CustomerId == customerId
                && inv.Status == "Completed"
                && inv.Items.Any(i => i.ProductId == id));

        return View(p);
    }

    [Authorize(Roles = "2")]
    public async Task<IActionResult> MyOrders()
    {
        var customerId = await GetCustomerIdAsync();
        if (customerId == null) return Forbid();

        var list = await _db.SalesInvoices
            .Where(x => x.CustomerId == customerId)
            .OrderByDescending(x => x.Id)
            .ToListAsync();

        return View(list);
    }

    [Authorize(Roles = "2")]
    public async Task<IActionResult> Details(long id)
    {
        var customerId = await GetCustomerIdAsync();
        if (customerId == null) return Forbid();

        var inv = await _db.SalesInvoices
            .Include(x => x.Items).ThenInclude(i => i.Product)
            .FirstOrDefaultAsync(x => x.Id == id);

        if (inv == null) return NotFound();
        if (inv.CustomerId != customerId) return Forbid();

        return View(inv);
    }

    [Authorize(Roles = "2")]
    [HttpPost]
    public async Task<IActionResult> AddReview(long productId, int rating, string? content)
    {
        var customerId = await GetCustomerIdAsync();
        if (customerId == null) return Forbid();

        if (rating < 1 || rating > 5) rating = 5;

        var existing = await _db.Reviews
            .FirstOrDefaultAsync(r => r.ProductId == productId && r.CustomerId == customerId);
        if (existing != null)
        {
            existing.Rating = rating;
            existing.Content = content;
            existing.CreatedAt = DateTime.Now;
        }
        else
        {
            _db.Reviews.Add(new Review
            {
                ProductId = productId,
                CustomerId = customerId.Value,
                Rating = rating,
                Content = content,
                CreatedAt = DateTime.Now
            });
        }

        await _db.SaveChangesAsync();
        TempData["Ok"] = "Đánh giá của bạn đã được ghi nhận!";
        return RedirectToAction(nameof(Product), new { id = productId });
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
