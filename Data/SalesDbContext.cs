using Microsoft.EntityFrameworkCore;
using QuanLyBanHang.Models;

namespace QuanLyBanHang.Data;

public class SalesDbContext : DbContext
{
    public SalesDbContext(DbContextOptions<SalesDbContext> options) : base(options) { }

    public DbSet<User> Users => Set<User>();

    public DbSet<Category> Categories => Set<Category>();
    public DbSet<Product> Products => Set<Product>();
    public DbSet<Customer> Customers => Set<Customer>();
    public DbSet<Supplier> Suppliers => Set<Supplier>();

    public DbSet<SalesInvoice> SalesInvoices => Set<SalesInvoice>();
    public DbSet<SalesItem> SalesItems => Set<SalesItem>();

    public DbSet<PurchaseInvoice> PurchaseInvoices => Set<PurchaseInvoice>();
    public DbSet<PurchaseItem> PurchaseItems => Set<PurchaseItem>();

    public DbSet<StockLedger> StockLedgers => Set<StockLedger>();

    public DbSet<Promotion> Promotions => Set<Promotion>();
    public DbSet<PromotionProduct> PromotionProducts => Set<PromotionProduct>();

    public DbSet<Wishlist> Wishlists => Set<Wishlist>();
    public DbSet<Review> Reviews => Set<Review>();
    public DbSet<Notification> Notifications => Set<Notification>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.Entity<User>()
            .HasIndex(x => x.Username)
            .IsUnique();

        // Khách hàng sẽ gắn với CustomerId để xem đơn hàng của mình
        modelBuilder.Entity<User>()
            .HasOne(x => x.Customer)
            .WithMany()
            .HasForeignKey(x => x.CustomerId)
            .OnDelete(DeleteBehavior.SetNull);

        modelBuilder.Entity<Product>()
            .HasIndex(x => x.Sku)
            .IsUnique();

        modelBuilder.Entity<Product>()
            .Property(x => x.Price)
            .HasPrecision(18, 2);

        modelBuilder.Entity<SalesInvoice>()
            .HasIndex(x => x.Code);

        modelBuilder.Entity<SalesInvoice>()
            .Property(x => x.Total)
            .HasPrecision(18, 2);

        modelBuilder.Entity<SalesInvoice>()
            .Property(x => x.ShippingFee)
            .HasPrecision(18, 2);

        modelBuilder.Entity<SalesInvoice>()
            .Property(x => x.Discount)
            .HasPrecision(18, 2);

        modelBuilder.Entity<PurchaseInvoice>()
            .Property(x => x.Total)
            .HasPrecision(18, 2);

        modelBuilder.Entity<SalesItem>()
            .Property(x => x.UnitPrice)
            .HasPrecision(18, 2);

        modelBuilder.Entity<SalesItem>()
            .Property(x => x.LineTotal)
            .HasPrecision(18, 2);

        modelBuilder.Entity<PurchaseItem>()
            .Property(x => x.UnitCost)
            .HasPrecision(18, 2);

        modelBuilder.Entity<PurchaseItem>()
            .Property(x => x.LineTotal)
            .HasPrecision(18, 2);

        modelBuilder.Entity<Category>()
            .HasMany(x => x.Products)
            .WithOne(x => x.Category)
            .HasForeignKey(x => x.CategoryId)
            .OnDelete(DeleteBehavior.SetNull);

        modelBuilder.Entity<SalesInvoice>()
            .HasMany(x => x.Items)
            .WithOne(x => x.Invoice)
            .HasForeignKey(x => x.SalesInvoiceId)
            .OnDelete(DeleteBehavior.Cascade);

        modelBuilder.Entity<PurchaseInvoice>()
            .HasMany(x => x.Items)
            .WithOne(x => x.Invoice)
            .HasForeignKey(x => x.PurchaseInvoiceId)
            .OnDelete(DeleteBehavior.Cascade);

        
        modelBuilder.Entity<Customer>()
            .Property(x => x.WalletBalance)
            .HasPrecision(18, 2);

        modelBuilder.Entity<Promotion>()
            .HasIndex(x => x.Code)
            .IsUnique();

        modelBuilder.Entity<Promotion>()
            .Property(x => x.DiscountValue)
            .HasPrecision(18, 2);

        modelBuilder.Entity<Promotion>()
            .Property(x => x.MinOrderValue)
            .HasPrecision(18, 2);

        modelBuilder.Entity<PromotionProduct>()
            .HasOne(x => x.Promotion)
            .WithMany(x => x.PromotionProducts)
            .HasForeignKey(x => x.PromotionId)
            .OnDelete(DeleteBehavior.Cascade);

        modelBuilder.Entity<PromotionProduct>()
            .HasOne(x => x.Product)
            .WithMany()
            .HasForeignKey(x => x.ProductId)
            .OnDelete(DeleteBehavior.Cascade);

        modelBuilder.Entity<Wishlist>()
            .HasOne(x => x.Customer)
            .WithMany()
            .HasForeignKey(x => x.CustomerId)
            .OnDelete(DeleteBehavior.Cascade);

        modelBuilder.Entity<Wishlist>()
            .HasOne(x => x.Product)
            .WithMany()
            .HasForeignKey(x => x.ProductId)
            .OnDelete(DeleteBehavior.Cascade);

        modelBuilder.Entity<Wishlist>()
            .HasIndex(x => new { x.CustomerId, x.ProductId })
            .IsUnique();

        modelBuilder.Entity<Review>()
            .HasOne(x => x.Customer)
            .WithMany()
            .HasForeignKey(x => x.CustomerId)
            .OnDelete(DeleteBehavior.Cascade);

        modelBuilder.Entity<Review>()
            .HasOne(x => x.Product)
            .WithMany()
            .HasForeignKey(x => x.ProductId)
            .OnDelete(DeleteBehavior.Cascade);

        modelBuilder.Entity<Notification>()
            .HasOne(x => x.Customer)
            .WithMany()
            .HasForeignKey(x => x.CustomerId)
            .OnDelete(DeleteBehavior.Cascade);

        modelBuilder.Entity<Notification>()
            .Property(x => x.Title)
            .HasMaxLength(200);

        modelBuilder.Entity<Notification>()
            .Property(x => x.Message)
            .HasMaxLength(1000);

        modelBuilder.Entity<Notification>()
            .Property(x => x.Url)
            .HasMaxLength(500);

        base.OnModelCreating(modelBuilder);
    }
}
