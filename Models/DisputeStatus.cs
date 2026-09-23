namespace PadiPay.Models;

public enum DisputeStatus
{
    Open = 1,
    UnderReview = 2,
    ResolvedRefundBuyer = 3,
    ResolvedPayoutSeller = 4,
    Cancelled = 5
}
