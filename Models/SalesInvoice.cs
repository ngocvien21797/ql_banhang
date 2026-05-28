using System.ComponentModel.DataAnnotations;

namespace QuanLyBanHang.Models;

public class SalesInvoice
{
    public long Id { get; set; }

    // Mã đơn hàng hiển thị (VD: DH000001)
    [StringLength(20)]
    public string Code { get; set; } = "";

    public long? CustomerId { get; set; }
    public Customer? Customer { get; set; }

    public DateTime CreatedAt { get; set; } = DateTime.Now;

    public long? CreatedBy { get; set; } // user id

    // Trạng thái đơn: Pending/Confirmed/Shipped/Completed/Cancelled
    [StringLength(20)]
    public string Status { get; set; } = "Pending";

    // Thanh toán: COD/BANK/WALLET (demo)
    [StringLength(10)]
    public string PaymentMethod { get; set; } = "COD";

    // Trạng thái thanh toán: Unpaid/Pending/Paid
    [StringLength(10)]
    public string PaymentStatus { get; set; } = "Unpaid";

    public DateTime? PaidAt { get; set; }

    // Demo: cổng/nhà cung cấp thanh toán + mã giao dịch giả
    [StringLength(30)]
    public string? PaymentProvider { get; set; }

    [StringLength(50)]
    public string? PaymentRef { get; set; }


    // Thông tin nhận hàng
    [StringLength(150)]
    public string ReceiverName { get; set; } = "";

    [StringLength(30)]
    public string? ShippingPhone { get; set; }

    [StringLength(255)]
    public string? ShippingAddress { get; set; }

    [StringLength(500)]
    public string? Note { get; set; }

    [Range(0, 999999999)]
    public decimal Total { get; set; } = 0;

    public static string DisplayStatus(string s) => s switch
    {
        "Pending" => "Chờ xác nhận",
        "Confirmed" => "Đã xác nhận",
        "Shipped" => "Đang giao",
        "Completed" => "Hoàn thành",
        "Cancelled" => "Đã hủy",
        _ => s
    };

    public static string DisplayPaymentStatus(string s) => s switch
    {
        "Unpaid" => "Chưa TT",
        "Pending" => "Chờ TT",
        "Paid" => "Đã TT",
        _ => s
    };

    public static string DisplayPaymentMethod(string s) => s.ToUpperInvariant() switch
    {
        "COD" => "COD",
        "BANK" => "Chuyển khoản",
        "WALLET" => "Ví điện tử",
        _ => s
    };

    public decimal ShippingFee { get; set; }
    public decimal Discount { get; set; }

    public ICollection<SalesItem> Items { get; set; } = new List<SalesItem>();
}
