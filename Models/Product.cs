using System.ComponentModel.DataAnnotations;

namespace QuanLyBanHang.Models;

public class Product
{
    public long Id { get; set; }

    [Required(ErrorMessage = "SKU không được để trống.")]
    [StringLength(50, ErrorMessage = "SKU tối đa 50 ký tự.")]
    public string Sku { get; set; } = "";

    [Required(ErrorMessage = "Tên sản phẩm không được để trống.")]
    [StringLength(200, ErrorMessage = "Tên sản phẩm tối đa 200 ký tự.")]
    public string Name { get; set; } = "";

    [Required(ErrorMessage = "Vui lòng chọn danh mục.")]
    public long? CategoryId { get; set; }
    public Category? Category { get; set; }

    [Required(ErrorMessage = "Giá không được để trống.")]
    [Range(1000, 999999999, ErrorMessage = "Giá phải từ 1,000 đến 999,999,999.")]
    public decimal Price { get; set; }

    [Required(ErrorMessage = "Tồn kho không được để trống.")]
    [Range(0, 999999, ErrorMessage = "Tồn kho phải từ 0 đến 999,999.")]
    public int Stock { get; set; } = 0;

    public bool IsActive { get; set; } = true;

    public string? ImagePath { get; set; }

    [StringLength(5000, ErrorMessage = "Giới thiệu tối đa 5,000 ký tự.")]
    public string? Description { get; set; }

    [StringLength(100, ErrorMessage = "Thương hiệu tối đa 100 ký tự.")]
    public string? Brand { get; set; }

    [StringLength(5000, ErrorMessage = "Thông số kỹ thuật tối đa 5,000 ký tự.")]
    public string? Specifications { get; set; }

    [Range(0, 480, ErrorMessage = "Bảo hành từ 0 đến 480 tháng.")]
    public int WarrantyMonths { get; set; } = 0;
}
