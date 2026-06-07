using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Authorization;
using Microsoft.EntityFrameworkCore;
using System.Security.Claims;
using QuanLyBanHang.Data;
using QuanLyBanHang.Models;

namespace QuanLyBanHang.Controllers;

public class AccountController : Controller
{
    private readonly SalesDbContext _db;
    public AccountController(SalesDbContext db) => _db = db;

    [HttpGet]
    [AllowAnonymous]
    public IActionResult Login(string? returnUrl = null)
    {
        if (User.Identity?.IsAuthenticated == true)
        {
            var role = User.FindFirst(ClaimTypes.Role)?.Value;
            return role == "1"
                ? RedirectToAction("Index", "Admin", new { area = "Admin" })
                : RedirectToAction("Index", "Shop", new { area = "Customer" });
        }

        ViewBag.ReturnUrl = string.IsNullOrWhiteSpace(returnUrl)
            ? Url.Action("Index", "Shop", new { area = "Customer" })
            : returnUrl;

        return View();
    }

    [HttpPost]
    [AllowAnonymous]
    public async Task<IActionResult> Login(string username, string password, string? returnUrl = null)
    {
        username = (username ?? "").Trim();

        var user = await _db.Users.FirstOrDefaultAsync(u => u.Username == username && u.Password == password);
        if (user == null)
        {
            ViewBag.Error = "Sai tài khoản hoặc mật khẩu!";
            ViewBag.ReturnUrl = string.IsNullOrWhiteSpace(returnUrl)
                ? Url.Action("Index", "Shop", new { area = "Customer" })
                : returnUrl;
            return View();
        }

        var claims = new List<Claim>
        {
            new(ClaimTypes.Name, user.Username),
            new(ClaimTypes.Role, user.Role.ToString()),
            new(ClaimTypes.NameIdentifier, user.Id.ToString())
        };

        var identity = new ClaimsIdentity(claims, CookieAuthenticationDefaults.AuthenticationScheme);
        await HttpContext.SignInAsync(
            CookieAuthenticationDefaults.AuthenticationScheme,
            new ClaimsPrincipal(identity));

        // Admin luôn vào thẳng trang quản trị.
        if (user.Role == 1)
            return RedirectToAction("Index", "Admin", new { area = "Admin" });

        // Khách hàng: ưu tiên quay lại trang đang thao tác (VD: /Checkout).
        if (!string.IsNullOrWhiteSpace(returnUrl) && Url.IsLocalUrl(returnUrl))
            return LocalRedirect(returnUrl);

        return RedirectToAction("Index", "Shop", new { area = "Customer" });
    }

    [HttpGet]
    [AllowAnonymous]
    public IActionResult Register(string? returnUrl = null)
    {
        if (User.Identity?.IsAuthenticated == true)
            return RedirectToAction("Index", "Shop", new { area = "Customer" });

        ViewBag.ReturnUrl = string.IsNullOrWhiteSpace(returnUrl)
            ? Url.Action("Index", "Shop", new { area = "Customer" })
            : returnUrl;

        return View();
    }

    // Đăng ký đơn giản cho KH (Role = 2)
    [HttpPost]
    [AllowAnonymous]
    public async Task<IActionResult> Register(
        string username,
        string password,
        string confirmPassword,
        string name,
        string? phone,
        string? returnUrl = null)
    {
        if (string.IsNullOrWhiteSpace(username) ||
            string.IsNullOrWhiteSpace(password) ||
            string.IsNullOrWhiteSpace(name))
        {
            ViewBag.Error = "Vui lòng nhập đầy đủ thông tin bắt buộc.";
            ViewBag.ReturnUrl = string.IsNullOrWhiteSpace(returnUrl)
                ? Url.Action("Index", "Shop", new { area = "Customer" })
                : returnUrl;
            return View();
        }

        username = username.Trim();

        if (password != confirmPassword)
        {
            ViewBag.Error = "Mật khẩu nhập lại không khớp.";
            ViewBag.ReturnUrl = string.IsNullOrWhiteSpace(returnUrl)
                ? Url.Action("Index", "Shop", new { area = "Customer" })
                : returnUrl;
            return View();
        }

        var exists = await _db.Users.AnyAsync(u => u.Username == username);
        if (exists)
        {
            ViewBag.Error = "Email đã được đăng ký.";
            ViewBag.ReturnUrl = string.IsNullOrWhiteSpace(returnUrl)
                ? Url.Action("Index", "Shop", new { area = "Customer" })
                : returnUrl;
            return View();
        }

        // Tạo Customer
        var customer = new Customer
        {
            Name = name.Trim(),
            Phone = string.IsNullOrWhiteSpace(phone) ? null : phone.Trim()
        };
        _db.Customers.Add(customer);
        await _db.SaveChangesAsync();

        // Tạo User (Role 2 = Khách hàng)
        var user = new User
        {
            Username = username,
            Password = password,
            Role = 2,
            CustomerId = customer.Id
        };
        _db.Users.Add(user);
        await _db.SaveChangesAsync();

        // Auto login
        var claims = new List<Claim>
        {
            new(ClaimTypes.Name, user.Username),
            new(ClaimTypes.Role, user.Role.ToString()),
            new(ClaimTypes.NameIdentifier, user.Id.ToString())
        };
        var identity = new ClaimsIdentity(claims, CookieAuthenticationDefaults.AuthenticationScheme);
        await HttpContext.SignInAsync(CookieAuthenticationDefaults.AuthenticationScheme, new ClaimsPrincipal(identity));

        if (!string.IsNullOrWhiteSpace(returnUrl) && Url.IsLocalUrl(returnUrl))
            return LocalRedirect(returnUrl);

        return RedirectToAction("Index", "Shop", new { area = "Customer" });
    }

    public async Task<IActionResult> Logout()
    {
        await HttpContext.SignOutAsync(CookieAuthenticationDefaults.AuthenticationScheme);
        return RedirectToAction("Index", "Shop", new { area = "Customer" });
    }

    [AllowAnonymous]
    public IActionResult AccessDenied(string? returnUrl = null)
    {
        ViewBag.ReturnUrl = returnUrl;
        return View();
    }
}
