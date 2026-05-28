using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using QuanLyBanHang.Data;
using QuanLyBanHang.Helpers;
using QuanLyBanHang.ViewModels;

namespace QuanLyBanHang.Areas.Customer.Controllers;
[Area("Customer")]
public class CartController : Controller
{
    private const string CartKey = "CART";
    private readonly SalesDbContext _db;

    public CartController(SalesDbContext db) => _db = db;

    [HttpGet]
    public async Task<IActionResult> Index()
    {
        var lines = await BuildCartLinesAsync();
        ViewBag.Subtotal = lines.Sum(x => x.LineTotal);
        ViewBag.CartCount = lines.Sum(x => x.Quantity);

        var now = DateTime.Now;
        ViewBag.ActivePromotions = await _db.Promotions
            .Where(p => p.IsActive && p.StartDate <= now && p.EndDate >= now)
            .ToListAsync();

        return View(lines);
    }

    [HttpPost]
    public async Task<IActionResult> Add(long productId, int qty = 1, string? returnUrl = null)
    {
        if (qty <= 0) qty = 1;

        var product = await _db.Products.FirstOrDefaultAsync(p => p.Id == productId && p.IsActive);
        if (product == null) return NotFound();

        var cart = GetCart();
        var existing = cart.FirstOrDefault(x => x.ProductId == productId);
        if (existing == null)
            cart.Add(new CartItemVM { ProductId = productId, Quantity = qty });
        else
            existing.Quantity += qty;

        SaveCart(cart);

        if (!string.IsNullOrWhiteSpace(returnUrl) && Url.IsLocalUrl(returnUrl))
            return LocalRedirect(returnUrl);

        return RedirectToAction(nameof(Index));
    }

    [HttpPost]
    public async Task<IActionResult> BuyNow(long productId, int qty = 1)
    {
        if (qty <= 0) qty = 1;

        var product = await _db.Products.FirstOrDefaultAsync(p => p.Id == productId && p.IsActive);
        if (product == null) return NotFound();

        var cart = new List<CartItemVM> { new CartItemVM { ProductId = productId, Quantity = qty } };
        SaveCart(cart);

        return RedirectToAction("Index", "Checkout");
    }

    [HttpPost]
    public async Task<IActionResult> Update(List<long> productIds, List<int> qtys)
    {
        if (productIds.Count != qtys.Count) return BadRequest();

        var cart = GetCart();

        for (int i = 0; i < productIds.Count; i++)
        {
            var pid = productIds[i];
            var q = qtys[i];

            var item = cart.FirstOrDefault(x => x.ProductId == pid);
            if (item == null) continue;

            if (q <= 0) cart.Remove(item);
            else item.Quantity = q;
        }

        SaveCart(cart);
        return RedirectToAction(nameof(Index));
    }

    [HttpPost]
    public async Task<IActionResult> UpdateQty(long productId, int qty)
    {
        var cart = GetCart();
        if (qty <= 0)
        {
            cart.RemoveAll(x => x.ProductId == productId);
        }
        else
        {
            var item = cart.FirstOrDefault(x => x.ProductId == productId);
            if (item != null) item.Quantity = qty;
        }
        SaveCart(cart);

        var lines = await BuildCartLinesAsync();
        var subtotal = lines.Sum(x => x.LineTotal);
        var cartCount = lines.Sum(x => x.Quantity);

        var stocks = new Dictionary<long, int>();
        foreach (var line in lines)
        {
            var p = await _db.Products.FindAsync(line.Product.Id);
            if (p != null) stocks[p.Id] = p.Stock;
        }

        return Json(new CartAjaxResult
        {
            Success = true,
            CartCount = cartCount,
            Subtotal = subtotal,
            Total = subtotal,
            Stocks = stocks
        });
    }

    [HttpPost]
    public async Task<IActionResult> RemoveAjax(long productId)
    {
        var cart = GetCart();
        cart.RemoveAll(x => x.ProductId == productId);
        SaveCart(cart);

        var lines = await BuildCartLinesAsync();
        var subtotal = lines.Sum(x => x.LineTotal);
        var cartCount = lines.Sum(x => x.Quantity);

        return Json(new CartAjaxResult
        {
            Success = true,
            CartCount = cartCount,
            Subtotal = subtotal,
            Total = subtotal
        });
    }

    [HttpPost]
    public IActionResult Remove(long productId)
    {
        var cart = GetCart();
        cart.RemoveAll(x => x.ProductId == productId);
        SaveCart(cart);
        return RedirectToAction(nameof(Index));
    }

    [HttpPost]
    public IActionResult Clear()
    {
        HttpContext.Session.Remove(CartKey);
        return RedirectToAction(nameof(Index));
    }

    private List<CartItemVM> GetCart()
        => HttpContext.Session.GetObject<List<CartItemVM>>(CartKey) ?? new List<CartItemVM>();

    private void SaveCart(List<CartItemVM> cart)
        => HttpContext.Session.SetObject(CartKey, cart);

    private async Task<List<CartLineVM>> BuildCartLinesAsync()
    {
        var cart = GetCart();
        if (cart.Count == 0) return new List<CartLineVM>();

        var ids = cart.Select(x => x.ProductId).Distinct().ToList();
        var products = await _db.Products.Include(p => p.Category).Where(p => ids.Contains(p.Id)).ToListAsync();

        var lines = (from c in cart
                     join p in products on c.ProductId equals p.Id
                     select new CartLineVM
                     {
                         Product = p,
                         Quantity = c.Quantity
                     }).ToList();

        return lines;
    }
}
