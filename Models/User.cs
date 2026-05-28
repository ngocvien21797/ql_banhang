using System.ComponentModel.DataAnnotations;

namespace QuanLyBanHang.Models;

public class User
{
    public long Id { get; set; }

    [Required, StringLength(50)]
    public string Username { get; set; } = "";

    // Làm bài cuối kì cho nhanh: dùng plaintext.
    // Nếu muốn chuẩn hơn: đổi thành PasswordHash và dùng BCrypt.
    [Required, StringLength(100)]
    public string Password { get; set; } = "";

    // 2 role:
    // 1 = Admin
    // 2 = Khách hàng
    public int Role { get; set; } = 2;

    // Khách hàng sẽ gắn với CustomerId để xem đơn hàng của mình
    public long? CustomerId { get; set; }
    public Customer? Customer { get; set; }
}
