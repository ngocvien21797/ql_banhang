# Quản Lý Bán Hàng (ASP.NET Core MVC + MySQL)

## 1) Yêu cầu
- .NET SDK 8.x
- MySQL 8.x

## 2) Cấu hình DB
Sửa `appsettings.json` -> `ConnectionStrings:DefaultConnection`.

Ví dụ:
`server=localhost;port=3306;database=ql_banhang;user=;password=;`

## 3) Chạy dự án
- Restore NuGet packages
- Run project

Lần chạy đầu tiên app sẽ tự tạo DB + bảng bằng `EnsureCreated()` và seed dữ liệu mẫu.

> Lưu ý: `EnsureCreated()` **không tự update schema**. Nếu bạn đổi Model (ví dụ thêm trường thanh toán),
hãy **DROP DATABASE** rồi chạy lại để tạo mới.

## 4) Tài khoản mẫu
- Admin: `admin / 123`
- Khách hàng: `khach / 123`

## 5) Chức năng
### Shop (public / khách hàng)
- Xem sản phẩm, lọc theo danh mục, tìm kiếm
- Giỏ hàng (Session): thêm/xóa/cập nhật số lượng
- Checkout (yêu cầu đăng nhập role khách hàng)
- Thanh toán **demo**: COD / chuyển khoản (xác nhận) / thẻ (đã thanh toán)
- Đơn hàng của tôi: xem danh sách + chi tiết, trạng thái & thanh toán

### Admin
- Dashboard tổng quan: đơn hôm nay, doanh thu, cảnh báo sắp hết hàng
- CRUD: Danh mục, Sản phẩm, Nhà cung cấp, Khách hàng
- Nhập hàng (phiếu nhập) -> cộng tồn + nhật ký kho
- Đơn hàng:
  - Xem chi tiết, cập nhật trạng thái (Pending/Confirmed/Shipped/Completed/Cancelled)
  - Đánh dấu đã thanh toán
  - Xóa đơn (hoàn tồn)
- Tồn kho + nhật ký kho
- Báo cáo doanh thu & top sản phẩm

## 6) Gợi ý nâng điểm
- Đổi `Password` -> `PasswordHash` và dùng BCrypt
- Thêm upload ảnh sản phẩm
- Thêm phân trang, export excel/pdf
- Tích hợp cổng thanh toán thật (VNPAY/MoMo) nếu yêu cầu
