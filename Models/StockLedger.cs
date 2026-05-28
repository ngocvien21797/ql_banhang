using System.ComponentModel.DataAnnotations;

namespace QuanLyBanHang.Models;

public class StockLedger
{
    public long Id { get; set; }

    public DateTime OccurredAt { get; set; } = DateTime.Now;

    public long ProductId { get; set; }
    public Product? Product { get; set; }

    // IN / OUT
    [Required, StringLength(10)]
    public string Type { get; set; } = "IN";

    public int Quantity { get; set; }

    // PURCHASE / SALE
    [Required, StringLength(20)]
    public string RefType { get; set; } = "";

    public long RefId { get; set; }
}
