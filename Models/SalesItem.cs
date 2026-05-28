using System.ComponentModel.DataAnnotations;

namespace QuanLyBanHang.Models;

public class SalesItem
{
    public long Id { get; set; }

    public long SalesInvoiceId { get; set; }
    public SalesInvoice? Invoice { get; set; }

    public long ProductId { get; set; }
    public Product? Product { get; set; }

    [Range(1, 999999)]
    public int Quantity { get; set; }

    [Range(0, 999999999)]
    public decimal UnitPrice { get; set; }

    [Range(0, 999999999)]
    public decimal LineTotal { get; set; }
}
