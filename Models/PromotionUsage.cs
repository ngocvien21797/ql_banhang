namespace QuanLyBanHang.Models;

public class PromotionUsage
{
    public long Id { get; set; }

    public long PromotionId { get; set; }
    public Promotion? Promotion { get; set; }

    public long CustomerId { get; set; }
    public long? SalesInvoiceId { get; set; }

    public DateTime UsedAt { get; set; } = DateTime.Now;
}
