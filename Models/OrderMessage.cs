using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace PadiPay.Models;

public class OrderMessage
{
    public int Id { get; set; }

    public int OrderId { get; set; }
    public virtual Order? Order { get; set; }

    [Required]
    public string SenderId { get; set; } = string.Empty;
    public virtual ApplicationUser? Sender { get; set; }

    [Required(ErrorMessage = "Message cannot be empty.")]
    [StringLength(2000, ErrorMessage = "Message cannot exceed 2000 characters.")]
    public string Message { get; set; } = string.Empty;

    [StringLength(500)]
    public string? AttachmentUrl { get; set; }

    [StringLength(255)]
    public string? AttachmentFileName { get; set; }

    public bool IsReadByCounterparty { get; set; } = false;

    public DateTime SentAt { get; set; } = DateTime.UtcNow;
}
