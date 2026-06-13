using Microsoft.AspNetCore.Mvc;

namespace QuanLyBanHang.Areas.Customer.Controllers;

[Area("Customer")]
public class ChatbotController : Controller
{
    private static readonly List<(string[] Keywords, string Answer)> _faq = new()
    {
        (new[] { "đặt hàng", "cách đặt", "hướng dẫn đặt", "mua hàng", "đặt mua" },
            "Bạn chọn sản phẩm → thêm vào giỏ → bấm \"Đặt hàng\" → điền thông tin → chọn phương thức thanh toán → xác nhận. Đơn hàng sẽ được xử lý trong vòng 24h."),
        (new[] { "vận chuyển", "giao hàng", "ship", "phí ship", "free ship" },
            "🚚 Giao tiêu chuẩn: 30.000₫ (3-5 ngày).\n🚚 Giao tiết kiệm: 15.000₫ (5-7 ngày).\n🎉 Miễn phí ship cho đơn từ 1.000.000₫ (áp dụng mã FREESHIP)."),
        (new[] { "đổi trả", "bảo hành", "đổi", "trả hàng", "hoàn tiền" },
            "🔄 Đổi trả trong 30 ngày nếu lỗi nhà sản xuất. Sản phẩm phải còn nguyên tem, hộp.\n🔧 Bảo hành chính hãng 12-24 tháng tuỳ sản phẩm."),
        (new[] { "thanh toán", "chuyển khoản", "cod", "trả tiền", "phương thức thanh toán" },
            "Chúng tôi hỗ trợ:\n• COD (Thanh toán khi nhận)\n• Chuyển khoản ngân hàng.\n📞 0364 921 797\n📧 support@minimart.vn"),
        (new[] { "kiểm tra đơn", "tra cứu đơn", "theo dõi đơn", "trạng thái đơn", "check đơn" },
            "Bạn đăng nhập → vào mục \"Đơn hàng\" để theo dõi trạng thái. Hoặc gọi hotline 0364 921 797 để được hỗ trợ nhanh."),
        (new[] { "bảo mật", "thông tin cá nhân", "riêng tư", "privacy" },
            "Chúng tôi cam kết bảo mật toàn bộ thông tin cá nhân của khách hàng. Thông tin chỉ được sử dụng nội bộ và không chia sẻ cho bên thứ ba."),
    };

    [HttpPost]
    public IActionResult Ask([FromBody] ChatRequest request)
    {
        try
        {
            if (string.IsNullOrWhiteSpace(request.Message))
                return Json(new { reply = "Vui lòng nhập câu hỏi." });

            var message = request.Message.ToLower().Trim();

            foreach (var (keywords, answer) in _faq)
            {
                if (keywords.Any(k => message.Contains(k)))
                    return Json(new { reply = answer });
            }

            return Json(new
            {
                reply = "Cảm ơn bạn đã liên hệ! Để được hỗ trợ nhanh nhất, gọi 📞 0364 921 797 hoặc gửi email 📧 support@minimart.vn."
            });
        }
        catch (System.Exception ex)
        {
            return Json(new { reply = "Lỗi hệ thống: " + ex.Message });
        }
    }
}

public class ChatRequest
{
    public string Message { get; set; } = "";
}
