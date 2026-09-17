using System.ComponentModel.DataAnnotations;
using PadiPay.Models;

namespace PadiPay.ViewModels;

public class CheckoutViewModel
{
    public int GigId { get; set; }
    public Gig? Gig { get; set; }

    public decimal WalletBalance { get; set; }
    public bool HasSufficientBalance => Gig != null && WalletBalance >= Gig.Price;
    public decimal ShortfallAmount => (Gig != null && WalletBalance < Gig.Price) ? (Gig.Price - WalletBalance) : 0;

    [Required(ErrorMessage = "Please provide your project specifications and requirements for the freelancer.")]
    [StringLength(3000, MinimumLength = 15, ErrorMessage = "Requirements must be between 15 and 3000 characters.")]
    [Display(Name = "Project Requirements & Instructions")]
    public string Requirements { get; set; } = string.Empty;
}
