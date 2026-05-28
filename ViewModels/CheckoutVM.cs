namespace QuanLyBanHang.ViewModels;

public class CheckoutVM
{
    public string ReceiverName { get; set; } = "";
    public string? Phone { get; set; }
    public string? Email { get; set; }

    public string? Province { get; set; }
    public string? District { get; set; }
    public string? Ward { get; set; }
    public string? Address { get; set; }

    public string ShippingMethod { get; set; } = "standard";

    public string PaymentMethod { get; set; } = "COD";

    public string? VoucherCode { get; set; }

    public string? Note { get; set; }

    public List<CartLineVM> Lines { get; set; } = new();
    public decimal Subtotal { get; set; }
    public decimal ShippingFee { get; set; }
    public decimal Discount { get; set; }
    public decimal Total { get; set; }

    public List<string> Provinces { get; set; } = new()
    {
        "Hồ Chí Minh", "Hà Nội", "Đà Nẵng", "Hải Phòng",
        "Cần Thơ", "An Giang", "Bà Rịa - Vũng Tàu", "Bắc Giang",
        "Bắc Kạn", "Bạc Liêu", "Bắc Ninh", "Bến Tre", "Bình Định",
        "Bình Dương", "Bình Phước", "Bình Thuận", "Cà Mau",
        "Cao Bằng", "Đắk Lắk", "Đắk Nông", "Điện Biên", "Đồng Nai",
        "Đồng Tháp", "Gia Lai", "Hà Giang", "Hà Nam", "Hà Tĩnh",
        "Hải Dương", "Hậu Giang", "Hòa Bình", "Hưng Yên", "Khánh Hòa",
        "Kiên Giang", "Kon Tum", "Lai Châu", "Lâm Đồng", "Lạng Sơn",
        "Lào Cai", "Long An", "Nam Định", "Nghệ An", "Ninh Bình",
        "Ninh Thuận", "Phú Thọ", "Phú Yên", "Quảng Bình", "Quảng Nam",
        "Quảng Ngãi", "Quảng Ninh", "Quảng Trị", "Sóc Trăng",
        "Sơn La", "Tây Ninh", "Thái Bình", "Thái Nguyên", "Thanh Hóa",
        "Thừa Thiên Huế", "Tiền Giang", "Trà Vinh", "Tuyên Quang",
        "Vĩnh Long", "Vĩnh Phúc", "Yên Bái"
    };
}

public class CartAjaxResult
{
    public bool Success { get; set; }
    public string? Message { get; set; }
    public int CartCount { get; set; }
    public decimal Subtotal { get; set; }
    public decimal ShippingFee { get; set; }
    public decimal Discount { get; set; }
    public decimal Total { get; set; }
    public Dictionary<long, int>? Stocks { get; set; }
}

public class VoucherCheckResult
{
    public bool Success { get; set; }
    public string? Message { get; set; }
    public decimal Discount { get; set; }
    public string? DiscountLabel { get; set; }
}
