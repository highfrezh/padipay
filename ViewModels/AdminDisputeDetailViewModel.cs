using PadiPay.Models;

namespace PadiPay.ViewModels;

public class AdminDisputeDetailViewModel
{
    public OrderDispute Dispute { get; set; } = null!;
    public Order Order { get; set; } = null!;
    public ApplicationUser Buyer { get; set; } = null!;
    public ApplicationUser Seller { get; set; } = null!;

    // Admin resolution inputs
    public string AdminNotes { get; set; } = string.Empty;
}
