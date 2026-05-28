using System.ComponentModel.DataAnnotations;

namespace QuanLyBanHang.Models;

public class Customer
{
    public long Id { get; set; }

    [Required, StringLength(150)]
    public string Name { get; set; } = "";

    [StringLength(30)]
    public string? Phone { get; set; }

    [StringLength(255)]
    public string? Address { get; set; }

    // Số dư ví ngân hàng (demo) - dùng để mô phỏng thanh toán.
    public decimal WalletBalance { get; set; } = 50000000;
}
