using System.ComponentModel.DataAnnotations;

namespace PadiPay.Models;

public class OrderDelivery
{
    public int Id { get; set; }

    // Foreign Key to Order
    public int OrderId { get; set; }
    public virtual Order? Order { get; set; }

    // Foreign Key to Seller
    public string SellerId { get; set; } = string.Empty;
    public virtual ApplicationUser? Seller { get; set; }

    [Required]
    [StringLength(2000, ErrorMessage = "Delivery message cannot exceed 2000 characters.")]
    public string Message { get; set; } = string.Empty;

    [Url]
    [StringLength(300)]
    public string? LivePreviewUrl { get; set; }

    [Url]
    [StringLength(300)]
    public string? GithubRepoUrl { get; set; }

    public string? DeliverableFileUrl { get; set; }

    public DateTime DeliveredAt { get; set; } = DateTime.UtcNow;

    // Revision feedback notes if the buyer requested revisions on this delivery
    [StringLength(2000)]
    public string? RevisionNotes { get; set; }
    public DateTime? RevisionRequestedAt { get; set; }
}
