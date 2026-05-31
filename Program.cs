using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.EntityFrameworkCore;
using QuanLyBanHang.Data;
using QuanLyBanHang.Services;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddControllersWithViews();

// DB
var cs = builder.Configuration.GetConnectionString("DefaultConnection");
builder.Services.AddDbContext<SalesDbContext>(options =>
    options.UseMySql(cs, ServerVersion.AutoDetect(cs)));

// Session (giỏ hàng)
builder.Services.AddDistributedMemoryCache();
builder.Services.AddSession(options =>
{
    options.Cookie.Name = ".QuanLyBanHang.Session";
    options.IdleTimeout = TimeSpan.FromHours(2);
    options.Cookie.HttpOnly = true;
    options.Cookie.IsEssential = true;
});

// Cookie Auth
builder.Services.AddAuthentication(CookieAuthenticationDefaults.AuthenticationScheme)
    .AddCookie(options =>
    {
        options.LoginPath = "/Account/Login";
        options.LogoutPath = "/Account/Logout";
        options.AccessDeniedPath = "/Account/AccessDenied";
        options.SlidingExpiration = true;
    });

builder.Services.AddAuthorization();

// Services
builder.Services.Configure<EmailOptions>(builder.Configuration.GetSection(EmailOptions.Section));
builder.Services.AddScoped<IEmailService, EmailService>();
builder.Services.AddHttpContextAccessor();

var app = builder.Build();

if (!app.Environment.IsDevelopment())
{
    app.UseExceptionHandler("/Home/Error");
    app.UseHsts();
}

app.UseHttpsRedirection();
app.UseStaticFiles();

app.UseRouting();

app.UseSession();

app.UseAuthentication();
app.UseAuthorization();

// Tạo DB + seed dữ liệu mẫu. Tự động migrate các bảng/cột mới nếu DB đã tồn tại.
using (var scope = app.Services.CreateScope())
{
    var db = scope.ServiceProvider.GetRequiredService<SalesDbContext>();
    db.Database.EnsureCreated();
    await MigrateSchemaAsync(db);

    SeedData.Seed(db);
}

static async Task MigrateSchemaAsync(SalesDbContext db)
{
    var sqls = new List<string>();

    sqls.Add("CREATE TABLE IF NOT EXISTS Wishlists (Id bigint NOT NULL AUTO_INCREMENT, CustomerId bigint NOT NULL, ProductId bigint NOT NULL, CreatedAt datetime NOT NULL DEFAULT CURRENT_TIMESTAMP, PRIMARY KEY (Id), UNIQUE KEY IX_Wishlists_CustomerId_ProductId (CustomerId,ProductId), CONSTRAINT FK_Wishlists_Customers_CustomerId FOREIGN KEY (CustomerId) REFERENCES Customers (Id) ON DELETE CASCADE, CONSTRAINT FK_Wishlists_Products_ProductId FOREIGN KEY (ProductId) REFERENCES Products (Id) ON DELETE CASCADE) ENGINE=InnoDB DEFAULT CHARSET=utf8mb4;");
    sqls.Add("CREATE TABLE IF NOT EXISTS Reviews (Id bigint NOT NULL AUTO_INCREMENT, ProductId bigint NOT NULL, CustomerId bigint NOT NULL, Rating int NOT NULL, Content longtext CHARACTER SET utf8mb4 NULL, CreatedAt datetime NOT NULL DEFAULT CURRENT_TIMESTAMP, PRIMARY KEY (Id), KEY IX_Reviews_ProductId (ProductId), CONSTRAINT FK_Reviews_Products_ProductId FOREIGN KEY (ProductId) REFERENCES Products (Id) ON DELETE CASCADE, CONSTRAINT FK_Reviews_Customers_CustomerId FOREIGN KEY (CustomerId) REFERENCES Customers (Id) ON DELETE CASCADE) ENGINE=InnoDB DEFAULT CHARSET=utf8mb4;");
    sqls.Add("CREATE TABLE IF NOT EXISTS Notifications (Id bigint NOT NULL AUTO_INCREMENT, CustomerId bigint NOT NULL, Title varchar(200) CHARACTER SET utf8mb4 NOT NULL, Message varchar(1000) CHARACTER SET utf8mb4 NULL, IsRead tinyint(1) NOT NULL DEFAULT 0, Url varchar(500) CHARACTER SET utf8mb4 NULL, CreatedAt datetime NOT NULL DEFAULT CURRENT_TIMESTAMP, PRIMARY KEY (Id), KEY IX_Notifications_CustomerId (CustomerId), CONSTRAINT FK_Notifications_Customers_CustomerId FOREIGN KEY (CustomerId) REFERENCES Customers (Id) ON DELETE CASCADE) ENGINE=InnoDB DEFAULT CHARSET=utf8mb4;");

    foreach (var sql in sqls)
    {
        try { await db.Database.ExecuteSqlRawAsync(sql); }
        catch { }
    }
}


// ROUTING (tách Admin/Customer bằng Areas)
app.MapControllerRoute(
    name: "home",
    pattern: "",
    defaults: new { controller = "Home", action = "Index" });

// Account ngoài Area
app.MapControllerRoute(
    name: "account",
    pattern: "Account/{action=Login}/{id?}",
    defaults: new { controller = "Account" });

// Admin area: /Admin/...
app.MapAreaControllerRoute(
    name: "admin",
    areaName: "Admin",
    pattern: "Admin/{controller=Admin}/{action=Index}/{id?}");

// Customer area: /Shop, /Cart, /Checkout... (không cần prefix Customer)
app.MapAreaControllerRoute(
    name: "customer",
    areaName: "Customer",
    pattern: "{controller}/{action=Index}/{id?}");

// Fallback: controller ngoài Area (nếu cần)
app.MapControllerRoute(
    name: "default",
    pattern: "{controller=Home}/{action=Index}/{id?}");

app.Run();
