using System.ComponentModel.DataAnnotations;

namespace QuanLyBanHang.Models;

public class Product
{
    public long Id { get; set; }

    [Required, StringLength(50)]
    public string Sku { get; set; } = "";

    [Required, StringLength(200)]
    public string Name { get; set; } = "";

    public long? CategoryId { get; set; }
    public Category? Category { get; set; }

    [Range(0, 999999999)]
    public decimal Price { get; set; }

    public int Stock { get; set; } = 0;
    public bool IsActive { get; set; } = true;

    public string? ImagePath { get; set; }

    [StringLength(5000)]
    public string? Description { get; set; }

    [StringLength(100)]
    public string? Brand { get; set; }

    [StringLength(5000)]
    public string? Specifications { get; set; }

    public int WarrantyMonths { get; set; } = 0;
}
