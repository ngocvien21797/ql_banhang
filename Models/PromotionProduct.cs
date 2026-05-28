namespace QuanLyBanHang.Models;

public class PromotionProduct
{
    public long Id { get; set; }

    public long PromotionId { get; set; }
    public Promotion Promotion { get; set; } = null!;

    public long ProductId { get; set; }
    public Product Product { get; set; } = null!;
}
