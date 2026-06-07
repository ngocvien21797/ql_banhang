using System.ComponentModel.DataAnnotations;

namespace QuanLyBanHang.Models;

public class Banner
{
    public long Id { get; set; }

    [Required(ErrorMessage = "Tên banner không được để trống.")]
    [StringLength(200, ErrorMessage = "Tên banner tối đa 200 ký tự.")]
    public string Title { get; set; } = "";

    [StringLength(500)]
    public string? ImagePath { get; set; }

    public int SortOrder { get; set; } = 0;

    public bool IsActive { get; set; } = true;

    public DateTime CreatedAt { get; set; } = DateTime.Now;
}
