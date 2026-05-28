namespace QuanLyBanHang.Models;

public class Notification
{
    public long Id { get; set; }

    public long CustomerId { get; set; }
    public Customer? Customer { get; set; }

    public string Title { get; set; } = "";

    public string? Message { get; set; }

    public bool IsRead { get; set; } = false;

    public string? Url { get; set; }

    public DateTime CreatedAt { get; set; } = DateTime.Now;
}
