using System.ComponentModel.DataAnnotations;

namespace QuanLyBanHang.Models;

public class CustomerAddress
{
    public long Id { get; set; }

    public long CustomerId { get; set; }
    public Customer? Customer { get; set; }

    [StringLength(100, ErrorMessage = "Ghi chú không quá 100 ký tự.")]
    public string Label { get; set; } = "";

    [Required(ErrorMessage = "Vui lòng nhập họ tên.")]
    [StringLength(150, ErrorMessage = "Họ tên không quá 150 ký tự.")]
    public string ReceiverName { get; set; } = "";

    [Required(ErrorMessage = "Vui lòng nhập số điện thoại.")]
    [StringLength(30, ErrorMessage = "Số điện thoại không quá 30 ký tự.")]
    [RegularExpression(@"^0[0-9]{9,10}$", ErrorMessage = "Số điện thoại không hợp lệ (10-11 số, bắt đầu bằng 0).")]
    public string Phone { get; set; } = "";

    [StringLength(100, ErrorMessage = "Tỉnh/Thành không quá 100 ký tự.")]
    public string? Province { get; set; }

    [StringLength(100, ErrorMessage = "Quận/Huyện không quá 100 ký tự.")]
    public string? District { get; set; }

    [StringLength(100, ErrorMessage = "Phường/Xã không quá 100 ký tự.")]
    public string? Ward { get; set; }

    [Required(ErrorMessage = "Vui lòng nhập địa chỉ.")]
    [StringLength(255, ErrorMessage = "Địa chỉ không quá 255 ký tự.")]
    public string Address { get; set; } = "";

    public bool IsDefault { get; set; }
}
