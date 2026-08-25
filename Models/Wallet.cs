using System.ComponentModel.DataAnnotations.Schema;

namespace PadiPay.Models;

public class Wallet
{
    public int Id { get; set; }
    public string UserId { get; set; } = string.Empty;
    
    [Column(TypeName = "decimal(18,2)")]
    public decimal Balance { get; set; }

    // Navigation property
    public virtual ApplicationUser? User { get; set; }
}
