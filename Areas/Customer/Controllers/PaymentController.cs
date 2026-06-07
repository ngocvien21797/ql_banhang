using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using QuanLyBanHang.Data;
using QuanLyBanHang.Models;
using System.Security.Claims;

namespace QuanLyBanHang.Areas.Customer.Controllers;

[Area("Customer")]
[Authorize(Roles = "2")]
public class PaymentController : Controller
{
    private readonly SalesDbContext _db;
    public PaymentController(SalesDbContext db) => _db = db;

    [HttpGet]
    public async Task<IActionResult> Pay(long id)
    {
        var inv = await GetMyInvoiceAsync(id);
        if (inv == null) return NotFound();

        if (inv.PaymentStatus == "Paid")
        {
            TempData["Ok"] = "Đơn này đã được thanh toán.";
            return RedirectToAction("Details", "Shop", new { id });
        }

        if (inv.PaymentMethod != "BANK" && inv.PaymentMethod != "WALLET")
        {
            TempData["Error"] = "Đơn này không cần thanh toán online.";
            return RedirectToAction("Details", "Shop", new { id });
        }

        // Build VietQR code with amount
        var addInfo = $"TT {inv.Code}";
        var accountName = "To Ngoc Vien";
        var amount = ((long)inv.Total).ToString();
        ViewBag.QR = $"https://api.vietqr.io/image/970422-0364921797-print.jpg?amount={amount}&addInfo={Uri.EscapeDataString(addInfo)}&accountName={Uri.EscapeDataString(accountName)}";
        ViewBag.QRContent = $"MB Bank - 0364921797 - TO NGOC VIEN - {amount}VND - {addInfo}";

        return View(inv);
    }

    [HttpPost]
    public async Task<IActionResult> Confirm(long id)
    {
        var inv = await GetMyInvoiceAsync(id);
        if (inv == null) return NotFound();

        if (inv.PaymentStatus == "Paid")
            return RedirectToAction("Details", "Shop", new { id });

        if (inv.PaymentMethod == "COD")
        {
            TempData["Error"] = "Đơn COD sẽ thanh toán khi nhận hàng.";
            return RedirectToAction(nameof(Pay), new { id });
        }

        if (inv.PaymentMethod == "BANK")
        {
            inv.PaymentStatus = "Paid";
            inv.PaidAt = DateTime.Now;
            inv.PaymentProvider = "MB_BANK";
            inv.PaymentRef = $"MB-{DateTime.Now:yyyyMMddHHmmss}-{inv.Id}";
            await _db.SaveChangesAsync();

            await CreateNotificationAsync(inv.CustomerId, $"Thanh toán {inv.Code}", $"Đơn hàng {inv.Code} đã thanh toán qua chuyển khoản.", $"/Shop/Details/{inv.Id}");

            TempData["Ok"] = "Xác nhận thanh toán chuyển khoản thành công!";
            return RedirectToAction("Details", "Shop", new { id });
        }

        if (inv.PaymentMethod == "WALLET")
        {
            var customer = await _db.Customers.FirstOrDefaultAsync(c => c.Id == inv.CustomerId);
            if (customer == null)
            {
                TempData["Error"] = "Không tìm thấy khách hàng.";
                return RedirectToAction(nameof(Pay), new { id });
            }

            if (customer.WalletBalance < inv.Total)
            {
                TempData["Error"] = $"Số dư ví không đủ. Hiện có {customer.WalletBalance:N0} đ.";
                return RedirectToAction(nameof(Pay), new { id });
            }

            customer.WalletBalance -= inv.Total;

            inv.PaymentStatus = "Paid";
            inv.PaidAt = DateTime.Now;
            inv.PaymentProvider = "WALLET";
            inv.PaymentRef = $"WALLET-{DateTime.Now:yyyyMMddHHmmss}-{inv.Id}";

            await _db.SaveChangesAsync();

            await CreateNotificationAsync(inv.CustomerId, $"Thanh toán {inv.Code}", $"Đơn hàng {inv.Code} đã thanh toán qua ví điện tử.", $"/Shop/Details/{inv.Id}");

            TempData["Ok"] = "Thanh toán ví điện tử thành công!";
            return RedirectToAction("Details", "Shop", new { id });
        }

        TempData["Error"] = "Phương thức thanh toán không hợp lệ.";
        return RedirectToAction(nameof(Pay), new { id });
    }

    private async Task CreateNotificationAsync(long? customerId, string title, string message, string url)
    {
        if (customerId.HasValue)
        {
            _db.Notifications.Add(new Notification
            {
                CustomerId = customerId.Value,
                Title = title,
                Message = message,
                Url = url,
                CreatedAt = DateTime.Now
            });
            await _db.SaveChangesAsync();
        }
    }

    private async Task<SalesInvoice?> GetMyInvoiceAsync(long id)
    {
        var uid = GetUserId();
        var customerId = await _db.Users.Where(u => u.Id == uid).Select(u => u.CustomerId).FirstOrDefaultAsync();
        if (customerId == null) return null;

        var inv = await _db.SalesInvoices
            .Include(x => x.Items).ThenInclude(i => i.Product)
            .Include(x => x.Customer)
            .FirstOrDefaultAsync(x => x.Id == id && x.CustomerId == customerId);

        return inv;
    }

    private long GetUserId()
    {
        var s = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
        return long.TryParse(s, out var id) ? id : 0;
    }
}
