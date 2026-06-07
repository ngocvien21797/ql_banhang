using System.ComponentModel.DataAnnotations;

namespace QuanLyBanHang.Models;

public class Customer
{
    public long Id { get; set; }

    [Required, StringLength(150)]
    public string Name { get; set; } = "";

    [StringLength(30)]
    public string? Phone { get; set; }

    [StringLength(255)]
    public string? Address { get; set; }

    [StringLength(100)]
    [EmailAddress(ErrorMessage = "Email không hợp lệ.")]
    public string? Email { get; set; }

    [StringLength(500)]
    public string? AvatarPath { get; set; }

    public DateTime? DateOfBirth { get; set; }

    [StringLength(10)]
    public string? Gender { get; set; }

    public decimal WalletBalance { get; set; } = 50000000;

    public ICollection<CustomerAddress> Addresses { get; set; } = new List<CustomerAddress>();
}
