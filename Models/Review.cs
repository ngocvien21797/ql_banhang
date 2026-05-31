namespace QuanLyBanHang.Models;

public class Review
{
    public long Id { get; set; }

    public long ProductId { get; set; }
    public Product? Product { get; set; }

    public long CustomerId { get; set; }
    public Customer? Customer { get; set; }

    public int Rating { get; set; }

    public string? Content { get; set; }

    public string? ImagePath { get; set; }

    public DateTime CreatedAt { get; set; } = DateTime.Now;
}
