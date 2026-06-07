using System.ComponentModel.DataAnnotations;

namespace QuanLyBanHang.Models;

public class Article
{
    public long Id { get; set; }

    [Required(ErrorMessage = "Tiêu đề không được để trống.")]
    [StringLength(200, ErrorMessage = "Tiêu đề tối đa 200 ký tự.")]
    public string Title { get; set; } = "";

    [StringLength(200)]
    public string? Slug { get; set; }

    [StringLength(500, ErrorMessage = "Tóm tắt tối đa 500 ký tự.")]
    public string? Summary { get; set; }

    [Required(ErrorMessage = "Nội dung không được để trống.")]
    public string Content { get; set; } = "";

    [StringLength(500)]
    public string? ImagePath { get; set; }

    public bool IsActive { get; set; } = true;

    public DateTime CreatedAt { get; set; } = DateTime.Now;

    [StringLength(50)]
    public string? CreatedBy { get; set; }
}
