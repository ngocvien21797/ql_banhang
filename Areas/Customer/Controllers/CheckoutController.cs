using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using QuanLyBanHang.Data;
using QuanLyBanHang.Helpers;
using QuanLyBanHang.Models;
using QuanLyBanHang.ViewModels;
using System.Security.Claims;

namespace QuanLyBanHang.Areas.Customer.Controllers;
[Area("Customer")]
[Authorize(Roles = "2")]
public class CheckoutController : Controller
{
    private const string CartKey = "CART";
    private readonly SalesDbContext _db;

    public CheckoutController(SalesDbContext db) => _db = db;

    [HttpGet]
    public async Task<IActionResult> Index()
    {
        var lines = await BuildCartLinesAsync();
        if (lines.Count == 0) return RedirectToAction("Index", "Cart");

        var uid = GetUserId();
        var customer = await _db.Users.Where(u => u.Id == uid).Select(u => u.Customer).FirstOrDefaultAsync();

        var subtotal = lines.Sum(x => x.LineTotal);
        var productIds = lines.Select(l => l.Product.Id).ToHashSet();
        var applicablePromos = await LoadApplicablePromotionsAsync(productIds, subtotal);
        ViewBag.ApplicablePromotions = applicablePromos;

        var savedAddresses = new List<QuanLyBanHang.Models.CustomerAddress>();
        if (customer != null)
        {
            savedAddresses = await _db.CustomerAddresses
                .Where(a => a.CustomerId == customer.Id)
                .OrderByDescending(a => a.IsDefault)
                .ThenBy(a => a.Id)
                .ToListAsync();
        }

        var defaultAddr = savedAddresses.FirstOrDefault(a => a.IsDefault);

        var userEmail = customer?.Email ?? User.Identity?.Name;
        var vm = new CheckoutVM
        {
            ReceiverName = defaultAddr?.ReceiverName ?? customer?.Name ?? User.Identity?.Name ?? "",
            Phone = defaultAddr?.Phone ?? customer?.Phone,
            Email = userEmail,
            Address = defaultAddr?.Address ?? customer?.Address,
            SelectedAddressId = defaultAddr?.Id,
            PaymentMethod = "COD",
            ShippingMethod = "standard",
            Lines = lines,
            Subtotal = subtotal,
            ShippingFee = 0,
            Discount = 0,
            Total = subtotal,
            SavedAddresses = savedAddresses
        };

        return View(vm);
    }

    private async Task<List<Promotion>> LoadApplicablePromotionsAsync(HashSet<long> productIds, decimal subtotal)
    {
        var now = DateTime.Now;
        var promos = await _db.Promotions
            .Include(p => p.PromotionProducts)
            .Where(p => p.IsActive && p.StartDate <= now && p.EndDate >= now)
            .ToListAsync();

        return promos.Where(p =>
        {
            if (p.MinOrderValue.HasValue && subtotal < p.MinOrderValue.Value)
                return false;
            if (p.PromotionProducts.Count > 0)
                return p.PromotionProducts.Any(pp => productIds.Contains(pp.ProductId));
            return true;
        }).ToList();
    }

    [HttpPost]
    public async Task<IActionResult> PlaceOrder(CheckoutVM vm)
    {
        var lines = await BuildCartLinesAsync();
        if (lines.Count == 0)
        {
            TempData["Error"] = "Giỏ hàng đang trống.";
            return RedirectToAction("Index", "Cart");
        }

        vm.Lines = lines;
        vm.Subtotal = lines.Sum(x => x.LineTotal);

        if (string.IsNullOrWhiteSpace(vm.ReceiverName))
            ModelState.AddModelError("ReceiverName", "Vui lòng chọn hoặc thêm địa chỉ giao hàng.");
        else if (vm.ReceiverName.Trim().Length > 150)
            ModelState.AddModelError("ReceiverName", "Họ tên không quá 150 ký tự.");

        if (string.IsNullOrWhiteSpace(vm.Phone))
            ModelState.AddModelError("Phone", "Vui lòng chọn hoặc thêm địa chỉ giao hàng.");
        else if (!System.Text.RegularExpressions.Regex.IsMatch(vm.Phone.Trim(), @"^0[0-9]{9,10}$"))
            ModelState.AddModelError("Phone", "Số điện thoại không hợp lệ (10-11 số, bắt đầu bằng 0).");

        if (string.IsNullOrWhiteSpace(vm.Email))
            ModelState.AddModelError("Email", "Vui lòng nhập email để nhận xác nhận đơn hàng.");
        else if (!System.Text.RegularExpressions.Regex.IsMatch(vm.Email.Trim(), @"^[^@\s]+@[^@\s]+\.[^@\s]+$"))
            ModelState.AddModelError("Email", "Email không hợp lệ.");
        else if (vm.Email.Trim().Length > 100)
            ModelState.AddModelError("Email", "Email không quá 100 ký tự.");

        if (string.IsNullOrWhiteSpace(vm.Address))
            ModelState.AddModelError("Address", "Vui lòng chọn hoặc thêm địa chỉ giao hàng.");
        else if (vm.Address.Trim().Length > 255)
            ModelState.AddModelError("Address", "Địa chỉ không quá 255 ký tự.");

        if (string.IsNullOrWhiteSpace(vm.ReceiverName) || string.IsNullOrWhiteSpace(vm.Phone) || string.IsNullOrWhiteSpace(vm.Address))
            ModelState.AddModelError("SelectedAddressId", "Vui lòng chọn địa chỉ giao hàng hoặc thêm địa chỉ mới.");

        vm.ShippingFee = CalcShipping(vm.ShippingMethod, vm.Province);
        var uid = GetUserId();
        var customerId = await _db.Users.Where(u => u.Id == uid).Select(u => u.CustomerId).FirstOrDefaultAsync();
        if (!string.IsNullOrWhiteSpace(vm.VoucherCode))
        {
            var promo = await _db.Promotions
                .Include(p => p.PromotionProducts)
                .FirstOrDefaultAsync(p => p.Code == vm.VoucherCode.Trim().ToUpper()
                    && p.IsActive
                    && p.StartDate <= DateTime.Now
                    && p.EndDate >= DateTime.Now);
            if (promo != null)
            {
                if (!promo.MinOrderValue.HasValue || vm.Subtotal >= promo.MinOrderValue.Value)
                {
                    bool eligible;
                    if (promo.PromotionProducts.Count == 0)
                    {
                        ModelState.AddModelError("VoucherCode", "Mã giảm giá không áp dụng cho sản phẩm nào.");
                        eligible = false;
                    }
                    else
                    {
                        var eligibleIds = promo.PromotionProducts.Select(pp => pp.ProductId).ToHashSet();
                        eligible = lines.Any(l => eligibleIds.Contains(l.Product.Id));
                        if (!eligible)
                            ModelState.AddModelError("VoucherCode", "Mã giảm giá không áp dụng cho sản phẩm trong giỏ hàng.");
                    }
                    if (eligible)
                    {
                        // Check max usage count
                        if (promo.MaxUsageCount.HasValue && promo.UsageCount >= promo.MaxUsageCount.Value)
                        {
                            ModelState.AddModelError("VoucherCode", "Mã giảm giá đã hết lượt sử dụng.");
                            eligible = false;
                        }
                        // Check max usage per customer
                        else if (promo.MaxUsagePerCustomer.HasValue && customerId.HasValue)
                        {
                            var usedCount = await _db.PromotionUsages
                                .CountAsync(u => u.PromotionId == promo.Id && u.CustomerId == customerId.Value);
                            if (usedCount >= promo.MaxUsagePerCustomer.Value)
                            {
                                ModelState.AddModelError("VoucherCode", "Bạn đã hết lượt sử dụng mã này.");
                                eligible = false;
                            }
                        }
                    }
                    if (eligible)
                    {
                        if (promo.DiscountType == "Percentage")
                            vm.Discount = vm.Subtotal * promo.DiscountValue / 100;
                        else
                            vm.Discount = promo.DiscountValue;
                        if (vm.Discount > vm.Subtotal) vm.Discount = vm.Subtotal;
                        ViewBag.VoucherMsg = $"Áp dụng mã {promo.Code} thành công!";
                    }
                }
                else
                    ModelState.AddModelError("VoucherCode", $"Đơn hàng tối thiểu {promo.MinOrderValue.Value:N0}₫ để áp dụng mã này.");
            }
            else
                ModelState.AddModelError("VoucherCode", "Mã giảm giá không hợp lệ.");
        }
        vm.Total = vm.Subtotal + vm.ShippingFee - vm.Discount;

        if (!ModelState.IsValid)
        {
            ViewBag.ValidationErrors = true;
            var productIds = lines.Select(l => l.Product.Id).ToHashSet();
            ViewBag.ApplicablePromotions = await LoadApplicablePromotionsAsync(productIds, vm.Subtotal);
            if (customerId.HasValue)
            {
                vm.SavedAddresses = await _db.CustomerAddresses
                    .Where(a => a.CustomerId == customerId)
                    .OrderByDescending(a => a.IsDefault)
                    .ToListAsync();
            }
            return View("Index", vm);
        }

        if (customerId == null) return Forbid();

        var paymentMethod = (vm.PaymentMethod ?? "COD").ToUpperInvariant();
        if (paymentMethod is not ("COD" or "BANK")) paymentMethod = "COD";

        using var tx = await _db.Database.BeginTransactionAsync();

        try
        {
            var inv = new SalesInvoice
            {
                CustomerId = customerId,
                CreatedAt = DateTime.Now,
                CreatedBy = uid,
                ReceiverName = vm.ReceiverName.Trim(),
                ShippingPhone = vm.Phone?.Trim(),
                ShippingAddress = vm.Address?.Trim(),
                Note = string.IsNullOrWhiteSpace(vm.Note) ? null : vm.Note.Trim(),
                Status = "Pending",
                PaymentMethod = paymentMethod,
                PaymentStatus = paymentMethod == "COD" ? "Unpaid" : "Pending",
                PaidAt = null,
                ShippingFee = vm.ShippingFee,
                Discount = vm.Discount
            };

            _db.SalesInvoices.Add(inv);
            await _db.SaveChangesAsync();

            inv.Code = $"DH{inv.Id:000000}";
            await _db.SaveChangesAsync();

            decimal total = 0;

            foreach (var line in lines)
            {
                var p = await _db.Products.FirstOrDefaultAsync(x => x.Id == line.Product.Id);
                if (p == null) { tx.Rollback(); return BadRequest("Sản phẩm không tồn tại."); }
                if (!p.IsActive) { tx.Rollback(); return BadRequest("Sản phẩm đã ngừng kinh doanh."); }
                if (p.Stock < line.Quantity) { tx.Rollback(); return BadRequest($"Không đủ tồn kho cho {p.Name}."); }

                p.Stock -= line.Quantity;

                var lineTotal = p.Price * line.Quantity;
                total += lineTotal;

                _db.SalesItems.Add(new SalesItem
                {
                    SalesInvoiceId = inv.Id,
                    ProductId = p.Id,
                    Quantity = line.Quantity,
                    UnitPrice = p.Price,
                    LineTotal = lineTotal
                });

                _db.StockLedgers.Add(new StockLedger
                {
                    ProductId = p.Id,
                    Type = "OUT",
                    Quantity = line.Quantity,
                    RefType = "SALE",
                    RefId = inv.Id,
                    OccurredAt = DateTime.Now
                });
            }

            inv.Total = total + vm.ShippingFee - vm.Discount;
            await _db.SaveChangesAsync();

            // Record promotion usage
            if (!string.IsNullOrWhiteSpace(vm.VoucherCode))
            {
                var promo = await _db.Promotions.FirstOrDefaultAsync(p => p.Code == vm.VoucherCode.Trim().ToUpper());
                if (promo != null && customerId.HasValue)
                {
                    promo.UsageCount++;
                    _db.PromotionUsages.Add(new PromotionUsage
                    {
                        PromotionId = promo.Id,
                        CustomerId = customerId.Value,
                        SalesInvoiceId = inv.Id,
                        UsedAt = DateTime.Now
                    });
                    await _db.SaveChangesAsync();
                }
            }

            await tx.CommitAsync();

            HttpContext.Session.Remove(CartKey);

            _db.Notifications.Add(new QuanLyBanHang.Models.Notification
            {
                CustomerId = customerId.Value,
                Title = $"Đặt hàng thành công",
                Message = $"Đơn hàng {inv.Code} đã được đặt và đang chờ xác nhận.",
                Url = $"/Shop/Details/{inv.Id}",
                CreatedAt = DateTime.Now
            });
            await _db.SaveChangesAsync();

            // Auto-save address to address book (only if not selecting a saved one)
            if (!vm.SelectedAddressId.HasValue && customerId.HasValue)
            {
                var receiverName = vm.ReceiverName?.Trim() ?? "";
                var phone = vm.Phone?.Trim() ?? "";
                var addr = vm.Address?.Trim() ?? "";

                var exists = await _db.CustomerAddresses.AnyAsync(a =>
                    a.CustomerId == customerId.Value &&
                    a.ReceiverName == receiverName &&
                    a.Phone == phone &&
                    a.Address == addr);

                if (!exists)
                {
                    var count = await _db.CustomerAddresses.CountAsync(a => a.CustomerId == customerId.Value);
                    _db.CustomerAddresses.Add(new QuanLyBanHang.Models.CustomerAddress
                    {
                        CustomerId = customerId.Value,
                        Label = "Đã giao",
                        ReceiverName = receiverName,
                        Phone = phone,
                        Address = addr,
                        IsDefault = count == 0
                    });
                    await _db.SaveChangesAsync();
                }
            }

            return RedirectToAction(nameof(Success), new { id = inv.Id });
        }
        catch
        {
            await tx.RollbackAsync();
            TempData["Error"] = "Có lỗi xảy ra khi xử lý đơn hàng. Vui lòng thử lại.";
            return View("Index", vm);
        }
    }

    [HttpGet]
    public async Task<IActionResult> Success(long id)
    {
        var inv = await GetMyInvoiceAsync(id);
        if (inv == null) return NotFound();

        return View(inv);
    }

    [HttpPost]
    public async Task<IActionResult> CheckVoucher(string code, decimal subtotal)
    {
        if (string.IsNullOrWhiteSpace(code))
            return Json(new { success = false, message = "Vui lòng nhập mã khuyến mãi." });

        var cart = GetCart();
        if (cart.Count == 0)
            return Json(new { success = false, message = "Giỏ hàng đang trống." });

        var promo = await _db.Promotions
            .Include(p => p.PromotionProducts)
            .FirstOrDefaultAsync(p => p.Code == code.Trim().ToUpper()
                && p.IsActive
                && p.StartDate <= DateTime.Now
                && p.EndDate >= DateTime.Now);

        if (promo == null)
            return Json(new { success = false, message = "Mã khuyến mãi không hợp lệ hoặc đã hết hạn." });

        if (promo.MinOrderValue.HasValue && subtotal < promo.MinOrderValue.Value)
            return Json(new { success = false, message = $"Đơn hàng tối thiểu {promo.MinOrderValue.Value:N0}₫ để áp dụng mã này." });

        if (promo.PromotionProducts.Count > 0)
        {
            var eligibleIds = promo.PromotionProducts.Select(pp => pp.ProductId).ToHashSet();
            if (!cart.Any(c => eligibleIds.Contains(c.ProductId)))
                return Json(new { success = false, message = "Giỏ hàng của bạn không có sản phẩm nào thuộc chương trình này." });
        }

        decimal discount = 0;
        if (promo.DiscountType == "Percentage")
            discount = subtotal * promo.DiscountValue / 100;
        else
            discount = promo.DiscountValue;

        if (discount > subtotal) discount = subtotal;

        return Json(new
        {
            success = true,
            message = $"Áp dụng mã <strong>{promo.Code}</strong> thành công!",
            discount = discount,
            discountDisplay = $"-{discount:N0}₫",
            discountLabel = promo.Name,
            promoName = promo.Name,
            promoCode = promo.Code
        });
    }

    [HttpPost]
    public IActionResult CalcShippingFee(string method, string? province)
    {
        var fee = CalcShipping(method, province);
        return Json(new { success = true, fee });
    }

    [HttpPost]
    public async Task<IActionResult> GetAddress(long id)
    {
        var uid = GetUserId();
        var customerId = await _db.Users.Where(u => u.Id == uid).Select(u => u.CustomerId).FirstOrDefaultAsync();
        if (customerId == null) return Json(new { success = false });

        var addr = await _db.CustomerAddresses
            .FirstOrDefaultAsync(a => a.Id == id && a.CustomerId == customerId);

        if (addr == null) return Json(new { success = false });

        return Json(new
        {
            success = true,
            receiverName = addr.ReceiverName,
            phone = addr.Phone,
            address = addr.Address
        });
    }

    [HttpPost]
    public async Task<IActionResult> AddAddress(string receiverName, string phone, string address, string? label)
    {
        var uid = GetUserId();
        var customerId = await _db.Users.Where(u => u.Id == uid).Select(u => u.CustomerId).FirstOrDefaultAsync();
        if (customerId == null) return Json(new { success = false, message = "Không tìm thấy khách hàng." });

        if (string.IsNullOrWhiteSpace(receiverName))
            return Json(new { success = false, message = "Vui lòng nhập họ tên." });
        if (receiverName.Trim().Length > 150)
            return Json(new { success = false, message = "Họ tên không quá 150 ký tự." });
        if (string.IsNullOrWhiteSpace(phone))
            return Json(new { success = false, message = "Vui lòng nhập số điện thoại." });
        if (!System.Text.RegularExpressions.Regex.IsMatch(phone.Trim(), @"^0[0-9]{9,10}$"))
            return Json(new { success = false, message = "Số điện thoại không hợp lệ (10-11 số, bắt đầu bằng 0)." });
        if (string.IsNullOrWhiteSpace(address))
            return Json(new { success = false, message = "Vui lòng nhập địa chỉ." });
        if (address.Trim().Length > 255)
            return Json(new { success = false, message = "Địa chỉ không quá 255 ký tự." });
        if (!string.IsNullOrWhiteSpace(label) && label.Trim().Length > 100)
            return Json(new { success = false, message = "Ghi chú không quá 100 ký tự." });

        var count = await _db.CustomerAddresses.CountAsync(a => a.CustomerId == customerId.Value);

        var addr = new QuanLyBanHang.Models.CustomerAddress
        {
            CustomerId = customerId.Value,
            ReceiverName = receiverName.Trim(),
            Phone = phone.Trim(),
            Address = address.Trim(),
            Label = string.IsNullOrWhiteSpace(label) ? null : label.Trim(),
            IsDefault = count == 0
        };

        _db.CustomerAddresses.Add(addr);
        await _db.SaveChangesAsync();

        return Json(new
        {
            success = true,
            id = addr.Id,
            receiverName = addr.ReceiverName,
            phone = addr.Phone,
            address = addr.Address
        });
    }

    private decimal CalcShipping(string method, string? province)
    {
        var isFar = province != null && new[] { "Hà Nội", "Hồ Chí Minh", "Đà Nẵng", "Hải Phòng", "Cần Thơ" }.Contains(province) == false;
        return method switch
        {
            "economy" => isFar ? 25000 : 15000,
            _ => isFar ? 35000 : 20000,
        };
    }

    private async Task<SalesInvoice?> GetMyInvoiceAsync(long id)
    {
        var uid = GetUserId();
        var customerId = await _db.Users.Where(u => u.Id == uid).Select(u => u.CustomerId).FirstOrDefaultAsync();
        if (customerId == null) return null;

        var inv = await _db.SalesInvoices
            .Include(x => x.Customer)
            .Include(x => x.Items).ThenInclude(i => i.Product)
            .FirstOrDefaultAsync(x => x.Id == id && x.CustomerId == customerId);

        return inv;
    }

    private List<CartItemVM> GetCart()
        => HttpContext.Session.GetObject<List<CartItemVM>>(CartKey) ?? new List<CartItemVM>();

    private async Task<List<CartLineVM>> BuildCartLinesAsync()
    {
        var cart = GetCart();
        if (cart.Count == 0) return new List<CartLineVM>();

        var ids = cart.Select(x => x.ProductId).Distinct().ToList();
        var products = await _db.Products.Include(p => p.Category).Where(p => ids.Contains(p.Id)).ToListAsync();

        var lines = (from c in cart
                     join p in products on c.ProductId equals p.Id
                     select new CartLineVM { Product = p, Quantity = c.Quantity }).ToList();

        return lines;
    }

    private long GetUserId()
    {
        var s = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
        return long.TryParse(s, out var id) ? id : 0;
    }
}
