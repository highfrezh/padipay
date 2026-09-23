namespace PadiPay.ViewModels;

public class AdminDashboardViewModel
{
    // User Stats
    public int TotalUsers { get; set; }
    public int TotalSellers { get; set; }
    public int TotalBuyers { get; set; }

    // Gig Stats
    public int TotalGigs { get; set; }
    public int ActiveGigs { get; set; }
    public int PausedGigs { get; set; }

    // Order & Financial Stats
    public int TotalOrders { get; set; }
    public int CompletedOrders { get; set; }
    public int InProgressOrders { get; set; }
    public int DisputedOrders { get; set; }
    public decimal TotalEscrowVolume { get; set; }
    public decimal TotalReleasedFunds { get; set; }
    public decimal TotalRefundedFunds { get; set; }

    // Dispute Stats
    public int OpenDisputes { get; set; }
    public int UnderReviewDisputes { get; set; }
    public int ResolvedDisputes { get; set; }

    // Recent Activity
    public List<RecentOrderSummary> RecentOrders { get; set; } = new();
    public List<RecentDisputeSummary> RecentDisputes { get; set; } = new();
}

public class RecentOrderSummary
{
    public int Id { get; set; }
    public string OrderNumber { get; set; } = string.Empty;
    public string BuyerName { get; set; } = string.Empty;
    public string SellerName { get; set; } = string.Empty;
    public string GigTitle { get; set; } = string.Empty;
    public decimal Amount { get; set; }
    public string Status { get; set; } = string.Empty;
    public DateTime CreatedAt { get; set; }
}

public class RecentDisputeSummary
{
    public int Id { get; set; }
    public int OrderId { get; set; }
    public string OrderNumber { get; set; } = string.Empty;
    public string InitiatorName { get; set; } = string.Empty;
    public string Reason { get; set; } = string.Empty;
    public decimal OrderAmount { get; set; }
    public string Status { get; set; } = string.Empty;
    public DateTime CreatedAt { get; set; }
}
