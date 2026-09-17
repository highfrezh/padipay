using Microsoft.AspNetCore.Identity.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore;
using PadiPay.Models;

namespace PadiPay.Data;

public class ApplicationDbContext : IdentityDbContext<ApplicationUser>
{
    public ApplicationDbContext(DbContextOptions<ApplicationDbContext> options)
        : base(options)
    {
    }

    public DbSet<Wallet> Wallets { get; set; }
    public DbSet<Transaction> Transactions { get; set; }
    public DbSet<Category> Categories { get; set; }
    public DbSet<Gig> Gigs { get; set; }
    public DbSet<Order> Orders { get; set; }
    public DbSet<OrderDelivery> OrderDeliveries { get; set; }

    protected override void OnModelCreating(ModelBuilder builder)
    {
        base.OnModelCreating(builder);

        // Configure decimal precision
        builder.Entity<Transaction>()
            .Property(t => t.Amount)
            .HasColumnType("decimal(18,2)");

        builder.Entity<Gig>()
            .Property(g => g.Price)
            .HasColumnType("decimal(18,2)");

        builder.Entity<Order>()
            .Property(o => o.Amount)
            .HasColumnType("decimal(18,2)");

        // One-to-One: ApplicationUser - Wallet
        builder.Entity<ApplicationUser>()
            .HasOne(u => u.Wallet)
            .WithOne(w => w.User)
            .HasForeignKey<Wallet>(w => w.UserId)
            .OnDelete(DeleteBehavior.Cascade);

        // One-to-Many: ApplicationUser (Seller) - Gigs
        builder.Entity<Gig>()
            .HasOne(g => g.Seller)
            .WithMany(u => u.Gigs)
            .HasForeignKey(g => g.SellerId)
            .OnDelete(DeleteBehavior.Restrict);

        // One-to-Many: Category - Gigs
        builder.Entity<Gig>()
            .HasOne(g => g.Category)
            .WithMany(c => c.Gigs)
            .HasForeignKey(g => g.CategoryId)
            .OnDelete(DeleteBehavior.Restrict);

        // One-to-Many: ApplicationUser (Buyer) - Orders
        builder.Entity<Order>()
            .HasOne(o => o.Buyer)
            .WithMany()
            .HasForeignKey(o => o.BuyerId)
            .OnDelete(DeleteBehavior.Restrict);

        // One-to-Many: ApplicationUser (Seller) - Orders
        builder.Entity<Order>()
            .HasOne(o => o.Seller)
            .WithMany()
            .HasForeignKey(o => o.SellerId)
            .OnDelete(DeleteBehavior.Restrict);

        // One-to-Many: Gig - Orders
        builder.Entity<Order>()
            .HasOne(o => o.Gig)
            .WithMany()
            .HasForeignKey(o => o.GigId)
            .OnDelete(DeleteBehavior.Restrict);

        // One-to-Many: Order - Deliveries
        builder.Entity<OrderDelivery>()
            .HasOne(d => d.Order)
            .WithMany(o => o.Deliveries)
            .HasForeignKey(d => d.OrderId)
            .OnDelete(DeleteBehavior.Cascade);
    }
}