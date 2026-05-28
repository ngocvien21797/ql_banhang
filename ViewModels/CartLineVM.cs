using QuanLyBanHang.Models;

namespace QuanLyBanHang.ViewModels;

public class CartLineVM
{
    public Product Product { get; set; } = null!;
    public int Quantity { get; set; }
    public decimal LineTotal => Product.Price * Quantity;
}
