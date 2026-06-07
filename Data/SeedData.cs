using QuanLyBanHang.Models;

namespace QuanLyBanHang.Data;

public static class SeedData
{
    public static void Seed(SalesDbContext db)
    {
        // ==================== USERS ====================
        if (!db.Users.Any(x => x.Username == "admin"))
            db.Users.Add(new User { Username = "admin", Password = "123", Role = 1 });

        // ==================== CATEGORIES ====================
        if (!db.Categories.Any())
        {
            db.Categories.AddRange(
                new Category { Name = "Điện thoại" },
                new Category { Name = "Laptop" },
                new Category { Name = "Phụ kiện" },
                new Category { Name = "Tai nghe" },
                new Category { Name = "Sạc & Cáp" },
                new Category { Name = "Bàn phím & Chuột" }
            );
            db.SaveChanges();
        }

        // ==================== CUSTOMERS ====================
        if (!db.Customers.Any())
        {
            db.Customers.AddRange(
                new Customer { Name = "Nguyễn Văn An", Phone = "0901111111", Address = "Quận 1, TP.HCM", WalletBalance = 50000000 },
                new Customer { Name = "Trần Thị Bình", Phone = "0902222222", Address = "Quận 2, TP.HCM", WalletBalance = 20000000 },
                new Customer { Name = "Lê Văn Cường", Phone = "0903333333", Address = "Quận 3, TP.HCM", WalletBalance = 100000000 },
                new Customer { Name = "Phạm Thị Dung", Phone = "0904444444", Address = "Hà Nội", WalletBalance = 30000000 },
                new Customer { Name = "Hoàng Văn Em", Phone = "0905555555", Address = "Đà Nẵng", WalletBalance = 15000000 },
                new Customer { Name = "Võ Thị Phương", Phone = "0906666666", Address = "Cần Thơ", WalletBalance = 25000000 },
                new Customer { Name = "Đặng Văn Giàu", Phone = "0907777777", Address = "Bình Dương", WalletBalance = 80000000 },
                new Customer { Name = "Bùi Thị Hạnh", Phone = "0908888888", Address = "Vũng Tàu", WalletBalance = 12000000 },
                new Customer { Name = "Khách hàng demo", Phone = "0900000000", Address = "TP.HCM", WalletBalance = 50000000 }
            );
            db.SaveChanges();
        }

        // ==================== PRODUCTS ====================
        if (!db.Products.Any())
        {
            var phone = db.Categories.First(c => c.Name == "Điện thoại");
            var laptop = db.Categories.First(c => c.Name == "Laptop");
            var pk = db.Categories.First(c => c.Name == "Phụ kiện");
            var tainghe = db.Categories.First(c => c.Name == "Tai nghe");
            var saccap = db.Categories.First(c => c.Name == "Sạc & Cáp");
            var banphim = db.Categories.First(c => c.Name == "Bàn phím & Chuột");

            var phoneDesc = "Sản phẩm chính hãng, mới 100%, nguyên seal. Hỗ trợ đổi trả trong 30 ngày.";
            var laptopDesc = "Laptop chính hãng, cấu hình mạnh mẽ, phù hợp cho công việc và giải trí.";
            var pkDesc = "Phụ kiện chính hãng, chất lượng cao, bền bỉ.";
            var tnDesc = "Tai nghe chính hãng, âm thanh sống động, đeo thoải mái.";
            var scDesc = "Sạc & cáp chính hãng, hỗ trợ sạc nhanh, an toàn cho thiết bị.";
            var bpDesc = "Bàn phím & chuột chính hãng, độ nhạy cao, bền bỉ.";

            db.Products.AddRange(
                new Product { Sku = "DT001", Name = "iPhone 15 Pro Max 256GB", CategoryId = phone.Id, Price = 34990000, Stock = 20, IsActive = true, ImagePath = "/uploads/products/dt001.jpg", Description = phoneDesc, Brand = "Apple", WarrantyMonths = 12, Specifications = "[{\"label\":\"Màn hình\",\"value\":\"6.7 inch OLED 120Hz\"},{\"label\":\"Chip\",\"value\":\"A17 Pro\"},{\"label\":\"RAM\",\"value\":\"8GB\"},{\"label\":\"Bộ nhớ\",\"value\":\"256GB\"},{\"label\":\"Pin\",\"value\":\"4422mAh\"},{\"label\":\"Camera\",\"value\":\"48MP + 12MP + 12MP\"}]" },
                new Product { Sku = "DT002", Name = "Samsung Galaxy S24 Ultra", CategoryId = phone.Id, Price = 29990000, Stock = 15, IsActive = true, ImagePath = "/uploads/products/dt002.png", Description = phoneDesc, Brand = "Samsung", WarrantyMonths = 12, Specifications = "[{\"label\":\"Màn hình\",\"value\":\"6.8 inch Dynamic AMOLED 120Hz\"},{\"label\":\"Chip\",\"value\":\"Snapdragon 8 Gen 3\"},{\"label\":\"RAM\",\"value\":\"12GB\"},{\"label\":\"Bộ nhớ\",\"value\":\"256GB\"},{\"label\":\"Pin\",\"value\":\"5000mAh\"},{\"label\":\"Camera\",\"value\":\"200MP + 12MP + 50MP + 10MP\"}]" },
                new Product { Sku = "DT003", Name = "Xiaomi 14 Pro", CategoryId = phone.Id, Price = 15990000, Stock = 35, IsActive = true, ImagePath = "/uploads/products/dt003.jpg", Description = phoneDesc, Brand = "Xiaomi", WarrantyMonths = 12, Specifications = "[{\"label\":\"Màn hình\",\"value\":\"6.73 inch LTPO AMOLED 120Hz\"},{\"label\":\"Chip\",\"value\":\"Snapdragon 8 Gen 3\"},{\"label\":\"RAM\",\"value\":\"12GB\"},{\"label\":\"Bộ nhớ\",\"value\":\"256GB\"},{\"label\":\"Pin\",\"value\":\"4880mAh\"},{\"label\":\"Camera\",\"value\":\"50MP + 50MP + 50MP\"}]" },
                new Product { Sku = "DT004", Name = "OPPO Find N3 Flip", CategoryId = phone.Id, Price = 19990000, Stock = 12, IsActive = true, ImagePath = "/uploads/products/dt004.jpg", Description = phoneDesc, Brand = "OPPO", WarrantyMonths = 12, Specifications = "[{\"label\":\"Màn hình\",\"value\":\"6.8 inch AMOLED 120Hz\"},{\"label\":\"Chip\",\"value\":\"MediaTek Dimensity 9200\"},{\"label\":\"RAM\",\"value\":\"12GB\"},{\"label\":\"Bộ nhớ\",\"value\":\"256GB\"},{\"label\":\"Pin\",\"value\":\"4300mAh\"},{\"label\":\"Camera\",\"value\":\"50MP + 48MP + 32MP\"}]" },
                new Product { Sku = "DT005", Name = "iPhone 15 128GB", CategoryId = phone.Id, Price = 22990000, Stock = 0, IsActive = true, ImagePath = "/uploads/products/dt005.jpg", Description = phoneDesc, Brand = "Apple", WarrantyMonths = 12, Specifications = "[{\"label\":\"Màn hình\",\"value\":\"6.1 inch OLED 60Hz\"},{\"label\":\"Chip\",\"value\":\"A16 Bionic\"},{\"label\":\"RAM\",\"value\":\"6GB\"},{\"label\":\"Bộ nhớ\",\"value\":\"128GB\"},{\"label\":\"Pin\",\"value\":\"3349mAh\"},{\"label\":\"Camera\",\"value\":\"48MP + 12MP\"}]" },
                new Product { Sku = "LT001", Name = "MacBook Air M3 15\"", CategoryId = laptop.Id, Price = 32990000, Stock = 10, IsActive = true, ImagePath = "/uploads/products/lt001.jpg", Description = laptopDesc, Brand = "Apple", WarrantyMonths = 12, Specifications = "[{\"label\":\"Màn hình\",\"value\":\"15.3 inch Liquid Retina\"},{\"label\":\"Chip\",\"value\":\"Apple M3\"},{\"label\":\"RAM\",\"value\":\"8GB Unified\"},{\"label\":\"Bộ nhớ\",\"value\":\"256GB SSD\"},{\"label\":\"Pin\",\"value\":\"18 giờ\"},{\"label\":\"Trọng lượng\",\"value\":\"1.51kg\"}]" },
                new Product { Sku = "LT002", Name = "MacBook Pro M3 Pro 14\"", CategoryId = laptop.Id, Price = 45990000, Stock = 5, IsActive = true, ImagePath = "/uploads/products/lt002.jpg", Description = laptopDesc, Brand = "Apple", WarrantyMonths = 12, Specifications = "[{\"label\":\"Màn hình\",\"value\":\"14.2 inch Liquid Retina XDR\"},{\"label\":\"Chip\",\"value\":\"Apple M3 Pro\"},{\"label\":\"RAM\",\"value\":\"18GB Unified\"},{\"label\":\"Bộ nhớ\",\"value\":\"512GB SSD\"},{\"label\":\"Pin\",\"value\":\"17 giờ\"},{\"label\":\"Trọng lượng\",\"value\":\"1.63kg\"}]" },
                new Product { Sku = "LT003", Name = "Dell XPS 15 Intel i9", CategoryId = laptop.Id, Price = 38990000, Stock = 8, IsActive = true, ImagePath = "/uploads/products/lt003.jpg", Description = laptopDesc, Brand = "Dell", WarrantyMonths = 24, Specifications = "[{\"label\":\"Màn hình\",\"value\":\"15.6 inch FHD+ OLED\"},{\"label\":\"Chip\",\"value\":\"Intel Core i9-13900H\"},{\"label\":\"RAM\",\"value\":\"32GB DDR5\"},{\"label\":\"Bộ nhớ\",\"value\":\"1TB SSD\"},{\"label\":\"Pin\",\"value\":\"86Wh\"},{\"label\":\"Trọng lượng\",\"value\":\"1.86kg\"}]" },
                new Product { Sku = "LT004", Name = "Lenovo ThinkPad X1 Carbon", CategoryId = laptop.Id, Price = 35990000, Stock = 7, IsActive = true, ImagePath = "/uploads/products/lt004.jpg", Description = laptopDesc, Brand = "Lenovo", WarrantyMonths = 24, Specifications = "[{\"label\":\"Màn hình\",\"value\":\"14 inch WUXGA IPS\"},{\"label\":\"Chip\",\"value\":\"Intel Core i7-1365U\"},{\"label\":\"RAM\",\"value\":\"16GB DDR5\"},{\"label\":\"Bộ nhớ\",\"value\":\"512GB SSD\"},{\"label\":\"Pin\",\"value\":\"57Wh\"},{\"label\":\"Trọng lượng\",\"value\":\"1.12kg\"}]" },
                new Product { Sku = "LT005", Name = "ASUS ROG Zephyrus G14", CategoryId = laptop.Id, Price = 28990000, Stock = 3, IsActive = true, ImagePath = "/uploads/products/lt005.jpg", Description = laptopDesc, Brand = "ASUS", WarrantyMonths = 24, Specifications = "[{\"label\":\"Màn hình\",\"value\":\"14 inch QHD 165Hz\"},{\"label\":\"Chip\",\"value\":\"AMD Ryzen 9 7940HS\"},{\"label\":\"RAM\",\"value\":\"16GB DDR5\"},{\"label\":\"Bộ nhớ\",\"value\":\"1TB SSD\"},{\"label\":\"GPU\",\"value\":\"NVIDIA RTX 4060\"},{\"label\":\"Trọng lượng\",\"value\":\"1.72kg\"}]" },
                new Product { Sku = "PK001", Name = "Ốp lưng iPhone 15 silicone", CategoryId = pk.Id, Price = 199000, Stock = 200, IsActive = true, ImagePath = "/uploads/products/pk001.jpg", Description = pkDesc, Brand = "MiniMart", WarrantyMonths = 3 },
                new Product { Sku = "PK002", Name = "Miếng dán cường lực iPhone", CategoryId = pk.Id, Price = 99000, Stock = 300, IsActive = true, ImagePath = "/uploads/products/pk002.jpg", Description = pkDesc, Brand = "MiniMart", WarrantyMonths = 3 },
                new Product { Sku = "PK003", Name = "Đế tản nhiệt laptop", CategoryId = pk.Id, Price = 350000, Stock = 40, IsActive = true, ImagePath = "/uploads/products/pk003.jpg", Description = pkDesc, Brand = "MiniMart", WarrantyMonths = 6 },
                new Product { Sku = "PK004", Name = "Giá đỡ điện thoại", CategoryId = pk.Id, Price = 149000, Stock = 150, IsActive = true, ImagePath = "/uploads/products/pk004.jpg", Description = pkDesc, Brand = "MiniMart", WarrantyMonths = 6 },
                new Product { Sku = "PK005", Name = "Túi chống sốc laptop 15.6\"", CategoryId = pk.Id, Price = 450000, Stock = 60, IsActive = true, ImagePath = "/uploads/products/pk005.jpg", Description = pkDesc, Brand = "MiniMart", WarrantyMonths = 6 },
                new Product { Sku = "TN001", Name = "Tai nghe AirPods Pro 2", CategoryId = tainghe.Id, Price = 5490000, Stock = 45, IsActive = true, ImagePath = "/uploads/products/tn001.jpg", Description = tnDesc, Brand = "Apple", WarrantyMonths = 12, Specifications = "[{\"label\":\"Loại\",\"value\":\"True Wireless\"},{\"label\":\"Chống ồn\",\"value\":\"Chủ động (ANC)\"},{\"label\":\"Thời lượng pin\",\"value\":\"6h (30h với hộp)\"},{\"label\":\"Kết nối\",\"value\":\"Bluetooth 5.3\"},{\"label\":\"Chống nước\",\"value\":\"IPX4\"}]" },
                new Product { Sku = "TN002", Name = "Tai nghe Sony WH-1000XM5", CategoryId = tainghe.Id, Price = 7990000, Stock = 20, IsActive = true, ImagePath = "/uploads/products/tn002.jpg", Description = tnDesc, Brand = "Sony", WarrantyMonths = 12, Specifications = "[{\"label\":\"Loại\",\"value\":\"Over-ear\"},{\"label\":\"Chống ồn\",\"value\":\"Chủ động (ANC)\"},{\"label\":\"Thời lượng pin\",\"value\":\"30h\"},{\"label\":\"Kết nối\",\"value\":\"Bluetooth 5.2\"},{\"label\":\"Trọng lượng\",\"value\":\"250g\"}]" },
                new Product { Sku = "TN003", Name = "Tai nghe Samsung Buds2 Pro", CategoryId = tainghe.Id, Price = 3990000, Stock = 35, IsActive = true, ImagePath = "/uploads/products/tn003.jpg", Description = tnDesc, Brand = "Samsung", WarrantyMonths = 12, Specifications = "[{\"label\":\"Loại\",\"value\":\"True Wireless\"},{\"label\":\"Chống ồn\",\"value\":\"Chủ động (ANC)\"},{\"label\":\"Thời lượng pin\",\"value\":\"5h (29h với hộp)\"},{\"label\":\"Kết nối\",\"value\":\"Bluetooth 5.3\"},{\"label\":\"Chống nước\",\"value\":\"IPX7\"}]" },
                new Product { Sku = "TN004", Name = "Tai nghe chụp tai HyperX Cloud II", CategoryId = tainghe.Id, Price = 1890000, Stock = 25, IsActive = true, ImagePath = "/uploads/products/tn004.jpg", Description = tnDesc, Brand = "HyperX", WarrantyMonths = 24, Specifications = "[{\"label\":\"Loại\",\"value\":\"Over-ear Gaming\"},{\"label\":\"Tần số\",\"value\":\"15Hz-25kHz\"},{\"label\":\"Micro\",\"value\":\"Có\"},{\"label\":\"Kết nối\",\"value\":\"USB 3.5mm\"},{\"label\":\"Trọng lượng\",\"value\":\"320g\"}]" },
                new Product { Sku = "TN005", Name = "Tai nghe Anker Soundcore Q30", CategoryId = tainghe.Id, Price = 1590000, Stock = 50, IsActive = true, ImagePath = "/uploads/products/tn005.jpg", Description = tnDesc, Brand = "Anker", WarrantyMonths = 18, Specifications = "[{\"label\":\"Loại\",\"value\":\"Over-ear\"},{\"label\":\"Chống ồn\",\"value\":\"Chủ động (ANC)\"},{\"label\":\"Thời lượng pin\",\"value\":\"40h\"},{\"label\":\"Kết nối\",\"value\":\"Bluetooth 5.0\"},{\"label\":\"Trọng lượng\",\"value\":\"260g\"}]" },
                new Product { Sku = "SC001", Name = "Sạc nhanh GaN 65W (2 cổng)", CategoryId = saccap.Id, Price = 590000, Stock = 120, IsActive = true, ImagePath = "/uploads/products/sc001.jpg", Description = scDesc, Brand = "MiniMart", WarrantyMonths = 12, Specifications = "[{\"label\":\"Công suất\",\"value\":\"65W\"},{\"label\":\"Cổng\",\"value\":\"USB-C + USB-A\"},{\"label\":\"Công nghệ\",\"value\":\"GaN\"},{\"label\":\"Sạc nhanh\",\"value\":\"PD 3.0 / QC 4+\"}]" },
                new Product { Sku = "SC002", Name = "Cáp USB-C 2M (bện dù)", CategoryId = saccap.Id, Price = 149000, Stock = 250, IsActive = true, ImagePath = "/uploads/products/sc002.jpg", Description = scDesc, Brand = "MiniMart", WarrantyMonths = 6, Specifications = "[{\"label\":\"Chiều dài\",\"value\":\"2m\"},{\"label\":\"Đầu cắm\",\"value\":\"USB-C to USB-C\"},{\"label\":\"Chất liệu\",\"value\":\"Bện dù\"},{\"label\":\"Sạc nhanh\",\"value\":\"PD 100W\"}]" },
                new Product { Sku = "SC003", Name = "Sạc không dây MagSafe", CategoryId = saccap.Id, Price = 690000, Stock = 80, IsActive = true, ImagePath = "/uploads/products/sc003.jpg", Description = scDesc, Brand = "MiniMart", WarrantyMonths = 12, Specifications = "[{\"label\":\"Công suất\",\"value\":\"15W\"},{\"label\":\"Chuẩn\",\"value\":\"Qi / MagSafe\"},{\"label\":\"Tương thích\",\"value\":\"iPhone 12+\"}]" },
                new Product { Sku = "SC004", Name = "Adapter sạc laptop 100W", CategoryId = saccap.Id, Price = 890000, Stock = 30, IsActive = true, ImagePath = "/uploads/products/sc004.jpg", Description = scDesc, Brand = "MiniMart", WarrantyMonths = 12, Specifications = "[{\"label\":\"Công suất\",\"value\":\"100W\"},{\"label\":\"Cổng\",\"value\":\"USB-C\"},{\"label\":\"Sạc nhanh\",\"value\":\"PD 3.0\"},{\"label\":\"Tương thích\",\"value\":\"Laptop MacBook / Dell / HP\"}]" },
                new Product { Sku = "SC005", Name = "Cáp Lightning 1M (MFi)", CategoryId = saccap.Id, Price = 199000, Stock = 180, IsActive = true, ImagePath = "/uploads/products/sc005.jpg", Description = scDesc, Brand = "MiniMart", WarrantyMonths = 6, Specifications = "[{\"label\":\"Chiều dài\",\"value\":\"1m\"},{\"label\":\"Đầu cắm\",\"value\":\"Lightning to USB-C\"},{\"label\":\"Chứng nhận\",\"value\":\"MFi\"},{\"label\":\"Sạc nhanh\",\"value\":\"PD 20W\"}]" },
                new Product { Sku = "BP001", Name = "Bàn phím cơ Logitech MX Mechanical", CategoryId = banphim.Id, Price = 3490000, Stock = 20, IsActive = true, ImagePath = "/uploads/products/bp001.jpg", Description = bpDesc, Brand = "Logitech", WarrantyMonths = 24, Specifications = "[{\"label\":\"Loại\",\"value\":\"Cơ (Mechanical)\"},{\"label\":\"Kết nối\",\"value\":\"Bluetooth / USB-C\"},{\"label\":\"Pin\",\"value\":\"15 ngày\"},{\"label\":\"Trọng lượng\",\"value\":\"828g\"}]" },
                new Product { Sku = "BP002", Name = "Bàn phím cơ AKKO 3087", CategoryId = banphim.Id, Price = 1290000, Stock = 35, IsActive = true, ImagePath = "/uploads/products/bp002.jpg", Description = bpDesc, Brand = "AKKO", WarrantyMonths = 12, Specifications = "[{\"label\":\"Loại\",\"value\":\"Cơ (Mechanical)\"},{\"label\":\"Kết nối\",\"value\":\"USB-C\"},{\"label\":\"Switch\",\"value\":\"AKKO V3\"},{\"label\":\"LED\",\"value\":\"RGB\"}]" },
                new Product { Sku = "BP003", Name = "Chuột Logitech G502 Hero", CategoryId = banphim.Id, Price = 1290000, Stock = 40, IsActive = true, ImagePath = "/uploads/products/bp003.jpg", Description = bpDesc, Brand = "Logitech", WarrantyMonths = 24, Specifications = "[{\"label\":\"Kết nối\",\"value\":\"USB\"},{\"label\":\"DPI\",\"value\":\"25,600\"},{\"label\":\"Nút bấm\",\"value\":\"11 nút\"},{\"label\":\"LED\",\"value\":\"RGB\"}]" },
                new Product { Sku = "BP004", Name = "Chuột không dây Logitech MX Master 3S", CategoryId = banphim.Id, Price = 1990000, Stock = 25, IsActive = true, ImagePath = "/uploads/products/bp004.jpg", Description = bpDesc, Brand = "Logitech", WarrantyMonths = 24, Specifications = "[{\"label\":\"Kết nối\",\"value\":\"Bluetooth / USB-C\"},{\"label\":\"DPI\",\"value\":\"8,000\"},{\"label\":\"Pin\",\"value\":\"70 ngày\"},{\"label\":\"Ergonomics\",\"value\":\"Công thái học\"}]" },
                new Product { Sku = "BP005", Name = "Mousepad Razer Gigantus V2 L", CategoryId = banphim.Id, Price = 399000, Stock = 60, IsActive = true, ImagePath = "/uploads/products/bp005.jpg", Description = bpDesc, Brand = "Razer", WarrantyMonths = 12, Specifications = "[{\"label\":\"Kích thước\",\"value\":\"455x455mm\"},{\"label\":\"Độ dày\",\"value\":\"3mm\"},{\"label\":\"Chất liệu\",\"value\":\"Vải micro-weave\"},{\"label\":\"Đế\",\"value\":\"Chống trượt\"}]" }
            );
            db.SaveChanges();
        }

        // ==================== SALES INVOICES + ITEMS + STOCK LEDGER ====================
        if (!db.SalesInvoices.Any())
        {
            var customers = db.Customers.ToList();
            var products = db.Products.Where(p => p.IsActive).ToList();
            var rng = new Random(123);
            var now = DateTime.Now;

            string[] statuses = ["Pending", "Confirmed", "Shipped", "Completed", "Cancelled"];
            string[] payments = ["Unpaid", "Paid"];
            string[] payMethods = ["COD", "BANK", "CARD"];

            // Tạo 15 hóa đơn trong 30 ngày qua
            for (int i = 0; i < 15; i++)
            {
                var customer = customers[rng.Next(customers.Count)];
                var createdAt = now.AddDays(-rng.Next(1, 30)).AddHours(rng.Next(7, 22));
                var status = statuses[rng.Next(statuses.Length)];
                var isPaid = status == "Completed" || (rng.Next(2) == 0 && status != "Cancelled");

                var inv = new SalesInvoice
                {
                    Code = "",
                    CustomerId = customer.Id,
                    CreatedAt = createdAt,
                    Status = status,
                    PaymentStatus = isPaid ? "Paid" : "Unpaid",
                    PaymentMethod = payMethods[rng.Next(payMethods.Length)],
                    Discount = 0,
                    ShippingFee = rng.Next(2) == 0 ? 30000 : 0,
                    Total = 0,
                    ReceiverName = customer.Name,
                    ShippingPhone = customer.Phone,
                    ShippingAddress = customer.Address
                };
                db.SalesInvoices.Add(inv);
                db.SaveChanges();

                inv.Code = $"DH{inv.Id:000000}";

                // Thêm 1-4 sản phẩm mỗi hóa đơn
                int itemCount = rng.Next(1, 5);
                decimal total = 0;
                for (int j = 0; j < itemCount; j++)
                {
                    var prod = products[rng.Next(products.Count)];
                    int qty = rng.Next(1, 4);
                    var lineTotal = prod.Price * qty;
                    total += lineTotal;

                    db.SalesItems.Add(new SalesItem
                    {
                        SalesInvoiceId = inv.Id,
                        ProductId = prod.Id,
                        Quantity = qty,
                        UnitPrice = prod.Price,
                        LineTotal = lineTotal
                    });

                    // Chỉ trừ stock nếu không phải cancelled
                    if (status != "Cancelled")
                    {
                        var stockUpdate = products.First(p => p.Id == prod.Id);
                        stockUpdate.Stock = Math.Max(0, stockUpdate.Stock - qty);

                        db.StockLedgers.Add(new StockLedger
                        {
                            ProductId = prod.Id,
                            Type = "OUT",
                            Quantity = qty,
                            OccurredAt = createdAt,
                            RefType = "Sale",
                            RefId = inv.Id
                        });
                    }
                }

                total = total - inv.Discount + inv.ShippingFee;
                inv.Total = total;
            }
            db.SaveChanges();
        }

        // ==================== PROMOTIONS ====================
        if (!db.Promotions.Any())
        {
            var dtNow = DateTime.Now;
            var phoneCat = db.Categories.FirstOrDefault(c => c.Name == "Điện thoại");
            var laptopCat = db.Categories.FirstOrDefault(c => c.Name == "Laptop");
            var accessoryCat = db.Categories.FirstOrDefault(c => c.Name == "Phụ kiện");
            var products = db.Products.ToList();

            var promos = new List<Promotion>
            {
                new() {
                    Name = "Giảm 10% điện thoại",
                    Code = "DT10",
                    Description = "Giảm 10% cho tất cả điện thoại, áp dụng cho đơn từ 1 triệu",
                    DiscountType = "Percentage", DiscountValue = 10,
                    MinOrderValue = 1000000,
                    StartDate = dtNow.AddDays(-1), EndDate = dtNow.AddMonths(1),
                    IsActive = true, CreatedAt = dtNow
                },
                new() {
                    Name = "Giảm 200K đơn từ 5 triệu",
                    Code = "GIAM200K",
                    Description = "Giảm thẳng 200.000₫ cho đơn hàng từ 5 triệu",
                    DiscountType = "Fixed", DiscountValue = 200000,
                    MinOrderValue = 5000000,
                    StartDate = dtNow.AddDays(-1), EndDate = dtNow.AddMonths(2),
                    IsActive = true, CreatedAt = dtNow
                },
                new() {
                    Name = "Flash sale cuối tuần 30%",
                    Code = "FS30",
                    Description = "Giảm 30% cho laptop và phụ kiện",
                    DiscountType = "Percentage", DiscountValue = 30,
                    MinOrderValue = 500000,
                    StartDate = dtNow.AddDays(-1), EndDate = dtNow.AddDays(7),
                    IsActive = true, CreatedAt = dtNow
                },
                new() {
                    Name = "Free ship toàn quốc",
                    Code = "FREESHIP",
                    Description = "Miễn phí vận chuyển toàn quốc cho mọi đơn hàng",
                    DiscountType = "Fixed", DiscountValue = 50000,
                    StartDate = dtNow.AddDays(-1), EndDate = dtNow.AddDays(30),
                    IsActive = true, CreatedAt = dtNow
                },
                new() {
                    Name = "Giảm 15% đơn đầu tiên",
                    Code = "WELCOME15",
                    Description = "Dành cho khách hàng mới — giảm 15% đơn đầu tiên, tối đa 500K",
                    DiscountType = "Percentage", DiscountValue = 15,
                    MinOrderValue = 200000,
                    StartDate = dtNow.AddDays(-1), EndDate = dtNow.AddMonths(3),
                    IsActive = true, CreatedAt = dtNow
                },
                new() {
                    Name = "Khuyến mãi mùa hè (hết hạn)",
                    Code = "HE2025",
                    Description = "Đã hết hạn — dùng để test",
                    DiscountType = "Percentage", DiscountValue = 15,
                    MinOrderValue = 100000,
                    StartDate = dtNow.AddMonths(-3), EndDate = dtNow.AddDays(-1),
                    IsActive = true, CreatedAt = dtNow
                }
            };
            db.Promotions.AddRange(promos);
            db.SaveChanges();

            // Gán sản phẩm
            foreach (var p in promos)
            {
                if (p.Code == "DT10" && phoneCat != null)
                {
                    foreach (var prod in products.Where(x => x.CategoryId == phoneCat.Id))
                        db.PromotionProducts.Add(new PromotionProduct { PromotionId = p.Id, ProductId = prod.Id });
                }
                if (p.Code == "FS30")
                {
                    foreach (var prod in products.Where(x => x.CategoryId == laptopCat?.Id || x.CategoryId == accessoryCat?.Id))
                        db.PromotionProducts.Add(new PromotionProduct { PromotionId = p.Id, ProductId = prod.Id });
                }
            }
            db.SaveChanges();
        }

        // ==================== REVIEWS MẪU ====================
        if (!db.Reviews.Any())
        {
            var allProducts = db.Products.Where(p => p.IsActive).ToList();
            var custs = db.Customers.Take(3).ToList();
            var rng2 = new Random(456);
            var reviewTexts = new[]
            {
                "Sản phẩm tốt, đóng gói cẩn thận, giao hàng nhanh.",
                "Chất lượng ổn, giá hợp lý. Sẽ ủng hộ lần sau.",
                "Hàng đúng mô tả, rất hài lòng.",
                "Tạm ổn, nhưng giao hàng hơi chậm.",
                "Sản phẩm xuất sắc, đáng tiền!",
                "Dùng tốt, nên mua.",
            };
            foreach (var prod in allProducts.Take(20))
            {
                int reviewCount = rng2.Next(1, 4);
                for (int i = 0; i < reviewCount; i++)
                {
                    var c = custs[rng2.Next(custs.Count)];
                    db.Reviews.Add(new Review
                    {
                        ProductId = prod.Id,
                        CustomerId = c.Id,
                        Rating = rng2.Next(3, 6),
                        Content = reviewTexts[rng2.Next(reviewTexts.Length)],
                        CreatedAt = DateTime.Now.AddDays(-rng2.Next(1, 60))
                    });
                }
            }
            db.SaveChanges();
        }

        // ==================== BANNERS ====================
        if (!db.Banners.Any())
        {
            db.Banners.AddRange(
                new Banner
                {
                    Title = "iPhone 15 Pro Max — Chính hãng giá tốt",
                    ImagePath = "https://picsum.photos/seed/banner1/1200/400",
                    SortOrder = 1,
                    IsActive = true,
                    CreatedAt = DateTime.Now
                },
                new Banner
                {
                    Title = "MacBook Air M3 — Siêu nhẹ, siêu mạnh",
                    ImagePath = "https://picsum.photos/seed/banner2/1200/400",
                    SortOrder = 2,
                    IsActive = true,
                    CreatedAt = DateTime.Now
                },
                new Banner
                {
                    Title = "Giảm đến 30% phụ kiện công nghệ",
                    ImagePath = "https://picsum.photos/seed/banner3/1200/400",
                    SortOrder = 3,
                    IsActive = true,
                    CreatedAt = DateTime.Now
                }
            );
            db.SaveChanges();
        }

        // ==================== ARTICLES ====================
        if (!db.Articles.Any())
        {
            db.Articles.AddRange(
                new Article
                {
                    Title = "Top 5 điện thoại đáng mua nhất 2026",
                    Slug = "top-5-dien-thoai-dang-mua-nhat-2026",
                    Summary = "Bạn đang phân vân chọn điện thoại mới? Bài viết tổng hợp top 5 smartphone đáng mua nhất năm 2026 với đầy đủ phân khúc từ bình dân đến cao cấp.",
                    Content = "Thị trường smartphone năm 2026 chứng kiến nhiều đột phá về công nghệ.\n\n" +
                              "1. iPhone 17 Pro Max — Flagship mới nhất từ Apple với chip A19 Pro, camera 48MP và pin siêu trâu.\n\n" +
                              "2. Samsung Galaxy S26 Ultra — Màn hình Dynamic AMOLED 120Hz, camera 200MP, bút S-Pen tích hợp.\n\n" +
                              "3. Xiaomi 16 Pro — Giá tốt nhất phân khúc cao cấp với chip Snapdragon 9 Gen 4.\n\n" +
                              "4. OPPO Find N6 Fold — Điện thoại gập mỏng nhẹ, màn hình không nếp gấp.\n\n" +
                              "5. Google Pixel 11 — Camera tính toán xuất sắc, Android thuần khiết.\n\n" +
                              "Kết luận: Tuỳ vào nhu cầu và ngân sách, bạn có thể chọn cho mình một chiếc máy phù hợp. Ghé MiniMart để trải nghiệm thực tế trước khi quyết định nhé!",
                    ImagePath = "https://picsum.photos/seed/article1/800/400",
                    IsActive = true,
                    CreatedAt = DateTime.Now.AddDays(-3),
                    CreatedBy = "admin"
                },
                new Article
                {
                    Title = "Hướng dẫn chọn laptop sinh viên 2026",
                    Slug = "huong-dan-chon-laptop-sinh-vien-2026",
                    Summary = "Những tiêu chí quan trọng khi chọn laptop cho sinh viên: cấu hình, thời lượng pin, trọng lượng và giá cả.",
                    Content = "Chọn laptop cho sinh viên cần cân nhắc nhiều yếu tố:\n\n" +
                              "1. Mục đích sử dụng — Học tập văn phòng hay đồ hoạ, lập trình?\n\n" +
                              "2. Thời lượng pin — Tối thiểu 8 tiếng cho một buổi học.\n\n" +
                              "3. Trọng lượng — Dưới 1.5kg cho việc di chuyển hàng ngày.\n\n" +
                              "4. Giá cả — Phân khúc 15-30 triệu là lý tưởng cho sinh viên.\n\n" +
                              "Gợi ý: MacBook Air M3, Dell XPS 15, Lenovo ThinkPad X1 Carbon là những lựa chọn hàng đầu.",
                    ImagePath = "https://picsum.photos/seed/article2/800/400",
                    IsActive = true,
                    CreatedAt = DateTime.Now.AddDays(-7),
                    CreatedBy = "admin"
                },
                new Article
                {
                    Title = "Công nghệ sạc nhanh GaN — Tại sao nên nâng cấp?",
                    Slug = "cong-nghe-sac-nhanh-gan",
                    Summary = "Sạc GaN nhỏ gọn hơn, mát hơn và sạc nhanh hơn sạc thường. Tìm hiểu lý do bạn nên chuyển sang sạc GaN ngay hôm nay.",
                    Content = "Công nghệ GaN (Gallium Nitride) đang thay đổi ngành sạc:\n\n" +
                              "1. Kích thước nhỏ hơn 50% so với sạc Silicon truyền thống.\n\n" +
                              "2. Tản nhiệt tốt hơn, sạc lâu không bị nóng.\n\n" +
                              "3. Hiệu suất chuyển đổi cao, tiết kiệm điện.\n\n" +
                              "4. Hỗ trợ đa giao thức: PD 3.0, QC 4+, PPS.\n\n" +
                              "MiniMart hiện có sẵn sạc GaN 65W và 100W với giá tốt nhất thị trường.",
                    ImagePath = "https://picsum.photos/seed/article3/800/400",
                    IsActive = true,
                    CreatedAt = DateTime.Now.AddDays(-14),
                    CreatedBy = "admin"
                },
                new Article
                {
                    Title = "So sánh AirPods Pro 2 vs Sony WF-1000XM6",
                    Slug = "so-sanh-airpods-pro-2-vs-sony-wf-1000xm6",
                    Summary = "Cuộc chiến tai nghe true wireless cao cấp: bên nào xứng đáng với số tiền bạn bỏ ra?",
                    Content = "Hai ông lớn trong làng tai nghe true wireless:\n\n" +
                              "--- AirPods Pro 2 ---\n" +
                              "• Chip H2, chống ồn ANC cải thiện 2x\n" +
                              "• Âm thanh thích ứng, spatial audio\n" +
                              "• Thời lượng pin: 6h (30h với hộp)\n" +
                              "• Tích hợp sâu với hệ sinh thái Apple\n" +
                              "• Giá: 5.490.000₫\n\n" +
                              "--- Sony WF-1000XM6 ---\n" +
                              "• Driver dynamic mới, âm bass mạnh\n" +
                              "• Chống ồn ANC tốt nhất thị trường\n" +
                              "• Thời lượng pin: 8h (32h với hộp)\n" +
                              "• Ứng dụng Sony Headphones Connect\n" +
                              "• Giá: 7.990.000₫\n\n" +
                              "Kết luận: Nếu bạn dùng iPhone — AirPods Pro 2 là lựa chọn tối ưu. Nếu bạn muốn chất âm tốt nhất — hãy chọn Sony.",
                    ImagePath = "https://picsum.photos/seed/article4/800/400",
                    IsActive = true,
                    CreatedAt = DateTime.Now.AddDays(-21),
                    CreatedBy = "admin"
                },
                new Article
                {
                    Title = "Bí quyết bảo quản pin laptop bền lâu",
                    Slug = "bi-quyet-bao-quan-pin-laptop",
                    Summary = "Pin laptop xuống cấp sau vài tháng? Áp dụng ngay những mẹo nhỏ này để kéo dài tuổi thọ pin.",
                    Content = "Tuổi thọ pin lithium-ion phụ thuộc nhiều vào thói quen sạc:\n\n" +
                              "1. Không sạc qua đêm — Dừng ở 80-90% là lý tưởng.\n\n" +
                              "2. Không để pin cạn kiệt hoàn toàn — Sạc khi còn 20%.\n\n" +
                              "3. Tránh nhiệt độ cao — Không dùng laptop trên chăn/gối khi đang sạc.\n\n" +
                              "4. Vệ sinh cổng sạc định kỳ.\n\n" +
                              "5. Nếu không dùng lâu, giữ pin ở mức 50% và tắt máy hoàn toàn.",
                    ImagePath = "https://picsum.photos/seed/article5/800/400",
                    IsActive = true,
                    CreatedAt = DateTime.Now.AddDays(-30),
                    CreatedBy = "admin"
                }
            );
            db.SaveChanges();
        }

        // ==================== TÀI KHOẢN KHÁCH HÀNG DEMO ====================
        var demoCustomer = db.Customers.FirstOrDefault(x => x.Name == "Khách hàng demo")
            ?? db.Customers.First();
        if (demoCustomer.WalletBalance <= 0)
            demoCustomer.WalletBalance = 50000000;

        if (!db.Users.Any(x => x.Username == "khach"))
            db.Users.Add(new User { Username = "khach", Password = "123", Role = 2, CustomerId = demoCustomer.Id });

        db.SaveChanges();
    }
}
