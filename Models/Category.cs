using System.ComponentModel.DataAnnotations;

namespace QuanLyBanHang.Models;

public class Category
{
    public long Id { get; set; }

    [Required, StringLength(100)]
    public string Name { get; set; } = "";

    public ICollection<Product> Products { get; set; } = new List<Product>();
}
