using System.ComponentModel.DataAnnotations;

namespace QuanLyBanHang.Models;

public class PurchaseInvoice
{
    public long Id { get; set; }

    public long? SupplierId { get; set; }
    public Supplier? Supplier { get; set; }

    public DateTime CreatedAt { get; set; } = DateTime.Now;

    public long? CreatedBy { get; set; }

    [Range(0, 999999999)]
    public decimal Total { get; set; } = 0;

    public ICollection<PurchaseItem> Items { get; set; } = new List<PurchaseItem>();
}
