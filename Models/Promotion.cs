using System.ComponentModel.DataAnnotations;

namespace QuanLyBanHang.Models;

public class Promotion
{
    public long Id { get; set; }

    [Required, StringLength(200)]
    public string Name { get; set; } = "";

    [Required, StringLength(50)]
    public string Code { get; set; } = "";

    [StringLength(500)]
    public string? Description { get; set; }

    [Required, StringLength(20)]
    public string DiscountType { get; set; } = "Percentage"; // Percentage | Fixed

    [Required, Range(0, double.MaxValue, ErrorMessage = "Giá trị giảm phải lớn hơn hoặc bằng 0.")]
    public decimal DiscountValue { get; set; }

    [Range(0, double.MaxValue)]
    public decimal? MinOrderValue { get; set; }

    [Required, DataType(DataType.Date)]
    public DateTime StartDate { get; set; }

    [Required, DataType(DataType.Date)]
    public DateTime EndDate { get; set; }

    public bool IsActive { get; set; } = true;

    [Range(0, int.MaxValue)]
    public int? MaxUsageCount { get; set; }

    [Range(0, int.MaxValue)]
    public int? MaxUsagePerCustomer { get; set; }

    public int UsageCount { get; set; }

    public DateTime CreatedAt { get; set; } = DateTime.Now;

    public long? CreatedBy { get; set; }

    public ICollection<PromotionProduct> PromotionProducts { get; set; } = new List<PromotionProduct>();
}
