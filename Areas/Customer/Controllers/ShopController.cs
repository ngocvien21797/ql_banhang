using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.SignalR;
using Microsoft.EntityFrameworkCore;
using QuanLyBanHang.Data;
using QuanLyBanHang.Helpers;
using QuanLyBanHang.Hubs;
using QuanLyBanHang.Models;
using QuanLyBanHang.ViewModels;
using System.Security.Claims;

namespace QuanLyBanHang.Areas.Customer.Controllers;
[Area("Customer")]
public class ShopController : Controller
{
    private readonly SalesDbContext _db;
    private readonly IHttpContextAccessor _httpAccessor;
    private readonly IWebHostEnvironment _env;
    private readonly IHubContext<OrderHub> _hubContext;

    public ShopController(SalesDbContext db, IHttpContextAccessor httpAccessor, IWebHostEnvironment env, IHubContext<OrderHub> hubContext)
    {
        _db = db;
        _httpAccessor = httpAccessor;
        _env = env;
        _hubContext = hubContext;
    }

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

        ViewBag.Banners = await _db.Banners
            .Where(b => b.IsActive)
            .OrderBy(b => b.SortOrder)
            .ToListAsync();

        ViewBag.RecentArticles = await _db.Articles
            .Where(a => a.IsActive)
            .OrderByDescending(a => a.CreatedAt)
            .Take(4)
            .ToListAsync();

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

        return View(p);
    }

    [Authorize(Roles = "2")]
    public async Task<IActionResult> MyOrders()
    {
        var customerId = await GetCustomerIdAsync();
        if (customerId == null) return Forbid();

        var list = await _db.SalesInvoices
            .Where(x => x.CustomerId == customerId)
            .Include(x => x.Items).ThenInclude(i => i.Product)
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
    public async Task<IActionResult> AddReview(long productId, int rating, string? content, IFormFile? imageFile)
    {
        var customerId = await GetCustomerIdAsync();
        if (customerId == null) return Forbid();

        if (rating < 1 || rating > 5) rating = 5;

        // Kiểm tra khách hàng đã mua và nhận hàng thành công chưa
        var hasPurchased = await _db.SalesInvoices
            .AnyAsync(inv => inv.CustomerId == customerId
                && inv.Status == "Completed"
                && inv.Items.Any(i => i.ProductId == productId));

        if (!hasPurchased)
        {
            TempData["Error"] = "Bạn cần mua sản phẩm và nhận hàng trước khi đánh giá.";
            return RedirectToAction(nameof(Product), new { id = productId });
        }

        var existing = await _db.Reviews
            .FirstOrDefaultAsync(r => r.ProductId == productId && r.CustomerId == customerId);

        string? imagePath = existing?.ImagePath;

        if (imageFile != null && imageFile.Length > 0)
        {
            var saveResult = await SaveReviewImageAsync(imageFile);
            if (!saveResult.Success)
            {
                TempData["Error"] = saveResult.ErrorMessage ?? "Upload ảnh thất bại.";
                return RedirectToAction(nameof(Product), new { id = productId });
            }

            // Xoá ảnh cũ nếu có
            if (!string.IsNullOrWhiteSpace(imagePath))
                DeleteImageIfExists(imagePath);

            imagePath = saveResult.RelativePath;
        }

        if (existing != null)
        {
            existing.Rating = rating;
            existing.Content = content;
            existing.ImagePath = imagePath;
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
                ImagePath = imagePath,
                CreatedAt = DateTime.Now
            });
        }

        await _db.SaveChangesAsync();
        TempData["Ok"] = "Đánh giá của bạn đã được ghi nhận!";
        return RedirectToAction(nameof(Product), new { id = productId });
    }

    private async Task<(bool Success, string? RelativePath, string? ErrorMessage)> SaveReviewImageAsync(IFormFile file)
    {
        const long maxBytes = 5 * 1024 * 1024;
        if (file.Length > maxBytes)
            return (false, null, "Ảnh quá lớn (tối đa 5MB).");

        var ext = Path.GetExtension(file.FileName).ToLowerInvariant();
        var allowed = new[] { ".jpg", ".jpeg", ".png", ".webp" };
        if (!allowed.Contains(ext))
            return (false, null, "Chỉ cho phép JPG/JPEG/PNG/WEBP.");

        var dir = Path.Combine(_env.WebRootPath, "uploads", "reviews");
        Directory.CreateDirectory(dir);

        var fileName = $"{Guid.NewGuid():N}{ext}";
        var physicalPath = Path.Combine(dir, fileName);

        using (var stream = new FileStream(physicalPath, FileMode.Create))
        {
            await file.CopyToAsync(stream);
        }

        var relative = $"/uploads/reviews/{fileName}";
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

    [Authorize(Roles = "2")]
    [HttpPost]
    public async Task<IActionResult> CancelOrder(long id)
    {
        var customerId = await GetCustomerIdAsync();
        if (customerId == null) return Forbid();

        var inv = await _db.SalesInvoices
            .Include(x => x.Items)
            .FirstOrDefaultAsync(x => x.Id == id && x.CustomerId == customerId);

        if (inv == null) return NotFound();
        if (inv.Status != "Pending")
        {
            TempData["Error"] = "Chỉ có thể hủy đơn hàng ở trạng thái 'Chờ xác nhận'.";
            return RedirectToAction(nameof(Details), new { id });
        }

        using var tx = await _db.Database.BeginTransactionAsync();
        try
        {
                inv.Status = "Cancelled";

            foreach (var item in inv.Items)
            {
                var product = await _db.Products.FindAsync(item.ProductId);
                if (product != null)
                {
                    product.Stock += item.Quantity;
                }

                _db.StockLedgers.Add(new StockLedger
                {
                    ProductId = item.ProductId,
                    Type = "IN",
                    Quantity = item.Quantity,
                    RefType = "SALE_CANCEL",
                    RefId = inv.Id,
                    OccurredAt = DateTime.Now
                });
            }

            await _db.SaveChangesAsync();

            _db.Notifications.Add(new Notification
            {
                CustomerId = customerId.Value,
                Title = $"Đã hủy đơn {inv.Code}",
                Message = $"Đơn hàng {inv.Code} đã được hủy theo yêu cầu của bạn.",
                Url = $"/Shop/Details/{inv.Id}",
                CreatedAt = DateTime.Now
            });
            await _db.SaveChangesAsync();

            await tx.CommitAsync();
            TempData["Ok"] = "Đơn hàng đã được hủy thành công.";
        }
        catch
        {
            await tx.RollbackAsync();
            TempData["Error"] = "Có lỗi xảy ra, vui lòng thử lại.";
        }

        return RedirectToAction(nameof(Details), new { id });
    }

    [Authorize(Roles = "2")]
    [HttpPost]
    public async Task<IActionResult> ConfirmReceived(long id)
    {
        var customerId = await GetCustomerIdAsync();
        if (customerId == null) return Forbid();

        var inv = await _db.SalesInvoices
            .Include(x => x.Items).ThenInclude(i => i.Product)
            .FirstOrDefaultAsync(x => x.Id == id && x.CustomerId == customerId);

        if (inv == null) return NotFound();
        if (inv.Status != "Shipped")
        {
            TempData["Error"] = "Đơn hàng chưa được giao, không thể xác nhận.";
            return RedirectToAction(nameof(Details), new { id });
        }

        inv.Status = "Completed";
        inv.PaymentStatus = "Paid";
        inv.PaidAt = DateTime.Now;
        await _db.SaveChangesAsync();

        await _hubContext.Clients.All.SendAsync("OrderStatusChanged", inv.Id, "Completed");

        _db.Notifications.Add(new Notification
        {
            CustomerId = customerId.Value,
            Title = $"Đã nhận hàng - {inv.Code}",
            Message = $"Bạn đã xác nhận đã nhận được đơn hàng {inv.Code}. Cảm ơn bạn đã mua sắm!",
            Url = $"/Shop/Details/{inv.Id}",
            CreatedAt = DateTime.Now
        });
        await _db.SaveChangesAsync();

        TempData["Ok"] = "Xác nhận nhận hàng thành công!";
        TempData["ShowReview"] = "1";
        return RedirectToAction(nameof(Details), new { id });
    }

    [Authorize(Roles = "2")]
    [HttpPost]
    public async Task<IActionResult> Reorder(long id)
    {
        var customerId = await GetCustomerIdAsync();
        if (customerId == null) return Forbid();

        var inv = await _db.SalesInvoices
            .Include(x => x.Items).ThenInclude(i => i.Product)
            .FirstOrDefaultAsync(x => x.Id == id && x.CustomerId == customerId);

        if (inv == null) return NotFound();

        var cart = new List<CartItemVM>();
        foreach (var item in inv.Items)
        {
            if (item.Product != null && item.Product.IsActive)
            {
                var existing = cart.FirstOrDefault(c => c.ProductId == item.ProductId);
                if (existing == null)
                    cart.Add(new CartItemVM { ProductId = item.ProductId, Quantity = item.Quantity });
                else
                    existing.Quantity += item.Quantity;
            }
        }

        if (cart.Count == 0)
        {
            TempData["Error"] = "Không có sản phẩm nào trong đơn hàng còn kinh doanh.";
            return RedirectToAction(nameof(Details), new { id });
        }

        HttpContext.Session.SetObject("CART", cart);

        TempData["Ok"] = "Đã thêm sản phẩm vào giỏ hàng!";
        return RedirectToAction("Index", "Cart");
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
