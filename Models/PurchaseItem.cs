using System.ComponentModel.DataAnnotations;

namespace QuanLyBanHang.Models;

public class PurchaseItem
{
    public long Id { get; set; }

    public long PurchaseInvoiceId { get; set; }
    public PurchaseInvoice? Invoice { get; set; }

    public long ProductId { get; set; }
    public Product? Product { get; set; }

    [Range(1, 999999)]
    public int Quantity { get; set; }

    [Range(0, 999999999)]
    public decimal UnitCost { get; set; }

    [Range(0, 999999999)]
    public decimal LineTotal { get; set; }
}
