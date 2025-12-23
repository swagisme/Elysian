using Elysian.Models;
using Microsoft.AspNetCore.Identity.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore;

namespace Elysian.Data
{
    public class ApplicationDbContext : IdentityDbContext<ApplicationUser>
    {
        public ApplicationDbContext(DbContextOptions<ApplicationDbContext> options)
            : base(options)
        {
        }

        public DbSet<Product> Products { get; set; }
        public DbSet<Message> Messages { get; set; }
        public DbSet<Cart> Carts { get; set; }
        public DbSet<CartItem> CartItems { get; set; }
        public DbSet<Order> Orders { get; set; }
        public DbSet<OrderItem> OrderItems { get; set; }
        public DbSet<OrderHistory> OrderHistories { get; set; }

        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            base.OnModelCreating(modelBuilder);

            // Configure Order-User relationship (UserId can be nullable for guest orders)
            modelBuilder.Entity<Order>(entity =>
            {
                entity.HasOne(o => o.User)
                      .WithMany()
                      .HasForeignKey(o => o.UserId)
                      .IsRequired(false); // ✅ Allow null for guest orders

                // Configure decimal precision
                entity.Property(o => o.TotalAmount)
                      .HasPrecision(18, 2);

                // Remove NotMapped properties from model configuration
                entity.Ignore(o => o.OrderNumber);
                entity.Ignore(o => o.PaymentMethod);
             
            });

            // Configure OrderItem
            modelBuilder.Entity<OrderItem>(entity =>
            {
                entity.HasOne(oi => oi.Order)
                      .WithMany(o => o.Items)
                      .HasForeignKey(oi => oi.OrderId)
                      .OnDelete(DeleteBehavior.Cascade);

                entity.HasOne(oi => oi.Product)
                      .WithMany(p => p.OrderItems)
                      .HasForeignKey(oi => oi.ProductId)
                      .OnDelete(DeleteBehavior.Restrict);

                entity.Property(oi => oi.UnitPrice)
                      .HasPrecision(18, 2);
            });

            // Configure Product
            modelBuilder.Entity<Product>(entity =>
            {
                entity.Property(p => p.Price)
                      .HasPrecision(18, 2);

                entity.Property(p => p.Rating)
                      .HasPrecision(3, 1); // 3 digits total, 1 decimal

                // Remove CreatedAt/UpdatedAt if they don't exist in database
                // entity.Ignore(p => p.CreatedAt);
                // entity.Ignore(p => p.UpdatedAt);
            });

            // Configure OrderHistory
            modelBuilder.Entity<OrderHistory>(entity =>
            {
                entity.HasOne(oh => oh.Order)
                      .WithMany(o => o.History)
                      .HasForeignKey(oh => oh.OrderId)
                      .OnDelete(DeleteBehavior.Cascade);
            });

            // Configure Cart
            modelBuilder.Entity<Cart>(entity =>
            {
                entity.HasOne(c => c.User)
                      .WithMany()
                      .HasForeignKey(c => c.UserId)
                      .IsRequired(false); // Allow null for guest carts
            });

            // Configure CartItem
            modelBuilder.Entity<CartItem>(entity =>
            {
                entity.HasOne(ci => ci.Cart)
                      .WithMany(c => c.Items)
                      .HasForeignKey(ci => ci.CartId)
                      .OnDelete(DeleteBehavior.Cascade);

                entity.HasOne(ci => ci.Product)
                      .WithMany()
                      .HasForeignKey(ci => ci.ProductId)
                      .OnDelete(DeleteBehavior.Restrict);
            });
        }
    }
}