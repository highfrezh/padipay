using System.ComponentModel.DataAnnotations;

namespace PadiPay.Models;

public class OrderDispute
{
    public int Id { get; set; }

    // Foreign Key to Order (1-to-1)
    public int OrderId { get; set; }
    public virtual Order? Order { get; set; }

    // Foreign Key to Initiator (Buyer or Seller)
    public string InitiatorId { get; set; } = string.Empty;
    public virtual ApplicationUser? Initiator { get; set; }

    [Required(ErrorMessage = "Please select a reason for the dispute.")]
    [StringLength(200)]
    public string Reason { get; set; } = string.Empty;

    [Required(ErrorMessage = "Please provide detailed explanation and context.")]
    [StringLength(3000, MinimumLength = 20, ErrorMessage = "Explanation must be between 20 and 3000 characters.")]
    public string Explanation { get; set; } = string.Empty;

    public DisputeStatus Status { get; set; } = DisputeStatus.Open;

    [StringLength(2000)]
    public string? AdminNotes { get; set; }

    public string? ResolvedById { get; set; }
    public virtual ApplicationUser? ResolvedBy { get; set; }

    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    public DateTime? ResolvedAt { get; set; }
}
