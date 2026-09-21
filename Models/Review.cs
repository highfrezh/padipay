using System.ComponentModel.DataAnnotations;

namespace PadiPay.Models;

public class Review
{
    public int Id { get; set; }

    // 1-to-1 relationship with Order
    public int OrderId { get; set; }
    public virtual Order? Order { get; set; }

    // Foreign key to Gig
    public int GigId { get; set; }
    public virtual Gig? Gig { get; set; }

    // Foreign key to Buyer (ApplicationUser)
    public string BuyerId { get; set; } = string.Empty;
    public virtual ApplicationUser? Buyer { get; set; }

    // Foreign key to Seller (ApplicationUser)
    public string SellerId { get; set; } = string.Empty;
    public virtual ApplicationUser? Seller { get; set; }

    [Range(1, 5, ErrorMessage = "Rating must be between 1 and 5 stars.")]
    public int Rating { get; set; } = 5;

    [Required(ErrorMessage = "Please share your review feedback.")]
    [StringLength(1500, MinimumLength = 5, ErrorMessage = "Review must be between 5 and 1500 characters.")]
    public string Comment { get; set; } = string.Empty;

    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
}
