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
    }
}