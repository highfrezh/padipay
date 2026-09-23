using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace PadiPay.Models;

public class Order
{
    public int Id { get; set; }

    [Required]
    [StringLength(32)]
    public string OrderNumber { get; set; } = string.Empty;

    // Foreign Key to Gig
    public int GigId { get; set; }
    public virtual Gig? Gig { get; set; }

    // Foreign Key to Buyer
    public string BuyerId { get; set; } = string.Empty;
    public virtual ApplicationUser? Buyer { get; set; }

    // Foreign Key to Seller
    public string SellerId { get; set; } = string.Empty;
    public virtual ApplicationUser? Seller { get; set; }

    [Column(TypeName = "decimal(18,2)")]
    public decimal Amount { get; set; }

    public int DeliveryDays { get; set; } = 3;

    public int AllowedRevisions { get; set; } = 2;
    public int RevisionsUsed { get; set; } = 0;

    public OrderStatus Status { get; set; } = OrderStatus.InProgress;

    [Required(ErrorMessage = "Please provide project specifications and requirements.")]
    [StringLength(3000, ErrorMessage = "Requirements cannot exceed 3000 characters.")]
    public string BuyerRequirements { get; set; } = string.Empty;

    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    public DateTime? DueDate { get; set; }
    public DateTime? DeliveredAt { get; set; }
    public DateTime? CompletedAt { get; set; }

    // Linked escrow transaction record
    public int? TransactionId { get; set; }
    public virtual Transaction? Transaction { get; set; }

    // Deliverables history (supports re-deliveries after revisions)
    public virtual ICollection<OrderDelivery> Deliveries { get; set; } = new List<OrderDelivery>();

    // Client review for completed orders (1-to-1)
    public virtual Review? Review { get; set; }

    // Dispute ticket if arbitration is requested (1-to-1)
    public virtual OrderDispute? Dispute { get; set; }
}
