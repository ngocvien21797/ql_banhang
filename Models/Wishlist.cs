namespace QuanLyBanHang.Models;

public class Wishlist
{
    public long Id { get; set; }

    public long CustomerId { get; set; }
    public Customer? Customer { get; set; }

    public long ProductId { get; set; }
    public Product? Product { get; set; }

    public DateTime CreatedAt { get; set; } = DateTime.Now;
}
