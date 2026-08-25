using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace PadiPay.Models;

public class Gig
{
    public int Id { get; set; }

    [Required]
    [StringLength(100, ErrorMessage = "Gig title must be under 100 characters.")]
    public string Title { get; set; } = string.Empty;

    public string Slug { get; set; } = string.Empty;

    [Required]
    public string Description { get; set; } = string.Empty;

    // Foreign Key to Category
    public int CategoryId { get; set; }
    public virtual Category? Category { get; set; }

    // Foreign Key to Seller (ApplicationUser)
    public string SellerId { get; set; } = string.Empty;
    public virtual ApplicationUser? Seller { get; set; }

    [Range(1000, 10000000, ErrorMessage = "Price must be between ₦1,000 and ₦10,000,000.")]
    [Column(TypeName = "decimal(18,2)")]
    public decimal Price { get; set; }

    [Range(1, 90, ErrorMessage = "Delivery turnaround must be between 1 and 90 days.")]
    public int DeliveryDays { get; set; } = 3;

    public int RevisionCount { get; set; } = 2; // e.g. 2 revisions included

    public string? CoverImageUrl { get; set; }

    [Required]
    [Display(Name = "Requirements from Buyer")]
    public string Requirements { get; set; } = string.Empty; // What buyer needs to provide

    public string Tags { get; set; } = string.Empty; // e.g. "c#, aspnet, react, sql"

    public bool IsActive { get; set; } = true;

    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    public DateTime? UpdatedAt { get; set; }
}
