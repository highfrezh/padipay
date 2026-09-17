namespace PadiPay.Models;

public enum OrderStatus
{
    InProgress = 1, // Escrow funded, requirements provided, seller working
    Delivered = 2,  // Seller submitted deliverables, awaiting buyer inspection
    InRevision = 3, // Buyer requested changes
    Completed = 4,  // Buyer accepted delivery, funds released to seller
    Disputed = 5,   // Dispute raised for admin arbitration
    Cancelled = 6   // Cancelled & refunded to buyer wallet
}
