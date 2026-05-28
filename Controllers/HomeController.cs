using Microsoft.AspNetCore.Mvc;
using System.Security.Claims;

namespace QuanLyBanHang.Controllers;

public class HomeController : Controller
{
    public IActionResult Index()
    {
        // Ẩn danh: vào shop (Customer area)ty?.IsAuthenticated != true)
            return RedirectToAction("Index", "Shop", new { area = "Customer" });

        var role = User.FindFirst(ClaimTypes.Role)?.Value;

        // Admin -> Admin area
        if (role == "1")
            return RedirectToAction("Index", "Admin", new { area = "Admin" });

        // Customer -> Customer area
        return RedirectToAction("Index", "Shop", new { area = "Customer" });
    }
}
